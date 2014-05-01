#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class SubtitleRendererV3 : SubtitleRenderer
  {
    #region Structs

    /// <summary>
    /// Structure used in communication with subtitle v3 filter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeSubtitleV3
    {
      // Start of bitmap fields
      public Int32 Type;
      public Int32 Width;
      public Int32 Height;
      public Int32 WidthBytes;
      public UInt16 Planes;
      public UInt16 BitsPixel;
      public IntPtr Bits;
      // End of bitmap fields

      // Start of screen size definition
      public Int32 ScreenWidth;
      public Int32 ScreenHeight;

      // Subtitle timestmap
      public UInt64 TimeStamp;

      // How long to display subtitle
      public UInt64 TimeOut; // in seconds
      public Int32 FirstScanLine;
      public Int32 HorizontalPosition;
    }

    #endregion

    #region Constructor

    public SubtitleRendererV3(Action onTextureInvalidated)
      : base (onTextureInvalidated)
    { }

    #endregion

    #region Overrides

    protected override string FilterName
    {
      get { return "MediaPortal DVBSub3"; }
    }

    #endregion

    #region Callback and event handling

    protected override Subtitle ToSubtitle(IntPtr nativeSubPtr)
    {
      NativeSubtitleV3 nativeSub = (NativeSubtitleV3)Marshal.PtrToStructure(nativeSubPtr, typeof(NativeSubtitleV3));
      Subtitle subtitle = new Subtitle
      {
        SubBitmap = new Bitmap(nativeSub.Width, nativeSub.Height, PixelFormat.Format32bppArgb),
        TimeOut = nativeSub.TimeOut,
        PresentTime = ((double)nativeSub.TimeStamp / 1000.0f) + _startPos,
        Height = (uint)nativeSub.Height,
        Width = (uint)nativeSub.Width,
        ScreenWidth = nativeSub.ScreenWidth,
        FirstScanLine = nativeSub.FirstScanLine,
        HorizontalPosition = nativeSub.HorizontalPosition,
        Id = _subCounter++
      };
      CopyBits(nativeSub.Bits, ref subtitle.SubBitmap, nativeSub.Width, nativeSub.Height, nativeSub.WidthBytes);
      return subtitle;
    }

    #endregion
  }
}
