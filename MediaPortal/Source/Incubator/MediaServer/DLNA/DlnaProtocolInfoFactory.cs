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

using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.MediaServer.DLNA
{
  public enum ProtocolInfoFormat
  {
    DLNA,
    Simple
  }

  public class DlnaProtocolInfoFactory
  {
    public DlnaProtocolInfo GetProtocolInfo(DlnaMediaItem item, ProtocolInfoFormat infoLevel)
    {
      if (item.DlnaMime == null)
        return null;

      var info = new DlnaProtocolInfo
      {
        Protocol = "http-get",
        Network = "*",
        MediaType = item.DlnaMime,
        AdditionalInfo = new DlnaForthField()
      };
      bool live = false;
      if (item.TranscodingParameter is VideoTranscoding)
      {
        live = ((VideoTranscoding)item.TranscodingParameter).TargetIsLive;
      }
      else if (item.TranscodingParameter is AudioTranscoding)
      {
        live = ((AudioTranscoding)item.TranscodingParameter).TargetIsLive;
      }

      ConfigureProfile(info.AdditionalInfo, item, infoLevel, live);
      return info;
    }

    public DlnaProtocolInfo GetThumbnailProtocolInfo(string dlnaMime, string dlnaProfile)
    {
      if (dlnaMime == null)
        return null;

      var info = new DlnaProtocolInfo
      {
        Protocol = "http-get",
        Network = "*",
        MediaType = dlnaMime,
        AdditionalInfo = new DlnaForthField()
      };

      info.AdditionalInfo.ConversionParameter.Show = true;
      info.AdditionalInfo.ConversionParameter.ConvertedContent = true;

      info.AdditionalInfo.ProfileParameter.Show = true;
      info.AdditionalInfo.ProfileParameter.ProfileName = dlnaProfile;

      info.AdditionalInfo.OperationsParameter.Show = true;
      info.AdditionalInfo.OperationsParameter.TimeSeekRangeSupport = false;
      info.AdditionalInfo.OperationsParameter.ByteSeekRangeSupport = false;

      info.AdditionalInfo.FlagsParameter.Show = true;
      info.AdditionalInfo.FlagsParameter.SenderPaced = false;
      info.AdditionalInfo.FlagsParameter.TimeBasedSeek = false;
      info.AdditionalInfo.FlagsParameter.ByteBasedSeek = false;
      info.AdditionalInfo.FlagsParameter.PlayerContainer = false;
      info.AdditionalInfo.FlagsParameter.UcdamS0Increasing = false;
      info.AdditionalInfo.FlagsParameter.UcdamSnIncreasing = false;
      info.AdditionalInfo.FlagsParameter.RtspPauseOperation = false;
      info.AdditionalInfo.FlagsParameter.StreamingMode = false;
      info.AdditionalInfo.FlagsParameter.InteractiveMode = true;
      info.AdditionalInfo.FlagsParameter.BackgroundMode = true;
      info.AdditionalInfo.FlagsParameter.HttpConnectionStalling = false;
      info.AdditionalInfo.FlagsParameter.Dlna1Dot5Version = true;
      info.AdditionalInfo.FlagsParameter.LinkProtectedContent = false;
      info.AdditionalInfo.FlagsParameter.CleartextByteFullDataSeek = false;
      info.AdditionalInfo.FlagsParameter.CleartextLimitedDataSeek = false;

      info.AdditionalInfo.PlaySpeedParameter.Show = false;

      return info;
    }

    private static void ConfigureProfile(DlnaForthField dlnaField, DlnaMediaItem item, ProtocolInfoFormat infoLevel, bool live)
    {
      if (infoLevel == ProtocolInfoFormat.Simple)
      {
        dlnaField.ProfileParameter.Show = false;
        dlnaField.OperationsParameter.Show = false;
        dlnaField.FlagsParameter.Show = false;
        dlnaField.PlaySpeedParameter.Show = false;

        dlnaField.ProfileParameter.ProfileName = item.DlnaProfile;
      }
      else
      {
        dlnaField.ConversionParameter.Show = true;
        if (item.IsTranscoded)
        {
          dlnaField.ConversionParameter.ConvertedContent = true;
        }
        else
        {
          dlnaField.ConversionParameter.ConvertedContent = false;
        }

        dlnaField.ProfileParameter.Show = true;
        dlnaField.ProfileParameter.ProfileName = item.DlnaProfile;

        if (item.IsImage)
        {
          dlnaField.OperationsParameter.Show = true;
          dlnaField.OperationsParameter.TimeSeekRangeSupport = false;
          dlnaField.OperationsParameter.ByteSeekRangeSupport = false;

          dlnaField.FlagsParameter.Show = true;
          dlnaField.FlagsParameter.SenderPaced = false;
          dlnaField.FlagsParameter.TimeBasedSeek = false;
          dlnaField.FlagsParameter.ByteBasedSeek = false;
          dlnaField.FlagsParameter.PlayerContainer = false;
          dlnaField.FlagsParameter.UcdamS0Increasing = false;
          dlnaField.FlagsParameter.UcdamSnIncreasing = false;
          dlnaField.FlagsParameter.RtspPauseOperation = false;
          dlnaField.FlagsParameter.StreamingMode = false;
          dlnaField.FlagsParameter.InteractiveMode = true;
          dlnaField.FlagsParameter.BackgroundMode = true;
          dlnaField.FlagsParameter.HttpConnectionStalling = false;
          dlnaField.FlagsParameter.Dlna1Dot5Version = true;
          dlnaField.FlagsParameter.LinkProtectedContent = false;
          dlnaField.FlagsParameter.CleartextByteFullDataSeek = false;
          dlnaField.FlagsParameter.CleartextLimitedDataSeek = false;

          dlnaField.PlaySpeedParameter.Show = false;
        }
        else if (item.IsVideo || item.IsAudio)
        {
          dlnaField.OperationsParameter.Show = true;
          double duration = 0;
          if (item != null)
          {
            try
            {
              duration = item.DlnaMetadata.Metadata.Duration;
            }
            catch
            {
              duration = 0;
            }
          }
          if (duration > 0 && live == false)
          {
            dlnaField.OperationsParameter.TimeSeekRangeSupport = true;
            dlnaField.OperationsParameter.ByteSeekRangeSupport = true;
            dlnaField.FlagsParameter.TimeBasedSeek = true;
            dlnaField.FlagsParameter.ByteBasedSeek = true;
          }
          else
          {
            dlnaField.OperationsParameter.TimeSeekRangeSupport = false;
            dlnaField.OperationsParameter.ByteSeekRangeSupport = false;
            dlnaField.FlagsParameter.TimeBasedSeek = false;
            dlnaField.FlagsParameter.ByteBasedSeek = false;
          }
          if (live) dlnaField.FlagsParameter.SenderPaced = true;
          else dlnaField.FlagsParameter.SenderPaced = false;

          dlnaField.FlagsParameter.Show = true;
          dlnaField.FlagsParameter.SenderPaced = false;
          dlnaField.FlagsParameter.PlayerContainer = false;
          dlnaField.FlagsParameter.UcdamS0Increasing = false;
          if (item.IsTranscoded == true)
          {
            dlnaField.FlagsParameter.UcdamSnIncreasing = true;
          }
          else
          {
            dlnaField.FlagsParameter.UcdamSnIncreasing = false;
          }
          dlnaField.FlagsParameter.RtspPauseOperation = false;
          dlnaField.FlagsParameter.StreamingMode = true;
          dlnaField.FlagsParameter.InteractiveMode = false;
          dlnaField.FlagsParameter.BackgroundMode = true;
          dlnaField.FlagsParameter.HttpConnectionStalling = false;
          dlnaField.FlagsParameter.Dlna1Dot5Version = true;
          dlnaField.FlagsParameter.LinkProtectedContent = false;
          dlnaField.FlagsParameter.CleartextByteFullDataSeek = false;
          dlnaField.FlagsParameter.CleartextLimitedDataSeek = false;

          dlnaField.PlaySpeedParameter.Show = false;
        }
      }
    }

    public static DlnaProtocolInfo GetProfileInfo(DlnaMediaItem item, ProtocolInfoFormat infoLevel)
    {
      var factory = new DlnaProtocolInfoFactory();
      return factory.GetProtocolInfo(item, infoLevel);
    }

    public static DlnaProtocolInfo GetThumbnailProfileInfo(string dlnaMime, string dlnaProfile)
    {
      var factory = new DlnaProtocolInfoFactory();
      return factory.GetThumbnailProtocolInfo(dlnaMime, dlnaProfile);
    }
  }
}
