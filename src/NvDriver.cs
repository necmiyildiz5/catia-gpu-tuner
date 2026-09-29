using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CatiaGpuTuner {
public sealed class ValueState {
 public uint Id, Value; public bool Exists, Local;
}
public interface IDriver : IDisposable {
 string Version {get;} string Gpus {get;}
 string Resolve(string executable);
 ValueState Read(string profile,uint id);
 void Set(string profile,uint id,uint value);
 void Remove(string profile,uint id);
 void Save(); void Reload(); void Export(string path);
}
public sealed class NvDriver : IDriver {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Query(uint id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Init();
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(out IntPtr s);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Session(IntPtr s);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int VersionCall(out uint v,StringBuilder branch);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GpuEnum([Out] IntPtr[] g,out int n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GpuName(IntPtr g,StringBuilder name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Unicode)] delegate int Find(IntPtr s,string name,out IntPtr p);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Unicode)] delegate int FindApp(IntPtr s,string name,out IntPtr p,IntPtr app);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Info(IntPtr s,IntPtr p,IntPtr data);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Get(IntPtr s,IntPtr p,uint id,IntPtr data);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int SetCall(IntPtr s,IntPtr p,IntPtr data);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Delete(IntPtr s,IntPtr p,uint id);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl,CharSet=CharSet.Unicode)] delegate int FileCall(IntPtr s,string path);
 static IntPtr module; Query query; IntPtr session;
 public string Version {get;private set;} public string Gpus {get;private set;}
 T Fn<T>(uint id) where T:class {IntPtr p=query(id);if(p==IntPtr.Zero)throw new InvalidOperationException("Sürücü gerekli NVIDIA API işlevini sunmuyor.");return Marshal.GetDelegateForFunctionPointer(p,typeof(T)) as T;}
 static void Check(int code){if(code!=0)throw new InvalidOperationException("NVIDIA API hata kodu: "+code);}
 static IntPtr Buffer(int size){var p=Marshal.AllocHGlobal(size);Marshal.Copy(new byte[size],0,p,size);Marshal.WriteInt32(p,size|0x10000);return p;}
 public NvDriver(){
  if(!Environment.Is64BitProcess)throw new InvalidOperationException("64 bit Windows gereklidir.");
  if(module==IntPtr.Zero)module=LoadLibraryEx(System.IO.Path.Combine(Environment.SystemDirectory,"nvapi64.dll"),IntPtr.Zero,0x800);
  if(module==IntPtr.Zero)throw new InvalidOperationException("NVIDIA sürücü API'si bulunamadı. RTX sürücüsünü kontrol edin.");
  query=(Query)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"nvapi_QueryInterface"),typeof(Query));Check(Fn<Init>(0x0150e828)());
  uint version;var branch=new StringBuilder(64);Check(Fn<VersionCall>(0x2926aaad)(out version,branch));Version=(version/100)+"."+(version%100).ToString("00");
  var handles=new IntPtr[64];int n;Check(Fn<GpuEnum>(0xe5ac921f)(handles,out n));var names=new System.Collections.Generic.List<string>();
  for(int i=0;i<n;i++){var name=new StringBuilder(64);Check(Fn<GpuName>(0xceee8e9f)(handles[i],name));names.Add(name.ToString());}Gpus=String.Join(" / ",names);
  if(Gpus.IndexOf("RTX",StringComparison.OrdinalIgnoreCase)<0)throw new InvalidOperationException("Bu sürüm NVIDIA RTX GPU gerektirir. Algılanan: "+Gpus);
  Check(Fn<Create>(0x0694d52e)(out session));try{Reload();}catch{Dispose();throw;}
 }
 IntPtr Profile(string name){IntPtr p;Check(Fn<Find>(0x7e4a9a0b)(session,name,out p));return p;}
 public string Resolve(string executable){
  if(!System.IO.File.Exists(executable)||!String.Equals(System.IO.Path.GetExtension(executable),".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Kurulu CATIA uygulamasının EXE dosyasını seçin.");
  IntPtr app=Buffer(12296),p;
  try{Check(Fn<FindApp>(0xeee566b2)(session,executable,out p,app));}finally{Marshal.FreeHGlobal(app);}
  IntPtr info=Buffer(4116);try{Check(Fn<Info>(0x61cd6fd6)(session,p,info));string name=Marshal.PtrToStringUni(IntPtr.Add(info,4),2048).Split('\0')[0];
   if(name.IndexOf("CATIA",StringComparison.OrdinalIgnoreCase)<0 && name.IndexOf("3DExperience",StringComparison.OrdinalIgnoreCase)<0)throw new InvalidOperationException("Seçilen EXE CATIA/3DEXPERIENCE profiline bağlı değil: "+name+". Başlatıcı yerine gerçek CATIA EXE'sini seçin.");return name;
  }finally{Marshal.FreeHGlobal(info);}
 }
 public ValueState Read(string name,uint id){IntPtr p=Buffer(12320);try{int code=Fn<Get>(0x73bf8338)(session,Profile(name),id,p);if(code==-160)return new ValueState{Id=id};Check(code);if(Marshal.ReadInt32(p,4104)!=0)throw new InvalidOperationException("Ayar veri türü desteklenmiyor.");return new ValueState{Id=id,Exists=true,Value=unchecked((uint)Marshal.ReadInt32(p,8220)),Local=Marshal.ReadInt32(p,4108)==0&&Marshal.ReadInt32(p,4112)==0};}finally{Marshal.FreeHGlobal(p);}}
 public void Set(string name,uint id,uint value){IntPtr p=Buffer(12320);try{Marshal.WriteInt32(p,4100,unchecked((int)id));Marshal.WriteInt32(p,8220,unchecked((int)value));Check(Fn<SetCall>(0x577dd202)(session,Profile(name),p));}finally{Marshal.FreeHGlobal(p);}}
 public void Remove(string name,uint id){int c=Fn<Delete>(0xe4a26362)(session,Profile(name),id);if(c!=-160)Check(c);}
 public void Save(){Check(Fn<Session>(0xfcbc7e14)(session));}
 public void Reload(){Check(Fn<Session>(0x375dbd6b)(session));}
 public void Export(string path){Check(Fn<FileCall>(0x2be25df8)(session,path));}
 public void Dispose(){if(session!=IntPtr.Zero){Fn<Session>(0xdad9cff8)(session);session=IntPtr.Zero;}}
}
}
