using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CatiaGpuTuner;
class Fake:IDriver {
 public string Version{get{return "596.71";}}public string Gpus{get{return "NVIDIA RTX Test";}}
 public Dictionary<uint,ValueState> disk=new Dictionary<uint,ValueState>(),memory=new Dictionary<uint,ValueState>();public int Saves;public bool FailSave,FailReadOnce;
 public Fake(){disk[Engine.Allowed[0]]=new ValueState{Id=Engine.Allowed[0],Exists=true,Local=true,Value=0};Reload();}
 public string Resolve(string e){return "Dassault Systemes CATIA";}
 static ValueState Copy(ValueState v){return new ValueState{Id=v.Id,Exists=v.Exists,Local=v.Local,Value=v.Value};}
 public ValueState Read(string p,uint id){if(FailReadOnce&&Saves==1){FailReadOnce=false;return new ValueState{Id=id};}return memory.ContainsKey(id)?Copy(memory[id]):new ValueState{Id=id};}
 public void Set(string p,uint id,uint v){memory[id]=new ValueState{Id=id,Value=v,Local=true,Exists=true};}
 public void Remove(string p,uint id){memory.Remove(id);}
 public void Save(){Saves++;if(FailSave){FailSave=false;throw new Exception("Simulated save failure");}disk=memory.ToDictionary(x=>x.Key,x=>Copy(x.Value));}
 public void Reload(){memory=disk.ToDictionary(x=>x.Key,x=>Copy(x.Value));}
 public void Export(string path){File.WriteAllText(path,"fake snapshot");}public void Dispose(){}
}
class Tests {
 static int count;
 static void Assert(bool b,string s){if(!b)throw new Exception(s);count++;Console.WriteLine("PASS "+s);}
 static void Throws(Action action,string label){bool failed=false;try{action();}catch{failed=true;}Assert(failed,label);}
 static int Main(string[] args){try{string root=args[0];Directory.CreateDirectory(root);
  var d=new Fake();string dir=Engine.Apply(d,"CNEXT.exe",true,root);Assert(d.Read("",Engine.Allowed[2]).Value==0x08416747,"Fast mode saved and verified");Assert(File.Exists(Path.Combine(dir,"driver-profiles.bin")),"Full snapshot created");
  var file=Path.Combine(dir,"settings.json");var b=Engine.Load(file);Assert(b.Outcome=="AppliedAndVerified","Backup checksum and outcome");
  Engine.Restore(d,file,root);Assert(d.disk.Count==1&&d.disk[Engine.Allowed[0]].Value==0,"Rollback restores local value and removes absent overrides");
  var fail=new Fake{FailSave=true};Throws(()=>Engine.Apply(fail,"CNEXT.exe",true,root),"Save failure reported");Assert(fail.disk.Count==1&&fail.disk[Engine.Allowed[0]].Value==0,"Save failure rollback");
  var mismatch=new Fake{FailReadOnce=true};Throws(()=>Engine.Apply(mismatch,"CNEXT.exe",false,root),"Verification mismatch reported");Assert(mismatch.disk.Count==1&&mismatch.disk[Engine.Allowed[0]].Value==0,"Mismatch rollback");
  b.Driver="other";Engine.Store(dir,b);Throws(()=>Engine.Restore(d,file,root),"Cross-driver restore blocked");
  File.AppendAllText(file,"tampered");Throws(()=>Engine.Load(file),"Damaged backup rejected");
  b.Before[0].Id=0xDEADBEEF;Throws(()=>Engine.Validate(b),"Unknown setting rejected");
  Assert(Engine.Plan(false)[2].Target==0x60925292,"Balanced mode application VSync");
  Console.WriteLine(count+" tests passed");return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}
