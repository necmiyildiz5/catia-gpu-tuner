using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace CatiaGpuTuner {
static class Program {
 [STAThread] static void Main(){
  bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\CatiaGpuTuner-2",out created)){
   if(!created){MessageBox.Show("CATIA GPU Tuner zaten açık.");return;}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());mutex.ReleaseMutex();
  }
 }
}
public sealed class MainForm:Form {
 static readonly string BackupRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CatiaGpuTuner","Backups");
 static readonly string ConsentFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CatiaGpuTuner","performance-profile-consent-v1");
 Label headline=new Label(),subhead=new Label(),summary=new Label(),details=new Label(); Button refresh,help,backups; bool running;
 string L(string tr,string en){return UiText.L(tr,en);}
 public MainForm(){
  Text="CATIA GPU Tuner";ClientSize=new Size(760,420);MinimumSize=new Size(760,420);MaximumSize=new Size(760,420);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(19,27,39);ForeColor=Color.White;AutoScaleMode=AutoScaleMode.None;
  var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(38,30,38,22),BackColor=BackColor};Controls.Add(body);
  headline.Text="CATIA GPU Tuner";headline.Font=new Font("Segoe UI",25,FontStyle.Regular);headline.ForeColor=Color.White;headline.Dock=DockStyle.Top;headline.Height=48;body.Controls.Add(headline);
  subhead.Text=L("NVIDIA RTX iş istasyonu GPU’ları için otomatik CATIA performans ayarları","Automatic CATIA performance settings for NVIDIA RTX workstation GPUs");subhead.ForeColor=Color.FromArgb(168,201,221);subhead.Dock=DockStyle.Top;subhead.Height=30;body.Controls.Add(subhead);
  summary.Dock=DockStyle.Top;summary.Height=70;summary.Padding=new Padding(14,12,14,8);summary.Font=new Font("Segoe UI",13,FontStyle.Bold);summary.BackColor=Color.FromArgb(36,58,82);summary.ForeColor=Color.White;body.Controls.Add(summary);
  details.Dock=DockStyle.Top;details.Height=140;details.Padding=new Padding(15,16,8,6);details.Font=new Font("Segoe UI",10);details.ForeColor=Color.FromArgb(224,233,239);body.Controls.Add(details);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=46,FlowDirection=FlowDirection.LeftToRight};body.Controls.Add(actions);
  refresh=Button(L("Sistemi yeniden denetle","Check system again"),RefreshNow,Color.FromArgb(55,112,218));help=Button(L("Yardım","Help"),ShowHelp,Color.FromArgb(38,126,99));backups=Button(L("Yedekleri aç","Open backups"),()=>{Directory.CreateDirectory(BackupRoot);Process.Start(BackupRoot);},Color.FromArgb(70,83,101));actions.Controls.Add(refresh);actions.Controls.Add(help);actions.Controls.Add(backups);
  Shown+=(s,e)=>RefreshNow();
 }
 Button Button(string text,Action action,Color color){var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(170,40),FlatStyle=FlatStyle.Flat,BackColor=color,ForeColor=Color.White,Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(0,0,12,0)};b.FlatAppearance.BorderSize=0;b.Click+=(s,e)=>action();return b;}
 async void RefreshNow(){if(running)return;running=true;refresh.Enabled=false;UseWaitCursor=true;try{await Task.Yield();var readiness=SystemCheck.Inspect();details.Text=String.Join(Environment.NewLine,readiness.Lines);
   if(!readiness.Ready){summary.Text=L("UYARI — Ayarlar yüklenmedi","WARNING — Settings were not applied");summary.BackColor=Color.FromArgb(114,65,45);details.Text+=Environment.NewLine+Environment.NewLine+L("Bu program ayarları yalnızca desteklenen sistemde otomatik yükler. Mevcut ayarlarınız korunmuştur.","This app applies settings automatically only on a supported system. Your current settings were preserved.");return;}
   if(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(readiness.CadExe)).Length>0){summary.Text=L("CATIA açık — Ayarlar yüklenmedi","CATIA is open — Settings were not applied");summary.BackColor=Color.FromArgb(114,65,45);details.Text+=Environment.NewLine+Environment.NewLine+L("CATIA/3DEXPERIENCE’i kapatın ve ‘Sistemi yeniden denetle’yi kullanın. Mevcut ayarlar korunmuştur.","Close CATIA/3DEXPERIENCE and use ‘Check system again’. Your current settings were preserved.");return;}
   if(!File.Exists(ConsentFile) && !ConfirmFirstUse()){summary.Text="ONAY BEKLENİYOR — Ayarlar yüklenmedi";summary.BackColor=Color.FromArgb(114,65,45);details.Text+=Environment.NewLine+Environment.NewLine+"Ayarları uygulamak için güvenlik bildirimini onaylayın. Mevcut ayarlarınız değiştirilmedi.";return;}
   using(var driver=new NvDriver()){Engine.Apply(driver,readiness.CadExe,BackupRoot);summary.Text=L("HAZIR — CATIA performans ayarları yüklendi","READY — CATIA performance settings were applied");summary.BackColor=Color.FromArgb(31,124,91);details.Text+=Environment.NewLine+Environment.NewLine+L("✓ Güç yönetimi: Maksimum performans\n✓ Kare hızı sınırı: Kapalı\n✓ Dikey senkronizasyon: Kapalı\n✓ Yedek alındı ve sürücüden yeniden okunarak doğrulandı.","✓ Power management: Maximum performance\n✓ Frame rate limiter: Off\n✓ Vertical sync: Off\n✓ Backup was created and verified by reading it from the driver.");}
  }catch(Exception e){summary.Text="İŞLEM TAMAMLANAMADI — Ayarlar korunmuş olabilir";summary.BackColor=Color.FromArgb(133,53,54);details.Text+="\r\n\r\n"+e.Message;}
  finally{running=false;refresh.Enabled=true;UseWaitCursor=false;}
 }
 bool ConfirmFirstUse(){var answer=MessageBox.Show(this,@"CATIA GPU Tuner NVIDIA’nın CATIA uygulama profilinde üç ayarı değiştirecektir: maksimum performans, sınırsız kare hızı ve V-Sync kapalı. Değişiklikten önce yedek alınır ve sonuç doğrulanır.

Araç yalnızca desteklenen NVIDIA iş istasyonu GPU’larında kullanılmalıdır. Her iş istasyonu, sürücü ve CAD sürümü farklıdır; uygunluk ve üretim ortamındaki sonuç kullanıcı tarafından doğrulanmalıdır. Araca güvenmiyorsanız veya bu değişiklikleri kabul etmiyorsanız Devam Etmeyin.

Devam etmek, ayarları kendi sorumluluğunuzda uygulamayı kabul ettiğiniz anlamına gelir.","İlk kullanım güvenlik bildirimi",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2);if(answer!=DialogResult.Yes)return false;Directory.CreateDirectory(Path.GetDirectoryName(ConsentFile));File.WriteAllText(ConsentFile,"accepted");return true;}
 void ShowHelp(){if(!UiText.Turkish){MessageBox.Show(this,@"CATIA GPU Tuner checks the system at startup and applies settings on a supported computer. It never asks the user to choose an EXE or profile.

Supported GPUs
• NVIDIA RTX A series
• NVIDIA RTX PRO
• NVIDIA Quadro RTX

NVIDIA Control Panel, the NVIDIA driver API and an installed CATIA/3DEXPERIENCE profile are required. Nothing is written on GeForce RTX or unsupported systems.

Applied settings
• Power management: Maximum performance
• Frame rate limiter: Off
• Vertical sync: Off — screen tearing can occur.

The app preserves the NVIDIA Control Panel advanced-3D preference, creates a full backup and reads settings back from the driver. This is not a certification tool; validate results before production use.","Help",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}MessageBox.Show(this,@"CATIA GPU Tuner başlangıçta sistemi otomatik denetler ve desteklenen bilgisayarda ayarları yükler. Kullanıcıdan EXE veya profil seçmesi istenmez.

Desteklenen GPU’lar
• NVIDIA RTX A serisi
• NVIDIA RTX PRO
• NVIDIA Quadro RTX

NVIDIA Control Panel, NVIDIA sürücü API’si ve kurulu CATIA/3DEXPERIENCE NVIDIA profili zorunludur. GeForce RTX veya desteklenmeyen sistemde hiçbir ayar yazılmaz. Sürücü uyumluluğu/sertifikasyonu Dassault’un resmi sayfasından ayrıca doğrulanmalıdır.

Yüklenen üç ayar
• Güç yönetimi: Maksimum performans
• Kare hızı sınırı: Kapalı
• Dikey senkronizasyon: Kapalı — ekran yırtılması görülebilir.

Program, seçtiğiniz NVIDIA Control Panel ‘Use the advanced 3D image settings’ tercihini değiştirmez. Önce tam NVIDIA profil yedeği alınır; ayarlar sürücüden tekrar okunur. Yedekler LocalAppData/CatiaGpuTuner/Backups klasöründedir.

Bu bir benchmark ya da sertifikasyon aracı değildir. Antialiasing, Threaded Optimization, OpenGL GPU, ECC ve diğer üretici/CATIA profil ayarları korunur.

Güvenlik sınırı: Bu araç yalnızca desteklenen iş istasyonu GPU’larında kullanılmalıdır. Yedek ve doğrulama sağlansa da her bilgisayar, sürücü ve CAD sürümü farklıdır; sonuçları üretimde kullanmadan önce doğrulayın. Araca güvenmiyorsanız ayarları uygulamayın.","Yardım",MessageBoxButtons.OK,MessageBoxIcon.Information); }
}
}
