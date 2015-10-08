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

using System;
using System.Collections.Generic;
using MediaPortal.Common.ResourceAccess;

// TODO: get this from the Transcoding Plugin once it is it's own plugin.

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS
{
  #region Enums

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
    Vtt,
    DvbSub,
    DvbTxt
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

  public enum EncoderHandler
  {
    Software,
    HardwareIntel,
    HardwareNvidia
  }

  #endregion

  #region Classes

  #region Transcoding

  public abstract class BaseTranscoding
  {
    public string TranscodeId = "";
    public IResourceAccessor SourceFile;
    public string TranscoderBinPath = "";
    public string TranscoderArguments = "";
  }

  public class VideoTranscoding : BaseTranscoding
  {
    //Source info
    public int SourceVideoStreamIndex = -1;
    public int SourceAudioStreamIndex = -1;
    public TimeSpan SourceDuration = new TimeSpan(0);
    public VideoContainer SourceVideoContainer = VideoContainer.Unknown;
    public VideoCodec SourceVideoCodec = VideoCodec.Unknown;
    public AudioCodec SourceAudioCodec = AudioCodec.Unknown;
    public PixelFormat SourcePixelFormat = PixelFormat.Yuv420;
    public long SourceVideoBitrate = -1;
    public long SourceAudioBitrate = -1;
    public int SourceAudioChannels = -1;
    public long SourceAudioFrequency = -1;
    public float SourceVideoAspectRatio = -1;
    public float SourceVideoPixelAspectRatio = -1;
    public float SourceFrameRate = -1;
    public int SourceVideoHeight = -1;
    public int SourceVideoWidth = -1;
    public SubtitleStream SourceSubtitle = null;

    //Target info
    public VideoContainer TargetVideoContainer = VideoContainer.Unknown;
    public VideoCodec TargetVideoCodec = VideoCodec.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public int TargetVideoMaxHeight = -1;
    public long TargetAudioFrequency = -1;
    public float TargetVideoAspectRatio = -1;
    public long TargetVideoBitrate = -1;
    public long TargetAudioBitrate = -1;
    public QualityMode TargetVideoQuality = QualityMode.Default;
    public int TargetVideoQualityFactor = -1;
    public int TargetQualityFactor = -1;
    public EncodingPreset TargetPreset = EncodingPreset.Default;
    public EncodingProfile TargetProfile = EncodingProfile.Baseline;
    public PixelFormat TargetPixelFormat = PixelFormat.Yuv420;
    public float TargetLevel = -1;
    public Coder TargetCoder = Coder.Default;
    public bool TargetForceVideoTranscoding = false;
    public bool TargetForceAudioStereo = false;
    public SubtitleSupport TargetSubtitleSupport = SubtitleSupport.None;
    public SubtitleCodec TargetSubtitleCodec = SubtitleCodec.Srt;
    public string Movflags = null;
  }

  public class ImageTranscoding : BaseTranscoding
  {
    //Source info
    public ImageContainer SourceImageCodec = ImageContainer.Unknown;
    public PixelFormat SourcePixelFormat = PixelFormat.Unknown;
    public int SourceHeight = -1;
    public int SourceWidth = -1;
    public int SourceOrientation = -1;

    //Target info
    public bool TargetAutoRotate = true;
    public int TargetHeight = -1;
    public int TargetWidth = -1;
    public ImageContainer TargetImageCodec = ImageContainer.Jpeg;
    public PixelFormat TargetPixelFormat = PixelFormat.Unknown;
    public QualityMode TargetImageQuality = QualityMode.Default;
    public int TargetImageQualityFactor = -1;
    public Coder TargetCoder = Coder.Default;
  }

  public class AudioTranscoding : BaseTranscoding
  {
    //Source info
    public TimeSpan SourceDuration = new TimeSpan(0);
    public AudioContainer SourceAudioContainer = AudioContainer.Unknown;
    public AudioCodec SourceAudioCodec = AudioCodec.Unknown;
    public long SourceAudioBitrate = -1;
    public int SourceAudioChannels = -1;
    public long SourceAudioFrequency = -1;

    //Target info
    public AudioContainer TargetAudioContainer = AudioContainer.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public long TargetAudioFrequency = -1;
    public long TargetAudioBitrate = -1;
    public bool TargetForceAudioStereo = false;
    public Coder TargetCoder = Coder.Default;
  }

  public class TranscodedVideoMetadata
  {
    public VideoContainer TargetVideoContainer = VideoContainer.Unknown;
    public VideoCodec TargetVideoCodec = VideoCodec.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public int TargetVideoMaxHeight = -1;
    public int TargetVideoMaxWidth = -1;
    public long TargetAudioFrequency = -1;
    public float TargetVideoAspectRatio = -1;
    public long TargetVideoBitrate = -1;
    public long TargetAudioBitrate = -1;
    public int TargetAudioChannels = -1;
    public float TargetVideoPixelAspectRatio = -1;
    public EncodingPreset TargetPreset = EncodingPreset.Default;
    public EncodingProfile TargetProfile = EncodingProfile.Baseline;
    public PixelFormat TargetVideoPixelFormat = PixelFormat.Yuv420;
    public float TargetLevel = -1;
    public float TargetVideoFrameRate = -1;
    public Timestamp TargetVideoTimestamp = Timestamp.None;
  }

  public class TranscodedImageMetadata
  {
    public int TargetMaxHeight = -1;
    public int TargetMaxWidth = -1;
    public ImageContainer TargetImageCodec = ImageContainer.Jpeg;
    public PixelFormat TargetPixelFormat = PixelFormat.Unknown;
    public int TargetOrientation = -1;
  }

  public class TranscodedAudioMetadata
  {
    public AudioContainer TargetAudioContainer = AudioContainer.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public long TargetAudioFrequency = -1;
    public long TargetAudioBitrate = -1;
    public int TargetAudioChannels = -1;
  }

  #endregion

  #region Analysis

  public class MetadataContainer
  {
    public MetadataStream Metadata = new MetadataStream();
    public ImageStream Image = new ImageStream();
    public VideoStream Video = new VideoStream();
    public List<AudioStream> Audio = new List<AudioStream>();
    public List<SubtitleStream> Subtitles = new List<SubtitleStream>();

    public bool IsImage
    {
      get
      {
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("image/"))
        {
          return true;
        }
        if (Audio.Count > 0)
        {
          return false;
        }
        if (Metadata.ImageContainerType != ImageContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }

    public bool IsAudio
    {
      get
      {
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("audio/"))
        {
          return true;
        }
        if (Metadata.AudioContainerType != AudioContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }

    public bool IsVideo
    {
      get
      {
        if (Metadata.Mime != null && Metadata.Mime.StartsWith("video/"))
        {
          return true;
        }
        if (Metadata.VideoContainerType != VideoContainer.Unknown)
        {
          return true;
        }
        return false;
      }
    }
  }

  public class SubtitleStream
  {
    public SubtitleCodec Codec;
    public int StreamIndex;
    public string Language;
    public string Source;
    public bool Default;

    public bool IsEmbedded
    {
      get
      {
        if (StreamIndex >= 0) return true;
        return false;
      }
    }
  }

  public class AudioStream
  {
    public AudioCodec Codec;
    public int StreamIndex;
    public string Language;
    public long Frequency;
    public int Channels;
    public long Bitrate;
    public bool Default;
  }

  public class VideoStream
  {
    public VideoCodec Codec;
    public string FourCC;
    public int StreamIndex;
    public string Language;
    public float AspectRatio;
    public float PixelAspectRatio;
    public PixelFormat PixelFormatType;
    public long Bitrate;
    public float Framerate;
    public EncodingProfile ProfileType;
    public float HeaderLevel;
    public float RefLevel;
    public Timestamp TimestampType;
    public int Width;
    public int Height;

    public bool HasSquarePixels
    {
      get
      {
        if (PixelAspectRatio == 0)
        {
          return true;
        }
        return Math.Abs(1.0 - PixelAspectRatio) < 0.01;
      }
    }
  }

  public class ImageStream
  {
    public PixelFormat PixelFormatType;
    public int Width;
    public int Height;
    public int Orientation;
  }

  public class MetadataStream
  {
    public AudioContainer AudioContainerType;
    public VideoContainer VideoContainerType;
    public ImageContainer ImageContainerType;
    public long Size;
    public long Bitrate;
    public double Duration;
    public IResourceAccessor Source;
    public string Mime;
    public string MajorBrand;
  }

  #endregion

  #endregion
}
