#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;


namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base.Metadata
{
  public class Metadata
  {
    internal void GetVideoDimensions(VideoTranscoding video, out Size newSize, out Size newContentSize, out float newPixelAspectRatio, out bool pixelARChanged, out bool videoARChanged, out bool videoHeightChanged)
    {
      newSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      newContentSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      newPixelAspectRatio = video.SourceVideoPixelAspectRatio;
      pixelARChanged = false;
      videoARChanged = false;
      videoHeightChanged = false;

      if (Checks.IsSquarePixelNeeded(video))
      {
        newSize.Width = Convert.ToInt32(Math.Round((double)video.SourceVideoWidth * video.SourceVideoPixelAspectRatio));
        newSize.Height = video.SourceVideoHeight;
        newContentSize.Width = newSize.Width;
        newContentSize.Height = newSize.Height;
        newPixelAspectRatio = 1;
        pixelARChanged = true;
      }
      if (Checks.IsVideoAspectRatioChanged(newSize.Width, newSize.Height, newPixelAspectRatio, video.TargetVideoAspectRatio) == true)
      {
        double sourceNewAspectRatio = (double)newSize.Width / (double)newSize.Height * video.SourceVideoAspectRatio;
        if (sourceNewAspectRatio < video.SourceVideoAspectRatio)
          newSize.Width = Convert.ToInt32(Math.Round((double)newSize.Height * video.TargetVideoAspectRatio / newPixelAspectRatio));
        else
          newSize.Height = Convert.ToInt32(Math.Round((double)newSize.Width * newPixelAspectRatio / video.TargetVideoAspectRatio));

        videoARChanged = true;
      }
      if (Checks.IsVideoHeightChangeNeeded(newSize.Height, video.TargetVideoMaxHeight) == true)
      {
        double oldWidth = newSize.Width;
        double oldHeight = newSize.Height;
        newSize.Width = Convert.ToInt32(Math.Round(newSize.Width * ((double)video.TargetVideoMaxHeight / (double)newSize.Height)));
        newSize.Height = video.TargetVideoMaxHeight;
        newContentSize.Width = Convert.ToInt32(Math.Round((double)newContentSize.Width * ((double)newSize.Width / oldWidth)));
        newContentSize.Height = Convert.ToInt32(Math.Round((double)newContentSize.Height * ((double)newSize.Height / oldHeight)));
        videoHeightChanged = true;
      }
      //Correct widths
      newSize.Width = ((newSize.Width + 1) / 2) * 2;
      newContentSize.Width = ((newContentSize.Width + 1) / 2) * 2;
    }

    public TranscodedAudioMetadata GetTranscodedAudioMetadata(AudioTranscoding audio)
    {
      TranscodedAudioMetadata metadata = new TranscodedAudioMetadata
      {
        TargetAudioBitrate = audio.TargetAudioBitrate,
        TargetAudioCodec = audio.TargetAudioCodec,
        TargetAudioContainer = audio.TargetAudioContainer,
        TargetAudioFrequency = audio.TargetAudioFrequency
      };
      if (Checks.IsAudioStreamChanged(audio))
      {
        long frequency = Validators.GetAudioFrequency(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioFrequency, audio.TargetAudioFrequency);
        if (frequency > 0)
        {
          metadata.TargetAudioFrequency = frequency;
        }
        if (audio.TargetAudioContainer != AudioContainer.Lpcm)
        {
          metadata.TargetAudioBitrate = Validators.GetAudioBitrate(audio.SourceAudioBitrate, audio.TargetAudioBitrate);
        }
      }
      metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(audio.SourceAudioCodec, audio.TargetAudioCodec, audio.SourceAudioChannels, audio.TargetForceAudioStereo);
      return metadata;
    }

    public TranscodedImageMetadata GetTranscodedImageMetadata(ImageTranscoding image)
    {
      TranscodedImageMetadata metadata = new TranscodedImageMetadata
      {
        TargetMaxHeight = image.SourceHeight,
        TargetMaxWidth = image.SourceWidth,
        TargetOrientation = image.SourceOrientation,
        TargetImageCodec = image.TargetImageCodec
      };
      if (metadata.TargetImageCodec == ImageContainer.Unknown)
      {
        metadata.TargetImageCodec = image.SourceImageCodec;
      }
      metadata.TargetPixelFormat = image.TargetPixelFormat;
      if (metadata.TargetPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetPixelFormat = image.SourcePixelFormat;
      }
      if (Checks.IsImageStreamChanged(image) == true)
      {
        metadata.TargetMaxHeight = image.SourceHeight;
        metadata.TargetMaxWidth = image.SourceWidth;
        if (metadata.TargetMaxHeight > image.TargetHeight && image.TargetHeight > 0)
        {
          double scale = (double)image.SourceWidth / (double)image.SourceHeight;
          metadata.TargetMaxHeight = image.TargetHeight;
          metadata.TargetMaxWidth = Convert.ToInt32(scale * (double)metadata.TargetMaxHeight);
        }
        if (metadata.TargetMaxWidth > image.TargetWidth && image.TargetWidth > 0)
        {
          double scale = (double)image.SourceHeight / (double)image.SourceWidth;
          metadata.TargetMaxWidth = image.TargetWidth;
          metadata.TargetMaxHeight = Convert.ToInt32(scale * (double)metadata.TargetMaxWidth);
        }

        if (image.TargetAutoRotate == true)
        {
          if (image.SourceOrientation > 4)
          {
            int iTemp = metadata.TargetMaxWidth;
            metadata.TargetMaxWidth = metadata.TargetMaxHeight;
            metadata.TargetMaxHeight = iTemp;
          }
          metadata.TargetOrientation = 0;
        }
      }
      return metadata;
    }

    public TranscodedVideoMetadata GetTranscodedVideoMetadata(VideoTranscoding video)
    {
      TranscodedVideoMetadata metadata = new TranscodedVideoMetadata
      {
        TargetAudioBitrate = video.TargetAudioBitrate,
        TargetAudioCodec = video.TargetAudioCodec,
        TargetAudioFrequency = video.TargetAudioFrequency,
        TargetVideoFrameRate = video.SourceFrameRate,
        TargetLevel = video.TargetLevel,
        TargetPreset = video.TargetPreset,
        TargetProfile = video.TargetProfile,
        TargetVideoPixelFormat = video.TargetPixelFormat
      };
      if (metadata.TargetVideoPixelFormat == PixelFormat.Unknown)
      {
        metadata.TargetVideoPixelFormat = PixelFormat.Yuv420;
      }
      metadata.TargetVideoAspectRatio = video.TargetVideoAspectRatio;
      if (metadata.TargetVideoAspectRatio <= 0)
      {
        metadata.TargetVideoAspectRatio = 16.0F / 9.0F;
      }
      metadata.TargetVideoBitrate = video.TargetVideoBitrate;
      metadata.TargetVideoCodec = video.TargetVideoCodec;
      if (metadata.TargetVideoCodec == VideoCodec.Unknown)
      {
        metadata.TargetVideoCodec = video.SourceVideoCodec;
      }
      metadata.TargetVideoContainer = video.TargetVideoContainer;
      metadata.TargetVideoTimestamp = Timestamp.None;
      if (video.TargetVideoContainer == VideoContainer.M2Ts)
      {
        metadata.TargetVideoTimestamp = Timestamp.Valid;
      }

      metadata.TargetVideoMaxWidth = video.SourceVideoWidth;
      metadata.TargetVideoMaxHeight = video.SourceVideoHeight;
      if (metadata.TargetVideoMaxHeight <= 0)
      {
        metadata.TargetVideoMaxHeight = 1080;
      }
      float newPixelAspectRatio = video.SourceVideoPixelAspectRatio;
      if (newPixelAspectRatio <= 0)
      {
        newPixelAspectRatio = 1.0F;
      }

      Size newSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      Size newContentSize = new Size(video.SourceVideoWidth, video.SourceVideoHeight);
      bool pixelARChanged = false;
      bool videoARChanged = false;
      bool videoHeightChanged = false;
      GetVideoDimensions(video, out newSize, out newContentSize, out newPixelAspectRatio, out pixelARChanged, out videoARChanged, out videoHeightChanged);
      metadata.TargetVideoPixelAspectRatio = newPixelAspectRatio;
      metadata.TargetVideoMaxWidth = newSize.Width;
      metadata.TargetVideoMaxHeight = newSize.Height;

      metadata.TargetVideoFrameRate = video.SourceFrameRate;
      if (metadata.TargetVideoFrameRate > 23.9 && metadata.TargetVideoFrameRate < 23.99)
        metadata.TargetVideoFrameRate = 23.976F;
      else if (metadata.TargetVideoFrameRate >= 23.99 && metadata.TargetVideoFrameRate < 24.1)
        metadata.TargetVideoFrameRate = 24;
      else if (metadata.TargetVideoFrameRate >= 24.99 && metadata.TargetVideoFrameRate < 25.1)
        metadata.TargetVideoFrameRate = 25;
      else if (metadata.TargetVideoFrameRate >= 29.9 && metadata.TargetVideoFrameRate < 29.99)
        metadata.TargetVideoFrameRate = 29.97F;
      else if (metadata.TargetVideoFrameRate >= 29.99 && metadata.TargetVideoFrameRate < 30.1)
        metadata.TargetVideoFrameRate = 30;
      else if (metadata.TargetVideoFrameRate >= 49.9 && metadata.TargetVideoFrameRate < 50.1)
        metadata.TargetVideoFrameRate = 50;
      else if (metadata.TargetVideoFrameRate >= 59.9 && metadata.TargetVideoFrameRate < 59.99)
        metadata.TargetVideoFrameRate = 59.94F;
      else if (metadata.TargetVideoFrameRate >= 59.99 && metadata.TargetVideoFrameRate < 60.1)
        metadata.TargetVideoFrameRate = 60;

      metadata.TargetAudioChannels = Validators.GetAudioNumberOfChannels(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioChannels, video.TargetForceAudioStereo);
      long frequency = Validators.GetAudioFrequency(video.SourceAudioCodec, video.TargetAudioCodec, video.SourceAudioFrequency, video.TargetAudioFrequency);
      if (frequency != -1)
      {
        metadata.TargetAudioFrequency = frequency;
      }
      if (video.TargetAudioCodec != AudioCodec.Lpcm)
      {
        metadata.TargetAudioBitrate = Validators.GetAudioBitrate(video.SourceAudioBitrate, video.TargetAudioBitrate);
      }
      if (video.TargetSubtitleSupport == SubtitleSupport.SoftCoded)
      {
        if (video.SourceSubtitles.Count > 0)
        {
          metadata.TargetSubtitled = true;
        }
        else
        {
          metadata.TargetSubtitled = IsExternalSubtitleAvaialable(video);
        }
      }
      return metadata;
    }

    private bool IsExternalSubtitleAvaialable(VideoTranscoding video)
    {
      if (video.SourceFile is ILocalFsResourceAccessor)
      {
        ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)video.SourceFile;
        if (lfsra.Exists)
        {
          // Impersonation
          using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(lfsra.CanonicalLocalResourcePath))
          {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(lfsra.LocalFileSystemPath), Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + "*.*");
            foreach (string file in files)
            {
              if (string.Compare(Path.GetExtension(file), ".srt", true, CultureInfo.InvariantCulture) == 0)
              {
                return true;
              }
              else if (string.Compare(Path.GetExtension(file), ".smi", true, CultureInfo.InvariantCulture) == 0)
              {
                return true;
              }
              else if (string.Compare(Path.GetExtension(file), ".ass", true, CultureInfo.InvariantCulture) == 0)
              {
                return true;
              }
              else if (string.Compare(Path.GetExtension(file), ".ssa", true, CultureInfo.InvariantCulture) == 0)
              {
                return true;
              }
              else if (string.Compare(Path.GetExtension(file), ".sub", true, CultureInfo.InvariantCulture) == 0)
              {
                return true;
              }
            }
          }
        }
      }
      return false;
    }
  }
}
