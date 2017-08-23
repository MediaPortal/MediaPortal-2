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

using System;
using System.Runtime.InteropServices;

namespace MediaPortal.UI.Players.Video.Interfaces
{
  /// <summary>
  /// ChangedMediaType is used to indicated what media types have been changed. Combinations of values are possible.
  /// </summary>
  [Flags]
  public enum ChangedMediaType
  {
    None = 0,
    Audio = 1,
    Video = 2,
  }

  /// <summary>
  /// Structure for callbacks by TsReader.
  /// </summary>
  [ComVisible(true), ComImport,
   Guid("324FAA1F-4DA6-47B8-832B-3993D8FF4151"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ITsReaderCallback
  {
    [PreserveSig]
    int OnMediaTypeChanged(ChangedMediaType mediaType);

    [PreserveSig]
    int OnVideoFormatChanged(int streamType, int width, int height, int aspectRatioX, int aspectRatioY, int bitrate,
                             int isInterlaced);

    [PreserveSig]
    int OnBitRateChanged(int bitrate);
  }

  /// <summary>
  /// Structure for callbacks by TsReader.
  /// </summary>
  [ComVisible(true), ComImport,
   Guid("324FAA1F-4DA6-47B8-832B-3993D8FF4152"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ITsReaderCallbackAudioChange
  {
    [PreserveSig]
    int OnRequestAudioChange();
  }

  /// <summary>
  /// Structure to pass the subtitle language data from TsReader to this class
  /// </summary>
  /// 
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct SubtitleLanguage
  {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
    public string lang;
  }
  /// <summary>
  /// Interface to the TsReader filter wich provides information about the 
  /// subtitle streams and allows us to change the current subtitle stream
  /// </summary>
  /// 
  [Guid("43FED769-C5EE-46aa-912D-7EBDAE4EE93A"),
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ISubtitleStream
  {
    void SetSubtitleStream(Int32 stream);
    void GetSubtitleStreamType(Int32 stream, ref Int32 type);
    void GetSubtitleStreamCount(ref Int32 count);
    void GetCurrentSubtitleStream(ref Int32 stream);
    void GetSubtitleStreamLanguage(Int32 stream, ref SubtitleLanguage szLanguage);
  }

  [Guid("b9559486-E1BB-45D3-A2A2-9A7AFE49B24F"),
   InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface ITsReader
  {
    [PreserveSig]
    int SetTsReaderCallback(ITsReaderCallback callback);

    [PreserveSig]
    int SetRequestAudioChangeCallback(ITsReaderCallbackAudioChange callback);

    [PreserveSig]
    int SetRelaxedMode(int relaxedReading);

    [PreserveSig]
    void OnZapping(int info);

    [PreserveSig]
    void OnGraphRebuild(ChangedMediaType info);

    [PreserveSig]
    void SetMediaPosition(long mediaPos);
  }
}
