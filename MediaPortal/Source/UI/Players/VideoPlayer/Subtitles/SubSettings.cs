using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  [StructLayout(LayoutKind.Sequential)]
  public struct SubtitleStyle
  {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string fontName;
    public int fontColor;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fontIsBold;
    public int fontSize;
    public int fontCharset;
    public int shadow;
    public int borderWidth;
    [MarshalAs(UnmanagedType.Bool)]
    public bool isBorderOutline;

    public void Load()
    {
      fontName = "Arial";
      fontSize = 18;
      fontIsBold = true;
      fontCharset = 1; //default charset

      string strColor = "ffffff";
      int argb = Int32.Parse(strColor, System.Globalization.NumberStyles.HexNumber);
      //convert ARGB to BGR (COLORREF)
      fontColor = (int)((argb & 0x000000FF) << 16) |
                  (int)(argb & 0x0000FF00) |
                  (int)((argb & 0x00FF0000) >> 16);
      shadow = 3;
      borderWidth = 2;
      isBorderOutline = true;
    }
  }
}
