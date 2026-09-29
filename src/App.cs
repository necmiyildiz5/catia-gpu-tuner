using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CatiaGpuTuner {
static class Program {
 [STAThread] static int Main(string[] args){
  if(args.Length==2&&args[0]=="--check"){try{using(var d=new NvDriver()){File.WriteAllText(args[1],"GPU: "+d.Gpus+"\r\nDriver: "+d.Version+"\r\nNVAPI: OK\r\nCertification: NOT VERIFIED\r\nRead only: no settings saved.\r\n");}return 0;}catch(Exception e){File.WriteAllText(args[1],e.ToString());return 1;}}
  bool created;using(var mutex=new System.Threading.Mutex(true,"Local\\CatiaGpuTuner-1",out created)){if(!created){bool acquired=false;if(args.Contains("--elevated")){try{acquired=mutex.WaitOne(5000);}catch(System.Threading.AbandonedMutexException){acquired=true;}}if(!acquired){MessageBox.Show("CATIA GPU Tuner zaten açık.");return 1;}}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());mutex.ReleaseMutex();}return 0;
 }
}
public sealed class MainForm:Form {
 TextBox exe=new TextBox(),release=new TextBox(),log=new TextBox();Label status=new Label();ComboBox mode=new ComboBox();CheckBox advanced=new CheckBox();DataGridView grid=new DataGridView();Button apply;TabControl tabs=new TabControl();bool busy;string lastProfile="";
 static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CatiaGpuTuner","Backups");
 public MainForm(){
  Text="CATIA GPU Tuner • 1.0.0";Size=new Size(1060,820);MinimumSize=new Size(920,720);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(244,247,251);AutoScaleMode=AutoScaleMode.None;
  var header=new Panel{Dock=DockStyle.Top,Height=104,BackColor=Color.FromArgb(20,33,52),Padding=new Padding(24)};
  header.Controls.Add(new Label{Text="CATIA GPU Tuner",Dock=DockStyle.Top,Height=36,ForeColor=Color.White,Font=new Font("Segoe UI",22,FontStyle.Bold)});
  header.Controls.Add(new Label{Text="NVIDIA RTX   /   Kontrol et · Yedekle · Uygula · Doğrula",Dock=DockStyle.Bottom,Height=24,ForeColor=Color.FromArgb(162,212,232)});
  tabs.Dock=DockStyle.Fill;tabs.Padding=new Point(18,8);Controls.Add(tabs);Controls.Add(header);
  var main=new TabPage("Performans"){BackColor=BackColor,Padding=new Padding(18)};tabs.TabPages.Add(main);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7};main.Controls.Add(layout);
  foreach(int height in new[]{68,48,46,40,110,0,68})layout.RowStyles.Add(new RowStyle(height==0?SizeType.Percent:SizeType.Absolute,height==0?100:height));
  var choose=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};choose.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,135));choose.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));choose.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,108));choose.Controls.Add(new Label{Text="CATIA uygulaması\nGerçek EXE dosyası",AutoSize=true});exe.Dock=DockStyle.Top;exe.ReadOnly=true;exe.Margin=new Padding(0,8,8,0);choose.Controls.Add(exe);var browse=Button("EXE seç",()=>{using(var f=new OpenFileDialog{Filter="CATIA uygulaması (*.exe)|*.exe",Title="Başlatıcı yerine kurulu CATIA EXE dosyasını seçin"})if(f.ShowDialog()==DialogResult.OK){exe.Text=f.FileName;InvalidatePreview();}});choose.Controls.Add(browse);layout.Controls.Add(choose);
  var options=new FlowLayoutPanel{Dock=DockStyle.Fill};options.Controls.Add(new Label{Text="Çalışma profili",AutoSize=true,Margin=new Padding(0,8,12,0)});mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.Width=265;mode.Items.AddRange(new object[]{"Dengeli CAD — önerilen başlangıç","Akıcı görünüm — V-Sync kapalı"});mode.SelectedIndex=0;mode.SelectedIndexChanged+=(s,e)=>InvalidatePreview();options.Controls.Add(mode);options.Controls.Add(new Label{Text="CATIA sürümü",AutoSize=true,Margin=new Padding(16,8,8,0)});release.Width=150;options.Controls.Add(release);layout.Controls.Add(options);
  advanced.Text="NVIDIA Control Panel: “Use the advanced 3D image settings” seçili ve Apply ile kaydedildi.";advanced.Dock=DockStyle.Fill;advanced.CheckedChanged+=(s,e)=>InvalidatePreview();layout.Controls.Add(advanced);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};buttons.Controls.Add(Button("Sistemi incele",()=>Run(Inspect)));apply=Button("Yedekle ve uygula",Apply);apply.Enabled=false;buttons.Controls.Add(apply);buttons.Controls.Add(Button("Yedekten geri al",Restore));buttons.Controls.Add(Button("Yedekleri aç",()=>{Directory.CreateDirectory(Root);Process.Start(Root);}));layout.Controls.Add(buttons);
  status.Dock=DockStyle.Fill;status.Padding=new Padding(10);status.BackColor=Color.FromArgb(229,237,245);status.Text="Başlamak için gerçek CATIA EXE dosyasını seçin. Sistem incelemesi ayar değiştirmez.\nSürücü sertifikasyonu için CATIA sürümü ve iş istasyonu eşleşmesi gerekir.";layout.Controls.Add(status);
  grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.BackgroundColor=Color.White;grid.BorderStyle=BorderStyle.None;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.Columns.Add("setting","Ayar");grid.Columns.Add("before","Mevcut değer");grid.Columns.Add("after","Önerilen değer");layout.Controls.Add(grid);
  layout.Controls.Add(new Label{Dock=DockStyle.Fill,Text="Yalnızca eşleşen CATIA uygulama profili düzenlenir. Global profil ve önizleme tercihi korunur.\nUygulamadan önce CATIA'yı kapatın. Sertifikasyon doğrulanmadıysa sonuç bir performans garantisi değildir.",Padding=new Padding(0,10,0,0),ForeColor=Color.FromArgb(65,80,100)});
  var help=new TabPage("Yardım ve sürücü"){Padding=new Padding(18),BackColor=BackColor};tabs.TabPages.Add(help);var helpText=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=BackColor,Text=Help()};var links=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=45};links.Controls.Add(Button("Dassault sertifikasyon",()=>Process.Start("https://www.3ds.com/support/hardware-and-software")));links.Controls.Add(Button("NVIDIA ayar rehberi",()=>Process.Start("https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm")));help.Controls.Add(helpText);help.Controls.Add(links);
  var logs=new TabPage("İşlem kaydı"){Padding=new Padding(12)};tabs.TabPages.Add(logs);log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Both;log.Dock=DockStyle.Fill;logs.Controls.Add(log);var export=Button("Raporu kaydet",()=>{using(var f=new SaveFileDialog{Filter="Metin raporu|*.txt",FileName="catia-gpu-tuner-report.txt"})if(f.ShowDialog()==DialogResult.OK)File.WriteAllText(f.FileName,log.Text);});export.Dock=DockStyle.Bottom;logs.Controls.Add(export);
  
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show("İşlem tamamlanana kadar bekleyin.");}};
 }
 Button Button(string text,Action action){var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(100,32),FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,10,0)};b.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"İşlem tamamlanamadı",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};return b;}
 void InvalidatePreview(){apply.Enabled=false;lastProfile="";grid.Rows.Clear();}
 void Write(string text){log.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+text+Environment.NewLine);}
 async void Run(Action action){if(busy)return;busy=true;tabs.Enabled=false;UseWaitCursor=true;try{await Task.Yield();action();}catch(Exception e){Write(e.Message);status.Text="İşlem tamamlanamadı: "+e.Message;MessageBox.Show(this,e.Message,"Kontrol sonucu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{busy=false;tabs.Enabled=true;UseWaitCursor=false;}}
 void Inspect(){using(var d=new NvDriver()){
  string machine="";try{using(var search=new ManagementObjectSearcher("SELECT Manufacturer,Model FROM Win32_ComputerSystem"))foreach(var m in search.Get())machine=m["Manufacturer"]+" "+m["Model"];}catch{machine="Model bilgisi okunamadı";}
  status.Text=d.Gpus+" | Sürücü "+d.Version+" | NVIDIA API erişimi başarılı\n"+machine+" | Sertifikasyon: doğrulanmadı (Yardım bölümünden kontrol edin)";
  Write(status.Text+" | CATIA sürümü: "+release.Text);if(String.IsNullOrWhiteSpace(exe.Text)){apply.Enabled=false;return;}
  lastProfile=d.Resolve(exe.Text);grid.Rows.Clear();foreach(var p in Engine.Plan(mode.SelectedIndex==1)){var v=d.Read(lastProfile,p.Id);grid.Rows.Add(p.Name,Engine.Label(p.Id,v),Engine.Label(p.Id,new ValueState{Exists=true,Value=p.Target}));}Write("Eşleşen sürücü profili: "+lastProfile);status.Text+="\nProfil: "+lastProfile;apply.Enabled=advanced.Checked;
 }}
 bool Admin(){if(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))return true;if(MessageBox.Show(this,"Ayarları kaydetmek için yönetici izni gerekir. Uygulama yönetici olarak yeniden açılsın mı? Seçimleri tekrar yapmanız gerekecek.","Yönetici izni",MessageBoxButtons.YesNo)==DialogResult.Yes){Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--elevated"){UseShellExecute=true,Verb="runas"});Close();}return false;}
 bool CatiaClosed(){string name=Path.GetFileNameWithoutExtension(exe.Text);if(!String.IsNullOrWhiteSpace(name)&&Process.GetProcessesByName(name).Length>0){MessageBox.Show("Önce CATIA'yı kapatın; ardından tekrar deneyin.");return false;}return true;}
 void Apply(){if(!advanced.Checked||String.IsNullOrWhiteSpace(lastProfile))return;if(!CatiaClosed()||!Admin())return;string summary=String.Join("\n",Engine.Plan(mode.SelectedIndex==1).Select(x=>x.Name+": "+Engine.Label(x.Id,new ValueState{Exists=true,Value=x.Target})));if(MessageBox.Show(this,"Profil: "+lastProfile+"\n\n"+summary+"\n\nÖnce yedek alınacak. Sertifikasyon doğrulanmış değildir. Uygulansın mı?","Ayarları uygula",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
  Run(()=>{using(var d=new NvDriver()){var dir=Engine.Apply(d,exe.Text,mode.SelectedIndex==1,Root);Write("Ayarlar kaydedildi ve yeniden okunarak doğrulandı. Yedek: "+dir);}Inspect();status.Text+="\nSONUÇ: Uygulandı ve doğrulandı. CATIA'yı yeniden açabilirsiniz.";});
 }
 void Restore(){using(var f=new OpenFileDialog{Filter="Tuner ayar yedeği|settings.json",InitialDirectory=Directory.Exists(Root)?Root:""}){if(f.ShowDialog()!=DialogResult.OK)return;var b=Engine.Load(f.FileName);if(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(b.Executable)).Length>0){MessageBox.Show("Önce CATIA'yı kapatın.");return;}if(!Admin())return;if(MessageBox.Show(this,"Profil: "+b.Profile+"\nYedek tarihi: "+b.CreatedUtc+"\nSürücü: "+b.Driver+"\n\nBu üç ayar işlem öncesindeki durumuna döndürülsün mü?","Geri al",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;string file=f.FileName;Run(()=>{using(var d=new NvDriver()){Write("Geri alma doğrulandı. İşlem öncesi yedek: "+Engine.Restore(d,file,Root));}InvalidatePreview();status.Text="Geri alma tamamlandı ve doğrulandı.";});}}
 static string Help(){return @"NASIL KULLANILIR?
1. NVIDIA Control Panel > Adjust image settings with preview:
   Use the advanced 3D image settings seçin ve Apply'a basın.
   Bu uygulama önizleme tercihini değiştirmez veya eski seçime döndürmez.
2. Başlatıcı yerine gerçek CATIA EXE dosyasını seçin (ör. CNEXT.exe).
   Sürücüdeki mevcut uygulama eşleştirmesi kullanılır. Uygun profil bulunamazsa yazma engellenir.
3. Sistemi incele ile GPU, sürücü, profil ve değişiklikleri görün.
4. CATIA'yı kapatın. Yedekle ve uygula ile kaydedin. Sonuç yeniden okunarak kontrol edilir.

SÜRÜCÜ UYGUN MU?
NVIDIA RTX algılaması, sürücü sürümü ve NVAPI erişimi otomatik kontrol edilir.
Bu, CATIA sertifikasyonu demek değildir. En yeni sürücü her zaman sertifikalı değildir.
Dassault sayfasında iş istasyonu modeli, GPU, Windows ve CATIA/3DEXPERIENCE sürümünü birlikte karşılaştırın. Bu uygulama canlı sertifikasyon veritabanı içermediğinden 'doğrulanmadı' sonucunu açıkça gösterir. Sürücü indirmez veya kurmaz.

UYGULANAN AYARLAR
Güç yönetimi: Maksimum performansı tercih et. GPU frekans geçişlerini azaltabilir; daha fazla ısı ve enerji tüketir. Orijinal adaptörle kullanım önerilir.
Kare hızı sınırı: Kapalı. Sürücünün FPS sınırını kaldırır.
Dikey senkronizasyon: Dengeli CAD uygulamaya bırakır. Akıcı görünüm kapatır; ekran yırtılması oluşabilir. Her iki profilin gerçek modelinizdeki farkını karşılaştırın.

KORUNAN ÖNEMLİ AYARLAR
Threaded optimization: Sürücünün CPU iş parçacıklarını kullanmasıyla ilgilidir; CATIA model hesaplamalarını otomatik çok çekirdekli yapmaz. Auto/On/Off etkisi modele göre değişir. Üretici profiliniz korunur.
Antialiasing: Çizgi kenarlarını yumuşatır; yüksek örnekleme GPU yükünü artırır. CATIA içinden ayarlayın.
OpenGL rendering GPU: OpenGL için ekran kartı seçimi. Birden fazla GPU varsa gerçek CATIA sürecinin RTX'i kullandığını kontrol edin. Bu sürüm GPU atamasını değiştirmez.
Vulkan/OpenGL present method: Görüntünün Windows'a sunulma yoludur. Evrensel en hızlı seçenek yoktur; mevcut tercih korunur.
Texture filtering: Dokuların görünümü ve maliyeti. Bazı seçenekler yalnızca DirectX'e etki eder; CAD geometrisi için evrensel hızlandırıcı değildir.
Shader cache: Derlenmiş shader'ları saklar. Sürücü değişiminden sonra ilk açılış daha yavaş olabilir. Mevcut ayar korunur.
ECC: Bellek hatası koruması. Bu uygulama ECC, overclock, voltaj veya termal limit değiştirmez.

YEDEK / GERİ ALMA
Her yazmadan önce tam DRS dışa aktarımı ve değiştirilecek ayarların JSON yedeği alınır. JSON için SHA-256 bütünlük kontrolü yapılır (dijital imza değildir).
Geri alma yalnızca bu aracın üç ayarını geri getirir. Önceden özel değer yoksa eklenen geçersiz kılma kaldırılır; tüm sürücü veritabanı eski dosyayla ezilmez.
Farklı sürücü/GPU yedeği otomatik geri alınmaz. Hata durumunda otomatik geri alma denenir; doğrulanamayan sonuç başarı diye gösterilmez.
Yedekler kullanıcı hesabının LocalAppData / CatiaGpuTuner / Backups klasöründedir.

SINIRLAR
Bu sürüm tek Windows x64 bilgisayarda yerel kullanım içindir. Profil birden fazla CATIA uygulamasınca paylaşılıyorsa hepsi etkilenebilir. İşlem sırasında başka profil düzenleyicilerini kullanmayın.
FPS ölçümü veya sertifikasyon testi değildir. Dosya açma, model güncelleme ve hesaplama darboğazları CPU, RAM, disk veya ağdan kaynaklanabilir.
Bağımsız, açık kaynak proje; Dassault Systèmes veya NVIDIA tarafından onaylanmış ürün değildir. Telemetri yoktur. İnternet yalnızca yardım bağlantısını siz açarsanız kullanılır.".Replace("\n",Environment.NewLine);}
}
}



