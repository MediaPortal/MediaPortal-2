#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.Transcoding.Service
{
  public enum QualityMode
  {
    Default,
    Best,
    Normal,
    Low,
    Custom
  }

  public enum Coder
  {
    Default = -1,
    None,
    VariableLength,
    Arithmic,
    Raw,
    RunLength,
    Deflate
  }

  public enum SubtitleSupport
  {
    None,
    SoftCoded,
    Embedded,
    HardCoded
  }

  public enum EncodingPreset
  {
    Default,
    Ultrafast,
    Superfast,
    Veryfast,
    Faster,
    Fast,
    Medium,
    Slow,
    Slower,
    Veryslow,
    Placebo
  }

  public enum EncodingProfile
  {
    Unknown,
    Simple,
    Baseline,
    Main,
    Main10,
    High,
    High10,
    High422,
    High444
  }

  public enum AudioContainer
  {
    Unknown,
    Ac3,
    Adts,
    Ape,
    Asf,
    Flac,
    Flv,
    Lpcm,
    Mp4,
    Mp3,
    Mp2,
    MusePack,
    Ogg,
    Rtp,
    Rtsp,
    WavPack
  }

  public enum AudioCodec
  {
    Unknown,
    Aac,
    Ac3,
    Amr,
    Dts,
    DtsHd,
    Flac,
    Lpcm,
    Mp3,
    Mp2,
    Mp1,
    Real,
    TrueHd,
    Vorbis,
    Wma,
    WmaPro,
    WmaLossless,
    Alac,
    Speex,
    Ape
  }

  public enum ImageContainer
  {
    Unknown,
    Bmp,
    Gif,
    Jpeg,
    Png,
    Raw
  }

  public enum PixelFormat
  {
    Unknown,
    Yuv411,
    Yuv420,
    Yuv422,
    Yuv440,
    Yuv444
  }

  public enum SubtitleCodec
  {
    Unknown,
    Ass,
    Ssa,
    MovTxt,
    Smi,
    Srt,
    MicroDvd,
    SubView,
    WebVtt,
    DvbSub,
    DvbTxt,
    VobSub
  }

  public enum VideoCodec
  {
    Unknown,
    DvVideo,
    Flv,
    H265,
    H264,
    H263,
    Mpeg4,
    MsMpeg4,
    Mpeg2,
    Mpeg1,
    MJpeg,
    Real,
    Theora,
    Vc1,
    Vp6,
    Vp7,
    Vp8,
    Vp9,
    Wmv
  }

  public enum VideoContainer
  {
    Unknown,
    Asf,
    Avi,
    Flv,
    Gp3,
    Hls,
    Matroska,
    Mp4,
    M2Ts,
    Mpeg2Ps,
    Mpeg2Ts,
    Mpeg1,
    MJpeg,
    Ogg,
    RealMedia,
    Rtp,
    Rtsp,
    Wtv
  }

  public enum Timestamp
  {
    None,
    Zeros,
    Valid
  }

  public enum LevelCheck
  {
    Any,
    RefFramesLevel,
    HeaderLevel
  }

  public enum EncoderHandler
  {
    Software,
    HardwareIntel,
    HardwareNvidia
  }
}
