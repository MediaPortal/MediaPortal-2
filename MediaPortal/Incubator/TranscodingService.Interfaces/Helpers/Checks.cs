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
using System.Linq;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Helpers
{
  public class Checks
  {
    public static bool IsVideoDimensionChanged(VideoTranscoding video)
    {
      return IsVideoHeightChangeNeeded(video.FirstSourceVideoStream.Height, video.TargetVideoMaxHeight) ||
        IsVideoAspectRatioChanged(video.FirstSourceVideoStream.Width, video.FirstSourceVideoStream.Height, video.FirstSourceVideoStream.PixelAspectRatio, video.TargetVideoAspectRatio) ||
        IsSquarePixelNeeded(video);
    }

    public static bool IsVideoHeightChangeNeeded(int? newHeight, int? targetMaximumHeight)
    {
      return (newHeight > 0 && targetMaximumHeight > 0 && newHeight > targetMaximumHeight);
    }

    public static bool IsSquarePixelNeeded(VideoTranscoding video)
    {
      bool squarePixels = IsSquarePixel(video.FirstSourceVideoStream.PixelAspectRatio);
      return (video.TargetVideoContainer == VideoContainer.Asf || video.TargetVideoContainer == VideoContainer.Flv) && squarePixels == false;
    }

    public static bool IsVideoAspectRatioChanged(int? newWidth, int? newHeight, double? pixelAspectRatio, double? targetAspectRatio)
    {
      if (!targetAspectRatio.HasValue || !pixelAspectRatio.HasValue)
        return false;
      return targetAspectRatio > 0 && newWidth > 0 && newHeight > 0 &&
        (Math.Round(targetAspectRatio.Value, 2, MidpointRounding.AwayFromZero) != Math.Round(pixelAspectRatio.Value * (double)newWidth / (double)newHeight, 2, MidpointRounding.AwayFromZero));
    }

    public static bool IsSquarePixel(double? pixelAspectRatio)
    {
      if (!pixelAspectRatio.HasValue)
      {
        return true;
      }
      return Math.Abs(1.0 - pixelAspectRatio.Value) < 0.01;
    }

    public static bool AreMultipleAudioStreamsSupported(VideoTranscoding video)
    {
      if (!video.TargetAudioMultiTrackSupport)
        return false;

      if (video.TargetVideoContainer == VideoContainer.Avi)
        return true;
      if (video.TargetVideoContainer == VideoContainer.Matroska)
        return true;
      if (video.TargetVideoContainer == VideoContainer.Mpeg2Ts)
        return true;
      if (video.TargetVideoContainer == VideoContainer.Mp4)
        return true;
      if (video.TargetVideoContainer == VideoContainer.Asf)
        return true;

      return false;
    }

    public static bool IsVideoStreamChanged(VideoTranscoding video)
    {
      bool notChanged = true;
      notChanged &= video.TargetForceVideoTranscoding == false;
      notChanged &= (video.TargetSubtitleSupport == SubtitleSupport.None || video.PreferredSourceSubtitles.Any() == false || video.TargetSubtitleSupport != SubtitleSupport.HardCoded);
      notChanged &= (video.TargetVideoCodec == VideoCodec.Unknown || video.TargetVideoCodec == video.FirstSourceVideoStream.Codec);
      notChanged &= IsVideoDimensionChanged(video) == false;
      notChanged &= video.TargetVideoBitrate <= 0;

      return notChanged == false;
    }

    public static bool IsMPEGTSContainer(VideoContainer container)
    {
      return container == VideoContainer.Mpeg2Ts || container == VideoContainer.Wtv || container == VideoContainer.Hls || container == VideoContainer.M2Ts;
    }

    public static bool IsAudioStreamChanged(int mediaStreamIndex, int audioStreamIndex, BaseTranscoding media)
    {
      AudioCodec sourceCodec = AudioCodec.Unknown;
      AudioCodec targetCodec = AudioCodec.Unknown;
      long? sourceBitrate = 0;
      long? targetBitrate = 0;
      long? sourceFrequency = 0;
      long? targetFrequency = 0;
      if (media is VideoTranscoding)
      {
        VideoTranscoding video = (VideoTranscoding)media;
        sourceCodec = video.SourceAudioStreams[mediaStreamIndex].First(s => s.StreamIndex == audioStreamIndex).Codec;
        sourceBitrate = video.SourceAudioStreams[mediaStreamIndex].First(s => s.StreamIndex == audioStreamIndex).Bitrate;
        sourceFrequency = video.SourceAudioStreams[mediaStreamIndex].First(s => s.StreamIndex == audioStreamIndex).Frequency;
        targetCodec = video.TargetAudioCodec;
        targetBitrate = video.TargetAudioBitrate;
        targetFrequency = video.TargetAudioFrequency;
      }
      if (media is AudioTranscoding)
      {
        AudioTranscoding audio = (AudioTranscoding)media;
        sourceCodec = audio.SourceAudioCodec;
        sourceBitrate = audio.SourceAudioBitrate;
        sourceFrequency = audio.SourceAudioFrequency;
        targetCodec = audio.TargetAudioCodec;
        targetBitrate = audio.TargetAudioBitrate;
        targetFrequency = audio.TargetAudioFrequency;
      }

      bool notChanged = true;
      notChanged &= (sourceCodec != AudioCodec.Unknown && targetCodec != AudioCodec.Unknown && sourceCodec == targetCodec);
      notChanged &= (sourceBitrate > 0 && targetBitrate > 0 && sourceBitrate == targetBitrate);
      notChanged &= (sourceFrequency > 0 && targetFrequency > 0 && sourceFrequency == targetFrequency);

      return notChanged == false;
    }

    public static bool IsImageStreamChanged(ImageTranscoding image)
    {
      bool notChanged = true;
      notChanged &= (image.SourceOrientation == 0 || image.TargetAutoRotate == false);
      notChanged &= (image.SourceHeight > 0 && image.SourceHeight <= image.TargetHeight);
      notChanged &= (image.SourceWidth > 0 && image.SourceWidth <= image.TargetWidth);
      notChanged &= (image.TargetPixelFormat == PixelFormat.Unknown || image.SourcePixelFormat == image.TargetPixelFormat);
      notChanged &= (image.TargetImageCodec == ImageContainer.Unknown && image.SourceImageCodec == image.TargetImageCodec);

      return notChanged == false;
    }
  }
}
