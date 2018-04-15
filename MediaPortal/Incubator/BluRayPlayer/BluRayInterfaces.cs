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

namespace MediaPortal.UI.Players.Video
{
  [ComVisible(true), ComImport, Guid("324FAA1F-4DA6-47B8-832B-3993D8FF4151"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IBDReaderCallback
  {
    // FIXME: incosistent type formats, here integer, inside BDStreamInfo byte!!!
    [PreserveSig]
    int OnMediaTypeChanged(BluRayAPI.VideoRate videoRate, BluRayAPI.BluRayStreamFormats videoFormat, BluRayAPI.BluRayStreamFormats audioFormat);

    [PreserveSig]
    int OnBDevent([Out] BluRayAPI.BluRayEvent bluRayEvent);

    [PreserveSig]
    int OnOSDUpdate([Out] BluRayAPI.OSDTexture osdInfo);

    [PreserveSig]
    int OnClockChange([Out] long duration, [Out] long position);
  }

  [Guid("79A37017-3178-4859-8079-ECB9D546FEC2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  public interface IBDReader
  {
    [PreserveSig]
    int SetBDReaderCallback(IBDReaderCallback callback);

    [PreserveSig]
    int Action(int key);

    [PreserveSig]
    int SetAngle(int angle);

    [PreserveSig]
    int SetChapter(uint chapter);

    [PreserveSig]
    int GetAngle(ref int angle);

    [PreserveSig]
    int GetChapter(ref uint chapter);

    [PreserveSig]
    int GetTitleCount(ref uint count);

    [PreserveSig]
    IntPtr GetTitleInfo(int index);

    [PreserveSig]
    int GetCurrentClipStreamInfo(ref BluRayAPI.BDStreamInfo stream);

    [PreserveSig]
    int FreeTitleInfo(IntPtr info);

    [PreserveSig]
    int OnGraphRebuild(BluRayAPI.ChangedMediaType info);

    [PreserveSig]
    int ForceTitleBasedPlayback(bool force, UInt32 title);

    [PreserveSig]
    int SetD3DDevice(IntPtr d3dDevice);

    [PreserveSig]
    int SetBDPlayerSettings(BluRayAPI.BDPlayerSettings settings);

    [PreserveSig]
    int Start();

    [PreserveSig]
    int MouseMove(int x, int y);

    [PreserveSig]
    int SetVideoDecoder(BluRayAPI.BluRayStreamFormats format, ref Guid decoder);

    [PreserveSig]
    int SetVC1Override(ref Guid decoder);

    [PreserveSig]
    int GetAudioChannelCount(long index);
  }

  [ComImport, Guid(BluRayAPI.BDREADER_GUID)]
  public class BDReader { }
}
