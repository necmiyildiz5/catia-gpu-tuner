using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace CatiaGpuTuner {
public sealed class Setting {
 public uint Id, Target;public string Name,Help;
 public Setting(uint id,uint value,string name,string help){Id=id;Target=value;Name=name;Help=help;}
}
public sealed class Backup {
 public int Schema=1;public string Driver,Gpus,Profile,Executable,CreatedUtc,Outcome; public List<ValueState> Before=new List<ValueState>(); public Dictionary<string,uint> Targets=new Dictionary<string,uint>();
}
public static class Engine {
 public static readonly uint[] Allowed={0x1057EB71,0x10835002,0x00A879CF};
 public static List<Setting> Plan(){return new List<Setting>{
  new Setting(Allowed[0],1,"Güç yönetimi","Maksimum performansı tercih et. CATIA çalışırken güç tasarrufundan kaynaklanan GPU frekans düşüşlerini azaltır; tüketim ve ısı artabilir. İşlemci darboğazını çözmez."),
  new Setting(Allowed[1],0,"Kare hızı sınırı","Kapalı. Sürücüdeki FPS sınırını kaldırır. CATIA veya ekran senkronizasyonu yine sınır koyabilir."),
  new Setting(Allowed[2],0x08416747u,"Dikey senkronizasyon","Kapalı. Döndürme ve yakınlaştırmada ekran yenilemesine bağlı sınırı kaldırır; görüntü yırtılması olabilir.")};}
 public static string Label(uint id,ValueState v){if(!v.Exists)return "Profilde kayıt yok";if(id==Allowed[0])return v.Value==1?"Maksimum performans":"0x"+v.Value.ToString("X8");if(id==Allowed[1])return v.Value==0?"Kapalı":v.Value+" FPS";return v.Value==0x08416747?"Kapalı":v.Value==0x60925292?"Uygulama kontrollü":"0x"+v.Value.ToString("X8");}
 static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=1024*1024};}
 public static string Hash(string path){using(var sha=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","");}
 public static void Store(string directory,Backup b){string path=Path.Combine(directory,"settings.json"),temp=path+".tmp";File.WriteAllText(temp,Json().Serialize(b),Encoding.UTF8);if(File.Exists(path))File.Delete(path);File.Move(temp,path);File.WriteAllText(path+".sha256",Hash(path));}
 public static Backup Load(string path){
  if(new FileInfo(path).Length>1024*1024)throw new InvalidOperationException("Yedek boyutu geçersiz.");
  if(!File.Exists(path+".sha256")||File.ReadAllText(path+".sha256").Trim()!=Hash(path))throw new InvalidOperationException("Yedek bütünlük kontrolü başarısız.");
  var b=Json().Deserialize<Backup>(File.ReadAllText(path));Validate(b);return b;
 }
 public static void Validate(Backup b){if(b==null||b.Schema!=1||String.IsNullOrWhiteSpace(b.Profile)||String.IsNullOrWhiteSpace(b.Driver)||String.IsNullOrWhiteSpace(b.Gpus)||b.Before==null||b.Before.Count!=3||b.Before.Select(x=>x.Id).Distinct().Count()!=3||b.Before.Any(x=>!Allowed.Contains(x.Id)||x.Local&&!x.Exists))throw new InvalidOperationException("Yedek şeması/ayarları geçersiz.");}
 static void RestoreValues(IDriver d,Backup b){foreach(var v in b.Before){if(v.Local)d.Set(b.Profile,v.Id,v.Value);else d.Remove(b.Profile,v.Id);}d.Save();d.Reload();foreach(var v in b.Before){var actual=d.Read(b.Profile,v.Id);if(v.Local?(!actual.Local||actual.Value!=v.Value):actual.Local)throw new InvalidOperationException("Geri alma doğrulanamadı: "+v.Id.ToString("X8"));}}
 public static string Apply(IDriver d,string exe,string root){
  string profile=d.Resolve(exe);var plan=Plan();d.Reload();
  var b=new Backup{Driver=d.Version,Gpus=d.Gpus,Profile=profile,Executable=exe,CreatedUtc=DateTime.UtcNow.ToString("o"),Outcome="Prepared"};
  foreach(var s in plan){b.Before.Add(d.Read(profile,s.Id));b.Targets.Add(s.Id.ToString("X8"),s.Target);}
  string dir=Path.Combine(root,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(dir);
  d.Export(Path.Combine(dir,"driver-profiles.bin"));Store(dir,b);
  try{foreach(var s in plan)d.Set(profile,s.Id,s.Target);d.Save();d.Reload();foreach(var s in plan){var v=d.Read(profile,s.Id);if(!v.Exists||v.Value!=s.Target)throw new InvalidOperationException("Uygulama doğrulanamadı: "+s.Name);}b.Outcome="AppliedAndVerified";Store(dir,b);return dir;}
  catch(Exception e){try{d.Reload();RestoreValues(d,b);b.Outcome="FailedAndRolledBack";Store(dir,b);}catch(Exception rollback){throw new InvalidOperationException("İşlem başarısız; otomatik geri alma da doğrulanamadı. Yedek: "+dir+"\n"+e.Message+"\n"+rollback.Message);}throw new InvalidOperationException("İşlem başarısız, önceki ayarlar geri alındı. "+e.Message+"\nYedek: "+dir);}
 }
 public static string Restore(IDriver d,string file,string root){var b=Load(file);if(b.Driver!=d.Version||b.Gpus!=d.Gpus)throw new InvalidOperationException("Yedek farklı GPU/sürücüye ait. Sürümler arası otomatik geri alma engellendi.");if(d.Resolve(b.Executable)!=b.Profile)throw new InvalidOperationException("Uygulama profil eşleştirmesi değişmiş.");
  d.Reload();var undo=new Backup{Driver=d.Version,Gpus=d.Gpus,Profile=b.Profile,Executable=b.Executable,CreatedUtc=DateTime.UtcNow.ToString("o"),Outcome="BeforeRestore"};foreach(var v in b.Before)undo.Before.Add(d.Read(b.Profile,v.Id));
  var dir=Path.Combine(root,"restore-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);d.Export(Path.Combine(dir,"driver-profiles.bin"));Store(dir,undo);
  try{RestoreValues(d,b);undo.Outcome="RestoreVerified";Store(dir,undo);return dir;}catch(Exception e){try{d.Reload();RestoreValues(d,undo);}catch(Exception r){throw new InvalidOperationException("Geri alma ve kurtarma başarısız: "+e.Message+" / "+r.Message+" Yedek: "+dir);}throw new InvalidOperationException("Geri alma başarısız; işlem öncesi durum korundu. "+e.Message);}
 }
}
}
