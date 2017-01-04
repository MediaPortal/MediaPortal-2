#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using SharpDX;

namespace MediaPortal.UI.Players.Video.Settings
{
  public class MpcSubsSettings
  {
    [Setting(SettingScope.User, "Arial")]
    public string FontName { get; set; }

    [Setting(SettingScope.User, true)]
    public bool FontIsBold { get; set; }

    [Setting(SettingScope.User, 18)]
    public int FontSize { get; set; }

    [Setting(SettingScope.User, 1)]
    public int FontCharset { get; set; }

    [Setting(SettingScope.User, "ffffff")]
    public string FontColorInHex { get; set; }

    [Setting(SettingScope.User, 3)]
    public int ShadowDepth { get; set; }

    [Setting(SettingScope.User, 2)]
    public int BorderWidth { get; set; }

    [Setting(SettingScope.User, true)]
    public bool IsBorderOutline { get; set; }
  }
  

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
      MpcSubsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MpcSubsSettings>();
      FontName = settings.FontName;
      FontSize = settings.FontSize;
      FontIsBold = settings.FontIsBold;
      FontCharset = settings.FontCharset; //default charset

      int argb = int.Parse(settings.FontColorInHex, System.Globalization.NumberStyles.HexNumber);

      //convert ARGB to BGR (COLORREF)
      FontColor = argb.ToBGR();
      ShadowDepth = settings.ShadowDepth;
      BorderWidth = settings.BorderWidth;
      IsBorderOutline = settings.IsBorderOutline;
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
