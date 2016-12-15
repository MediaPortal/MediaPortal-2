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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UI.Players.Video
{
  public class BluRayAPI
  {
    public const string BDREADER_FILTER_NAME = "MediaPortal BD Reader";
    public const string BDREADER_GUID = "79A37017-3178-4859-8079-ECB9D546FEB2";

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    public struct BDPlayerSettings
    {
      public int RegionCode;
      public int ParentalControl;
      public int AudioType;
      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string AudioLanguage;
      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string MenuLanguage;
      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string SubtitleLanguage;
      [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string CountryCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BluRayEvent
    {
      public BDEvents Event;
      public int Param;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnmanagedBDTitleInfo
    {
      public UInt32 Index;
      public UInt32 Playlist;
      public UInt64 Duration;
      public UInt32 ClipCount;
      public byte AngleCount;
      public UInt32 ChapterCount;
      public IntPtr Clips;
      public IntPtr Chapters;
      public UInt32 MarkCount;
      public IntPtr Marks;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BDMark
    {
      public UInt32 Index;
      public Int32 Type;
      public UInt64 Start;
      public UInt64 Duration;
      public UInt64 Offset;
      public byte ClipRef;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BDTitleInfo
    {
      public UInt32 Index;
      public UInt32 Playlist;
      public UInt64 Duration;
      public byte AngleCount;
      public BDClipInfo[] Clips;
      public BDChapter[] Chapters;
      public override string ToString()
      {
        return string.Format("Title {0} (Clips: {1}, Chapters: {2}, Angles: {3}, Duration: {4})", Index, Clips.Length,
                             Chapters.Length, AngleCount, Duration);
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BDChapter
    {
      public UInt32 Index;
      public UInt64 Start;
      public UInt64 Duration;
      public UInt64 Offset;
      public UInt16 ClipRef;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnmanagedBDClipInfo
    {
      public UInt32 PktCount;
      public StillModeType StillMode;
      public UInt16 StillTime;  /* seconds */
      public byte VideoStreamCount;
      public byte AudioStreamCount;
      public byte PgStreamCount;
      public byte IgStreamCount;
      public byte SecAudioStreamCount;
      public byte SecVideoStreamCount;
      public byte RawStreamCount;

      // FIXME: if the builtin marshalling would work with arrays of BDStreamInfo structs, we could save 
      // a lot of manual marshalling!

      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 3)]
      //public BDStreamInfo[] video_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 4)]
      //public BDStreamInfo[] audio_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 5)]
      //public BDStreamInfo[] pg_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 6)]
      //public BDStreamInfo[] ig_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 7)]
      //public BDStreamInfo[] sec_audio_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 8)]
      //public BDStreamInfo[] sec_video_streams;
      //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.ByValArray, SizeParamIndex = 9)]
      //public BDStreamInfo[] raw_streams;

      public IntPtr /*BDStreamInfo* */ VideoStreams;
      public IntPtr /*BDStreamInfo* */ AudioStreams;
      public IntPtr /*BDStreamInfo* */ PgStreams;
      public IntPtr /*BDStreamInfo* */ IgStreams;
      public IntPtr /*BDStreamInfo* */ SecAudioStreams;
      public IntPtr /*BDStreamInfo* */ SecVideoStreams;
      public IntPtr /*BDStreamInfo* */ RawStreams;
      public UInt64 StartTime;
      public UInt64 InTime;
      public UInt64 OutTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BDClipInfo
    {
      public UInt32 PktCount;
      public StillModeType StillMode;
      public UInt16 StillTime;  /* seconds */

      public BDStreamInfo[] VideoStreams;
      public BDStreamInfo[] AudioStreams;
      public BDStreamInfo[] PgStreams;
      public BDStreamInfo[] IgStreams;
      public BDStreamInfo[] SecAudioStreams;
      public BDStreamInfo[] SecVideoStreams;
      public BDStreamInfo[] RawStreams;

      public override string ToString()
      {
        return string.Format("Clip (V: {0}, A: {1}, Still: {2}, Pkt: {3})", VideoStreams.Length, AudioStreams.Length, StillMode, PktCount);
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BDStreamInfo
    {
      public BluRayStreamFormats CodingType;
      public VideoFormat Format;
      public VideoRate Rate;
      public CharCodeType CharCode;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
      public string Lang;
      public UInt16 Pid;
      public AspectRatio Aspect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OSDTexture
    {
      public OverlayPlane Plane;
      public int Width;
      public int Height;
      public int X;
      public int Y;
      public IntPtr Texture; // IDirect3DTexture9
    }

    #endregion

    #region Enums

    public enum PlayState
    {
      Init,
      Playing,
      Paused,
      Ended
    }

    public enum MenuState
    {
      None,
      Root,
      RootPending,
      PopUp
    }

    [Flags]
    public enum MenuItems
    {
      None = 0,
      MainMenu = 1,
      PopUpMenu = 2,
      Chapter = 4,
      Audio = 8,
      Subtitle = 16,
      All = 255
    }

    public enum StillModeType : byte
    {
      None = 0x00,
      Time = 0x01,
      Infinite = 0x02,
    }

    public static Dictionary<Key, BDKeys> KeyMapping =
      new Dictionary<Key, BDKeys>
        {
          {Key.Left, BDKeys.BD_VK_LEFT},
          {Key.Right, BDKeys.BD_VK_RIGHT},
          {Key.Up, BDKeys.BD_VK_UP},
          {Key.Down, BDKeys.BD_VK_DOWN},
          {Key.Enter, BDKeys.BD_VK_ENTER},
          {Key.Ok, BDKeys.BD_VK_ENTER},
          {Key.DVDMenu, BDKeys.BD_VK_ROOT_MENU},
          {Key.Info, BDKeys.BD_VK_POPUP},
          // TODO: what to do with numeric keys?
        };

    // ReSharper disable InconsistentNaming
    public enum BDKeys
    {
      /* numeric key events */
      BD_VK_0 = 0,
      BD_VK_1 = 1,
      BD_VK_2 = 2,
      BD_VK_3 = 3,
      BD_VK_4 = 4,
      BD_VK_5 = 5,
      BD_VK_6 = 6,
      BD_VK_7 = 7,
      BD_VK_8 = 8,
      BD_VK_9 = 9,

      BD_VK_ROOT_MENU = 10,  /* open root menu */
      BD_VK_POPUP = 11,  /* toggle popup menu */

      /* interactive key events */
      BD_VK_UP = 12,
      BD_VK_DOWN = 13,
      BD_VK_LEFT = 14,
      BD_VK_RIGHT = 15,
      BD_VK_ENTER = 16,

      /* Mouse click */
      /* Translated to BD_VK_ENTER if mouse is over valid button */
      BD_VK_MOUSE_ACTIVATE = 17,
    }
    // ReSharper restore InconsistentNaming

    public enum BDEvents
    {
      None = 0,
      Error = 1,     /* Fatal error. Playback can't be continued. */
      ReadError = 2, /* Reading of .m2ts aligned unit failed. Next call to read will try next block. */
      Encrypted = 3, /* .m2ts file is encrypted and can't be played */

      /* current playback position */
      Angle = 4,     /* current angle, 1...N */
      Title = 5,     /* current title, 1...N (0 = top menu) */
      Playlist = 6,  /* current playlist (xxxxx.mpls) */
      Playitem = 7,  /* current play item */
      Chapter = 8,   /* current chapter, 1...N */
      Playmark = 30, /* playmark reached */
      EndOfTitle = 9,

      /* stream selection */
      AudioStream = 10,          /* 1..32,  0xff  = none */
      IgStream = 11,             /* 1..32                */
      PgTextStream = 12,         /* 1..255, 0xfff = none */
      PipPgTextStream = 13,      /* 1..255, 0xfff = none */
      SecondaryAudioStream = 14, /* 1..32,  0xff  = none */
      SecondaryVideoStream = 15, /* 1..32,  0xff  = none */

      PgText = 16,               /* 0 - disable, 1 - enable */
      PipPgText = 17,            /* 0 - disable, 1 - enable */
      SecondaryAudio = 18,       /* 0 - disable, 1 - enable */
      SecondaryVideo = 19,       /* 0 - disable, 1 - enable */
      VideoSize = 20,            /* 0 - PIP, 0xf - fullscreen */

      /* discontinuity in the stream (non-seamless connection). Reset demuxer PES buffers. */
      Discontinuity = 28,  /* new timestamp (45 kHz) */

      /* HDMV VM or JVM seeked the stream. Next read() will return data from new position. */
      Seek = 21,

      /* still playback (pause) */
      Still = 22,                  /* 0 - off, 1 - on */

      /* Still playback for n seconds (reached end of still mode play item) */
      StillTime = 23,             /* 0 = infinite ; 1...300 = seconds */
      SoundEffect = 24,           /* effect ID */

      /* Nothing to do. Playlist is not playing, but title applet is running. */
      Idle = 29,
      Popup = 25,                 /* 0 - no, 1 - yes */
      Menu = 26,                  /* 0 - no, 1 - yes */
      StereoscopicStatus = 27,    /* 0 - 2D, 1 - 3D */

      CustomEventMenuVisibility = 1000  /* 0 - not shown, 1 shown*/
    }

    public enum BluRayStreamFormats : byte
    {
      Unknown = 0,
      VideoMPEG1 = 0x01,
      VideoMPEG2 = 0x02,
      AudioMPEG1 = 0x03,
      AudioMPEG2 = 0x04,
      AudioLPCM = 0x80,
      AudioAc3 = 0x81,
      AudioDts = 0x82,
      AudioTruHd = 0x83,
      AudioAc3Plus = 0x84,
      TypeAudioDtsHd = 0x85,
      AudioDtsHdMaster = 0x86,
      VideoVc1 = 0xea,
      VideoH264 = 0x1b,
      SubPg = 0x90,
      SubIg = 0x91,
      SubText = 0x92,
      Ac3PlusSecondary = 0xa1,
      DtsHdSecondary = 0xa2
    }

    // ReSharper disable InconsistentNaming
    public enum VideoRate : byte
    {
      Rate_24000_1001 = 1,  // 23.976
      Rate_24 = 2,
      Rate_25 = 3,
      Rate_30000_1001 = 4,  // 29.97
      Rate_50 = 6,
      Rate_60000_1001 = 7   // 59.94
    }
    // ReSharper restore InconsistentNaming

    public enum VideoFormat : byte
    {
      Format480I = 1,  // ITU-R BT.601-5
      Format576I = 2,  // ITU-R BT.601-4
      Format480P = 3,  // SMPTE 293M
      Format1080I = 4,  // SMPTE 274M
      Format720P = 5,  // SMPTE 296M
      Format1080P = 6,  // SMPTE 274M
      Format576P = 7   // ITU-R BT.1358
    }

    // ReSharper disable InconsistentNaming
    public enum AspectRatio : byte
    {
      Ratio_4_3 = 2,
      Ratio_16_9 = 3
    }
    // ReSharper restore InconsistentNaming

    public enum CharCodeType : byte
    {
      Utf8 = 0x01,
      Utf16Be = 0x02,
      ShiftJis = 0x03,
      EucKr = 0x04,
      // ReSharper disable InconsistentNaming
      GB18030_20001 = 0x05,
      // ReSharper restore InconsistentNaming
      CnGb = 0x06,
      Big5 = 0x07
    }

    [Flags]
    public enum ChangedMediaType
    {
      None = 0,
      Video = 1,
      Audio = 2
    }

    public enum BluRayTitle : uint
    {
      Current = 0xffffffff,
      FirstPlay = 0xffff,
      TopMenu = 0
    }

    public enum OverlayPlane : byte
    {
      Presentation = 0,  /* Presentation Graphics plane */
      Interactive = 1,  /* Interactive Graphics plane (on top of PG plane) */
    }


    #endregion
  }
}
