using System.Runtime.InteropServices;
using SharpDX;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  [StructLayout(LayoutKind.Sequential)]
  public struct SubtitleStyle
  {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string FontName;
    public int FontColor;
    [MarshalAs(UnmanagedType.Bool)]
    public bool FontIsBold;
    public int FontSize;
    public int FontCharset;
    public int ShadowDepth;
    public int BorderWidth;
    [MarshalAs(UnmanagedType.Bool)]
    public bool IsBorderOutline;

    public void Load()
    {
      FontName = "Arial";
      FontSize = 18;
      FontIsBold = true;
      FontCharset = 1; //default charset

      string strColor = "ffffff";
      int argb = int.Parse(strColor, System.Globalization.NumberStyles.HexNumber);

      //convert ARGB to BGR (COLORREF)
      FontColor = argb.ToBGR();
      ShadowDepth = 3;
      BorderWidth = 2;
      IsBorderOutline = true;
    }
  }

  public static class ColorExtension
  {
    public static int ToBGR(this int argb)
    {
      return (argb & 0x000000FF) << 16 | argb & 0x0000FF00 | (argb & 0x00FF0000) >> 16;
    }
    public static int ToBGR(this Color color)
    {
      return ((int)color).ToBGR();
    }
  }
}
