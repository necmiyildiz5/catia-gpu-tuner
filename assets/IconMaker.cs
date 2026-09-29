using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
class IconMaker {
 static void Main(){
  using(var b=new Bitmap(64,64)) using(var g=Graphics.FromImage(b)){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   g.Clear(Color.FromArgb(19,27,39));
   using(var p=new Pen(Color.FromArgb(67,206,182),3)){
    g.DrawRectangle(p,8,8,48,48);
    g.DrawLine(p,15,45,31,17);g.DrawLine(p,31,17,49,45);g.DrawLine(p,15,45,49,45);
   }
   using(var brush=new SolidBrush(Color.FromArgb(53,124,218))) g.FillEllipse(brush,new Rectangle(25,25,13,13));
   using(var p=new Pen(Color.White,3)){g.DrawLine(p,27,32,31,36);g.DrawLine(p,31,36,40,25);}
   IntPtr h=b.GetHicon(); using(var ico=Icon.FromHandle(h)) using(var fs=File.Create("assets\\CATIA-GPU-Tuner.ico")){ico.Save(fs);} Native.DeleteObject(h);
  }
 }
 static class Native{[System.Runtime.InteropServices.DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);}
}


