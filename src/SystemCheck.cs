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
  var r=new Readiness();r.ControlPanel=ControlPanelInstalled();r.Lines.Add((r.ControlPanel?"✓":"!")+" NVIDIA Control Panel: "+(r.ControlPanel?UiText.L("kurulu.","installed."):UiText.L("bulunamadı. Microsoft Store/NVIDIA sürücü paketiyle kurun.","not found. Install it from Microsoft Store or the NVIDIA driver package.")));
  try { using(var d=new NvDriver()){
   r.Gpu=d.Gpus;r.Driver=d.Version;r.SupportedGpu=d.IsProfessionalCadGpu;
   r.Lines.Add((r.SupportedGpu?"✓":"!")+" NVIDIA GPU: "+d.Gpus+" | "+UiText.L("sürücü ","driver ")+d.Version);
   if(!r.SupportedGpu)r.Lines.Add(UiText.L("Bu uygulama yalnızca NVIDIA RTX A serisi, RTX PRO ve Quadro RTX iş istasyonu GPU’ları için ayar yükler.","This app applies settings only for NVIDIA RTX A series, RTX PRO and Quadro RTX workstation GPUs."));
   foreach(var candidate in CadExecutables())try{var profile=d.Resolve(candidate);r.CadExe=candidate;r.Profile=profile;r.CadFound=true;r.ProfileFound=true;break;}catch{}
  }}catch(Exception e){r.Reason=e.Message;r.Lines.Add("! "+UiText.L("NVIDIA sürücü erişimi: ","NVIDIA driver access: ")+e.Message);}
  if(r.CadFound)r.Lines.Add("✓ "+UiText.L("CAD uygulaması ve NVIDIA profili: ","CAD application and NVIDIA profile: ")+Path.GetFileName(r.CadExe)+" → "+r.Profile);else r.Lines.Add("! "+UiText.L("CATIA/3DEXPERIENCE için eşleşen kurulum ve NVIDIA profili bulunamadı.","No matching CATIA/3DEXPERIENCE installation and NVIDIA profile was found."));
  if(!r.Ready)r.Reason=String.IsNullOrEmpty(r.Reason)?UiText.L("Sistem destek koşullarını karşılamıyor; hiçbir ayar değiştirilmedi.","The system does not meet the support requirements; no settings were changed."):r.Reason;
  return r;
 }
}
}
