using System;
using System.Globalization;
namespace CatiaGpuTuner {
 public static class UiText {
  public static readonly bool Turkish=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr",StringComparison.OrdinalIgnoreCase);
  public static string L(string tr,string en){return Turkish?tr:en;}
 }
}
