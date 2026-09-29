using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;

namespace CatiaGpuTuner {
public sealed class Readiness {
 public bool ControlPanel, CadFound, SupportedGpu, ProfileFound;
 public string Gpu, Driver, CadExe, Profile, Reason;
 public List<string> Lines=new List<string>();
 public bool Ready { get { return ControlPanel && CadFound && SupportedGpu && ProfileFound; } }
}
public static class SystemCheck {
 static readonly string[] Roots={
  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Dassault Systemes"),
  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Dassault Systemes")
 };
 static bool ControlPanelInstalled(){
 try { using(var packages=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\\Classes\\ActivatableClasses\\Package")) if(packages!=null && packages.GetSubKeyNames().Any(x=>x.IndexOf("NVIDIACorp.NVIDIAControlPanel",StringComparison.OrdinalIgnoreCase)>=0))return true; } catch {}
  try { using(var key=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall")) foreach(var name in key.GetSubKeyNames()) using(var item=key.OpenSubKey(name)) if(((item.GetValue("DisplayName")??"").ToString()).IndexOf("NVIDIA Control Panel",StringComparison.OrdinalIgnoreCase)>=0)return true; } catch {}
  if(Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"NVIDIA Corporation","Control Panel Client")))return true;
  try { using(var p=Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -Command \"if (Get-AppxPackage -Name NVIDIACorp.NVIDIAControlPanel) { exit 0 } else { exit 1 }\""){CreateNoWindow=true,UseShellExecute=false})) {p.WaitForExit(5000);return p.ExitCode==0;} } catch { return false; }
 }
 static IEnumerable<string> CadExecutables(){
  foreach(var root in Roots) if(Directory.Exists(root)) {
   IEnumerable<string> found=new string[0];try{found=Directory.EnumerateFiles(root,"CNEXT.exe",SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(root,"3DEXPERIENCE.exe",SearchOption.AllDirectories));}catch(UnauthorizedAccessException){}catch(IOException){}
   foreach(var file in found.OrderByDescending(x=>x.IndexOf("Cloud",StringComparison.OrdinalIgnoreCase)>=0).ThenByDescending(File.GetLastWriteTimeUtc))yield return file;
  }
 }
 public static Readiness Inspect(){
  var r=new Readiness();r.ControlPanel=ControlPanelInstalled();r.Lines.Add((r.ControlPanel?"✓":"!")+" NVIDIA Control Panel: "+(r.ControlPanel?"kurulu.":"bulunamadı. Microsoft Store/NVIDIA sürücü paketiyle kurun."));
  try { using(var d=new NvDriver()){
   r.Gpu=d.Gpus;r.Driver=d.Version;r.SupportedGpu=d.IsProfessionalCadGpu;
   r.Lines.Add((r.SupportedGpu?"✓":"!")+" NVIDIA GPU: "+d.Gpus+" | sürücü "+d.Version);
   if(!r.SupportedGpu)r.Lines.Add("Bu uygulama yalnızca NVIDIA RTX A serisi, RTX PRO ve Quadro RTX iş istasyonu GPU’ları için ayar yükler.");
   foreach(var candidate in CadExecutables())try{var profile=d.Resolve(candidate);r.CadExe=candidate;r.Profile=profile;r.CadFound=true;r.ProfileFound=true;break;}catch{}
  }}catch(Exception e){r.Reason=e.Message;r.Lines.Add("! NVIDIA sürücü erişimi: "+e.Message);}
  if(r.CadFound)r.Lines.Add("✓ CAD uygulaması ve NVIDIA profili: "+Path.GetFileName(r.CadExe)+" → "+r.Profile);else r.Lines.Add("! CATIA/3DEXPERIENCE için eşleşen kurulum ve NVIDIA profili bulunamadı.");
  if(!r.Ready)r.Reason=String.IsNullOrEmpty(r.Reason)?"Sistem destek koşullarını karşılamıyor; hiçbir ayar değiştirilmedi.":r.Reason;
  return r;
 }
}
}
