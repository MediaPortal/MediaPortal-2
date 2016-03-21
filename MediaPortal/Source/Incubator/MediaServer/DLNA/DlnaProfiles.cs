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
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MediaServer.DLNA
{
  public static class DlnaProfiles
  {
    public static List<string> ResolveImageProfile(ImageContainer container, int width, int height)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == ImageContainer.Jpeg)
      {
        if (width == 0 || height == 0)
        {
          valuesProfiles.Add("JPEG_LRG");
        }
        else if (width == 48 && height == 48)
        {
          valuesProfiles.Add("JPEG_SM_ICO");
          valuesProfiles.Add("JPEG_TN");
        }
        else if (width == 120 && height == 120)
        {
          valuesProfiles.Add("JPEG_LRG_ICO");
          valuesProfiles.Add("JPEG_TN");
        }
        else if (width <= 160 && height <= 160)
        {
          valuesProfiles.Add("JPEG_TN");
        }
        else if (width <= 160 && height <= 160)
        {
          valuesProfiles.Add("JPEG_TN");
        }
        else if (width <= 640 && height <= 480)
        {
          valuesProfiles.Add("JPEG_SM");
        }
        else if (width <= 1024 && height <= 768)
        {
          valuesProfiles.Add("JPEG_MED");
        }
        else
        {
          valuesProfiles.Add("JPEG_LRG");
        }
      }
      else if (container == ImageContainer.Png)
      {
        if (width == 0 || height == 0)
        {
          valuesProfiles.Add("PNG_LRG");
        }
        else if (width == 48 && height == 48)
        {
          valuesProfiles.Add("PNG_SM_ICO");
          valuesProfiles.Add("PNG_TN");
        }
        else if (width == 120 && height == 120)
        {
          valuesProfiles.Add("PNG_LRG_ICO");
          valuesProfiles.Add("PNG_TN");
        }
        else if (width <= 160 && height <= 160)
        {
          valuesProfiles.Add("PNG_TN");
        }
        else
        {
          valuesProfiles.Add("PNG_LRG");
        }
      }
      else if (container == ImageContainer.Gif)
      {
        valuesProfiles.Add("GIF_LRG");
      }
      else if (container == ImageContainer.Raw)
      {
        valuesProfiles.Add("RAW");
      }
      else
      {
        throw new Exception("Image does not match any supported DLNA profile");
      }
      return valuesProfiles;
    }

    public static List<string> ResolveAudioProfile(AudioContainer container, AudioCodec audioCodec, long bitrate, long frequency, int channels)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == AudioContainer.Ac3)
      {
        valuesProfiles.Add("AC3");
      }
      else if (container == AudioContainer.Asf)
      {
        if (audioCodec == AudioCodec.WmaPro)
        {
          valuesProfiles.Add("WMA_PRO");
        }
        else if (bitrate != 0 && bitrate < 193)
        {
          valuesProfiles.Add("WMA_BASE");
        }
        else
        {
          valuesProfiles.Add("WMA_FULL");
        }
      }
      else if (container == AudioContainer.Mp3)
      {
        if (frequency < 32000)
        {
          valuesProfiles.Add("MP3X");
        }
        else
        {
          valuesProfiles.Add("MP3");
        }
      }
      else if (container == AudioContainer.Mp2)
      {
        valuesProfiles.Add("MP2_MPS");
      }
      else if (container == AudioContainer.Lpcm)
      {
        if (frequency != 0 && channels != 0)
        {
          if (frequency == 44100 && channels == 1)
          {
            valuesProfiles.Add("LPCM16_44_MONO");
          }
          else if (frequency == 44100 && channels == 2)
          {
            valuesProfiles.Add("LPCM16_44_STEREO");
          }
          else if (frequency == 48000 && channels == 1)
          {
            valuesProfiles.Add("LPCM16_48_MONO");
          }
          else if (frequency == 48000 && channels == 2)
          {
            valuesProfiles.Add("LPCM16_48_STEREO");
          }
          else
          {
            throw new Exception("Unsupported LPCM format. Only 44.1 / 48 kHz and Mono / Stereo formats are allowed.");
          }
        }
        else
        {
          valuesProfiles.Add("LPCM16_48_STEREO");
        }
      }
      else if (container == AudioContainer.Mp4)
      {
        //AAC stereo only
        if (bitrate != 0 && bitrate <= 320)
        {
          valuesProfiles.Add("AAC_ISO_320");
        }
        else
        {
          valuesProfiles.Add("AAC_ISO");
        }
      }
      else if (container == AudioContainer.Adts)
      {
        //AAC stereo only
        if (bitrate != 0 && bitrate <= 320)
        {
          valuesProfiles.Add("AAC_ADTS_320");
        }
        else
        {
          valuesProfiles.Add("AAC_ADTS");
        }
      }
      else if (container == AudioContainer.Flac)
      {
        valuesProfiles.Add("FLAC");
      }
      else if (container == AudioContainer.Ogg)
      {
        valuesProfiles.Add("OGG");
      }
      else if (container == AudioContainer.Rtsp)
      {
        valuesProfiles.Add("RTSP");
      }
      else if (container == AudioContainer.Rtp)
      {
        valuesProfiles.Add("RTP");
      }
      else
      {
        throw new Exception("Audio does not match any supported DLNA profile");
      }
      return valuesProfiles;
    }

    public static List<string> ResolveVideoProfile(VideoContainer container, VideoCodec videoCodec, AudioCodec audioCodec, EncodingProfile h264Profile, float h264Level, float fps, int width, int height, long videoBitrate, long audioBitrate, Timestamp timestampType)
    {
      List<string> valuesProfiles = new List<string>();
      if (container == VideoContainer.Asf)
      {
        if ((videoCodec == VideoCodec.Wmv && audioCodec == AudioCodec.Unknown) ||
          (videoCodec == VideoCodec.Wmv && audioCodec == AudioCodec.Mp3) ||
          (audioCodec == AudioCodec.Wma || audioCodec == AudioCodec.WmaPro))
        {
          if (width <= 176 && height <= 144)
          {
            if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Wma)
            {
              if (audioBitrate != 0 && audioBitrate < 193)
              {
                valuesProfiles.Add("WMVSPLL_BASE");

                //Fallback
                valuesProfiles.Add("WMVMED_BASE");
              }
              else
              {
                valuesProfiles.Add("WMVMED_FULL");
              }
            }
            else if (audioCodec == AudioCodec.Mp3)
            {
              valuesProfiles.Add("WMVSPLL_MP3");
            }
            else
            {
              valuesProfiles.Add("WMVMED_PRO");
            }
          }
          else if (width <= 352 && height <= 288)
          {
            if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Wma)
            {
              if (audioBitrate != 0 && audioBitrate < 193)
              {
                valuesProfiles.Add("WMVSPML_BASE");

                //Fallback
                valuesProfiles.Add("WMVMED_BASE");
              }
              else
              {
                valuesProfiles.Add("WMVMED_FULL");
              }
            }
            else if (audioCodec == AudioCodec.Mp3)
            {
              valuesProfiles.Add("WMVSPML_MP3");
            }
            else
            {
              valuesProfiles.Add("WMVMED_PRO");
            }
          }
          else if (width <= 720 && height <= 576)
          {
            if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Wma)
            {
              if (audioBitrate != 0 && audioBitrate < 193)
              {
                valuesProfiles.Add("WMVMED_BASE");
              }
              else
              {
                valuesProfiles.Add("WMVMED_FULL");
              }
            }
            else
            {
              valuesProfiles.Add("WMVMED_PRO");
            }
          }
          else if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Wma)
          {
            if (audioBitrate != 0 && audioBitrate < 193)
            {
              valuesProfiles.Add("WMVHIGH_BASE");
            }
            else
            {
              valuesProfiles.Add("WMVHIGH_FULL");
            }
          }
          else
          {
            valuesProfiles.Add("WMVHIGH_PRO");
          }
        }
        else if (videoCodec == VideoCodec.Vc1)
        {
          if (width <= 720 && height <= 576)
          {
            valuesProfiles.Add("VC1_ASF_AP_L1_WMA");
          }
          else if (width <= 1280 && height <= 720)
          {
            valuesProfiles.Add("VC1_ASF_AP_L2_WMA");
          }
          else if (width <= 1920 && height <= 1080)
          {
            valuesProfiles.Add("VC1_ASF_AP_L3_WMA");
          }
        }
        else if (videoCodec == VideoCodec.Mpeg1 || videoCodec == VideoCodec.Mpeg2)
        {
          valuesProfiles.Add("DVR_MS");
        }
        else
        {
          throw new Exception("ASF video file does not match any supported DLNA profile");
        }
      }
      else if (container == VideoContainer.Avi)
      {
        valuesProfiles.Add("AVI");
      }
      else if (container == VideoContainer.Matroska)
      {
        valuesProfiles.Add("MATROSKA");
      }
      else if (container == VideoContainer.Mp4)
      {
        bool isHD = false;
        if (Convert.ToInt32(width) > 720 || Convert.ToInt32(height) > 576)
        {
          isHD = true;
        }
        long bitrate = 0;
        if (videoBitrate > 0 && audioBitrate > 0)
        {
          bitrate = videoBitrate + audioBitrate;
        }

        if (videoCodec == VideoCodec.H264)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Ac3)
          {
            if (h264Profile == EncodingProfile.Baseline)
            {
              if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
              {
                valuesProfiles.Add("AVC_MP4_BL_CIF30_AC3");
              }
            }

            if (isHD)
            {
              valuesProfiles.Add("AVC_MP4_MP_HD_AC3");
            }

            //Main profile and fallback
            valuesProfiles.Add("AVC_MP4_MP_SD_AC3");
          }
          else if (audioCodec == AudioCodec.Mp3)
          {
            valuesProfiles.Add("AVC_MP4_MP_SD_MPEG1_L3");
          }
          else if (audioCodec == AudioCodec.Lpcm)
          {
            valuesProfiles.Add("AVC_MP4_LPCM");
          }
          if (isHD == false)
          {
            if (audioCodec == AudioCodec.Aac)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  if (bitrate > 0 && bitrate <= 520)
                  {
                    valuesProfiles.Add("AVC_MP4_BL_CIF30_AAC_520");
                  }
                  else if (bitrate > 0 && bitrate <= 940)
                  {
                    valuesProfiles.Add("AVC_MP4_BL_CIF30_AAC_940");
                  }

                  //Fallback
                  valuesProfiles.Add("AVC_MP4_BL_CIF30_AAC_MULT5");
                }
                else
                {
                  if (h264Level == 3.0 && bitrate > 0 && bitrate <= 5000)
                  {
                    valuesProfiles.Add("AVC_MP4_BL_L3L_SD_AAC");
                  }
                  else if (h264Level <= 3.1 && bitrate > 0 && bitrate <= 15000)
                  {
                    valuesProfiles.Add("AVC_MP4_BL_L31_HD_AAC");
                  }
                }
              }

              //Main profile and fallback
              valuesProfiles.Add("AVC_MP4_MP_SD_AAC_MULT5");
            }
            else if (audioCodec == AudioCodec.Dts)
            {
              valuesProfiles.Add("AVC_MP4_MP_SD_DTS");
            }
            else if (audioCodec == AudioCodec.DtsHd)
            {
              valuesProfiles.Add("AVC_MP4_MP_SD_DTSHD");
            }
          }
          else if (Convert.ToInt32(width) <= 1280 && Convert.ToInt32(height) <= 720)
          {
            if (audioCodec == AudioCodec.Aac)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (h264Level <= 3.1 && bitrate > 0 && bitrate <= 15000)
                {
                  valuesProfiles.Add("AVC_MP4_BL_L31_HD_AAC");
                }
                else if (h264Level <= 3.2 && bitrate > 0 && bitrate <= 21000)
                {
                  valuesProfiles.Add("AVC_MP4_BL_L32_HD_AAC");
                }
              }

              //Main profile and fallback
              valuesProfiles.Add("AVC_MP4_MP_HD_720p_AAC");
            }
            else if (audioCodec == AudioCodec.Dts)
            {
              valuesProfiles.Add("AVC_MP4_HP_HD_DTS");
            }
            else if (audioCodec == AudioCodec.DtsHd)
            {
              valuesProfiles.Add("AVC_MP4_HP_HD_DTSHD");
            }
          }
          else if (Convert.ToInt32(width) <= 1920 && Convert.ToInt32(height) <= 1080)
          {
            if (audioCodec == AudioCodec.Aac)
            {
              if (h264Profile == EncodingProfile.High || h264Profile == EncodingProfile.High10 || h264Profile == EncodingProfile.High422 || h264Profile == EncodingProfile.High444)
              {
                valuesProfiles.Add("AVC_MP4_HP_HD_AAC");
              }

              //Main profile and fallback
              valuesProfiles.Add("AVC_MP4_MP_HD_1080i_AAC");
            }
            else if (audioCodec == AudioCodec.Dts)
            {
              valuesProfiles.Add("AVC_MP4_HP_HD_DTS");
            }
            else if (audioCodec == AudioCodec.DtsHd)
            {
              valuesProfiles.Add("AVC_MP4_HP_HD_DTSHD");
            }
          }
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          if (Convert.ToInt32(width) <= 720 && Convert.ToInt32(height) <= 576)
          {
            if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac)
            {
              valuesProfiles.Add("MPEG4_P2_MP4_ASP_AAC");
            }
            else if (audioCodec == AudioCodec.Ac3 || audioCodec == AudioCodec.Mp3)
            {
              valuesProfiles.Add("MPEG4_P2_MP4_NDSD");
            }
          }
          else if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac)
          {
            valuesProfiles.Add("MPEG4_P2_MP4_SP_L6_AAC");
          }
        }
        else if (videoCodec == VideoCodec.H263 && audioCodec == AudioCodec.Aac)
        {
          valuesProfiles.Add("MPEG4_H263_MP4_P0_L10_AAC");
        }
        else
        {
          throw new Exception("MP4 video file does not match any supported DLNA profile");
        }
      }
      else if (container == VideoContainer.Mpeg2Ps)
      {
        valuesProfiles.Add("MPEG_PS_PAL");
        valuesProfiles.Add("MPEG_PS_NTSC");
      }
      else if (container == VideoContainer.Mpeg1)
      {
        valuesProfiles.Add("MPEG1");
      }
      else if (container == VideoContainer.Mpeg2Ts || container == VideoContainer.M2Ts)
      {
        bool isHD = false;
        if (Convert.ToInt32(width) > 720 || Convert.ToInt32(height) > 576)
        {
          isHD = true;
        }

        if (videoCodec == VideoCodec.Mpeg2)
        {
          if (isHD)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG_TS_HD_EU_ISO");
              valuesProfiles.Add("MPEG_TS_HD_NA_ISO");
              valuesProfiles.Add("MPEG_TS_HD_KO_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG_TS_HD_EU_T");
              valuesProfiles.Add("MPEG_TS_HD_NA_T");
              valuesProfiles.Add("MPEG_TS_HD_KO_T");
              if (audioCodec == AudioCodec.Aac)
              {
                valuesProfiles.Add("MPEG_TS_JP_T");
              }
            }
            else
            {
              valuesProfiles.Add("MPEG_TS_HD_EU");
              valuesProfiles.Add("MPEG_TS_HD_NA");
              valuesProfiles.Add("MPEG_TS_HD_KO");
            }
          }
          else
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG_TS_SD_EU_ISO");
              valuesProfiles.Add("MPEG_TS_SD_NA_ISO");
              valuesProfiles.Add("MPEG_TS_SD_KO_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG_TS_SD_EU_T");
              valuesProfiles.Add("MPEG_TS_SD_NA_T");
              valuesProfiles.Add("MPEG_TS_SD_KO_T");
              if (audioCodec == AudioCodec.Aac)
              {
                valuesProfiles.Add("MPEG_TS_JP_T");
              }
            }
            else
            {
              valuesProfiles.Add("MPEG_TS_SD_EU");
              valuesProfiles.Add("MPEG_TS_SD_NA");
              valuesProfiles.Add("MPEG_TS_SD_KO");
            }
          }
        }
        else if (videoCodec == VideoCodec.H264)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Ac3)
          {
            if (timestampType == Timestamp.None)
            {
              if(h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AC3_ISO");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AC3_ISO");
                }
              }
              else if (h264Profile == EncodingProfile.High || h264Profile == EncodingProfile.High10 || h264Profile == EncodingProfile.High422 || h264Profile == EncodingProfile.High444)
              {
                if (isHD)
                {
                  valuesProfiles.Add("AVC_TS_HP_HD_AC3_ISO");
                }
                else
                {
                  valuesProfiles.Add("AVC_TS_HP_SD_AC3_ISO");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AC3_ISO");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AC3_ISO");
              }
            }
            else if (timestampType == Timestamp.Valid)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AC3_T");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AC3_T");
                }
              }
              else if (h264Profile == EncodingProfile.High || h264Profile == EncodingProfile.High10 || h264Profile == EncodingProfile.High422 || h264Profile == EncodingProfile.High444)
              {
                if (isHD)
                {
                  valuesProfiles.Add("AVC_TS_HP_HD_AC3_T");
                }
                else
                {
                  valuesProfiles.Add("AVC_TS_HP_SD_AC3_T");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AC3_T");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AC3_T");
              }
            }
            else
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AC3");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AC3");
                }
              }
              else if (h264Profile == EncodingProfile.High || h264Profile == EncodingProfile.High10 || h264Profile == EncodingProfile.High422 || h264Profile == EncodingProfile.High444)
              {
                if (isHD)
                {
                  valuesProfiles.Add("AVC_TS_HP_HD_AC3");
                }
                else
                {
                  valuesProfiles.Add("AVC_TS_HP_SD_AC3");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AC3");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AC3");
              }
            }
          }
          else if (audioCodec == AudioCodec.Lpcm)
          {
            if (fps >= 59)
            {
              valuesProfiles.Add("AVC_TS_HD_60_LPCM_T");
            }

            //Fallback
            valuesProfiles.Add("AVC_TS_HD_50_LPCM_T");
          }
          else if (audioCodec == AudioCodec.Dts)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("AVC_TS_HD_DTS_ISO");
            }
            else
            {
              valuesProfiles.Add("AVC_TS_HD_DTS_T");
            }
          }
          else if (audioCodec == AudioCodec.DtsHd)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("AVC_TS_DTSHD_MA_ISO");
            }
            else
            {
              valuesProfiles.Add("AVC_TS_DTSHD_MA_T");
            }
          }
          else if (audioCodec == AudioCodec.Mp2)
          {
            if (timestampType == Timestamp.None)
            {
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_HP_HD_MPEG1_L2_ISO");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_HP_SD_MPEG1_L2_ISO");
              }
            }
            else
            {
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_HP_HD_MPEG1_L2_T");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_HP_SD_MPEG1_L2_T");
              }
            }
          }
          else if (audioCodec == AudioCodec.Aac)
          {
            if (timestampType == Timestamp.None)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AAC_MULT5_ISO");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AAC_MULT5_ISO");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AAC_MULT5_ISO");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AAC_MULT5_ISO");
              }
            }
            else if (timestampType == Timestamp.Valid)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AAC_MULT5_T");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AAC_MULT5_T");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AAC_MULT5_T");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AAC_MULT5_T");
              }
            }
            else
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 384)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF15_AAC_MULT5");
                }
                else if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_AAC_MULT5");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_AAC_MULT5");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_AAC_MULT5");
              }
            }
          }
          else if (audioCodec == AudioCodec.Mp3)
          {
            if (timestampType == Timestamp.None)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_MPEG1_L3_ISO");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_MPEG1_L3_ISO");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_MPEG1_L3_ISO");
              }
            }
            else if (timestampType == Timestamp.Valid)
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_MPEG1_L3_T");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_MPEG1_L3_T");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_MPEG1_L3_T");
              }
            }
            else
            {
              if (h264Profile == EncodingProfile.Baseline)
              {
                if (width <= 352 && height <= 288 && videoBitrate > 0 && videoBitrate <= 3000)
                {
                  valuesProfiles.Add("AVC_TS_BL_CIF30_MPEG1_L3");
                }
              }

              //Main profile and fallbacks
              if (isHD)
              {
                valuesProfiles.Add("AVC_TS_MP_HD_MPEG1_L3");
              }
              else
              {
                valuesProfiles.Add("AVC_TS_MP_SD_MPEG1_L3");
              }
            }
          }
        }
        else if (videoCodec == VideoCodec.Vc1)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Ac3)
          {
            if (isHD)
            {
              valuesProfiles.Add("VC1_TS_AP_L2_AC3_ISO");
            }
            else
            {
              valuesProfiles.Add("VC1_TS_AP_L1_AC3_ISO");
            }
          }
          else if (audioCodec == AudioCodec.Dts)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("VC1_TS_HD_DTS_ISO");
            }
            else
            {
              valuesProfiles.Add("VC1_TS_HD_DTS_T");
            }
          }
          else if (audioCodec == AudioCodec.DtsHd)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("VC1_TS_HD_DTSHD_MA_ISO");
            }
            else
            {
              valuesProfiles.Add("VC1_TS_HD_DTSHD_MA_T");
            }
          }
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Ac3)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AC3_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AC3_T");
            }
            else
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AC3");
            }
          }
          else if (audioCodec == AudioCodec.Aac)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AAC_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AAC_T");
            }
            else
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_AAC");
            }
          }
          else if (audioCodec == AudioCodec.Mp3)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG1_L3_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG1_L3_T");
            }
            else
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG1_L3");
            }
          }
          else if (audioCodec == AudioCodec.Mp2)
          {
            if (timestampType == Timestamp.None)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG2_L2_ISO");
            }
            else if (timestampType == Timestamp.Valid)
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG2_L2_T");
            }
            else
            {
              valuesProfiles.Add("MPEG4_P2_TS_ASP_MPEG2_L2");
            }
          }
        }
        else
        {
          throw new Exception("MPEG2TS video file does not match any supported DLNA profile");
        }
      }
      else if (container == VideoContainer.Flv)
      {
        valuesProfiles.Add("FLV");
      }
      else if (container == VideoContainer.Wtv)
      {
        valuesProfiles.Add("WTV");
      }
      else if (container == VideoContainer.Gp3)
      {
        if (videoCodec == VideoCodec.H264)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac)
          {
            valuesProfiles.Add("AVC_3GPP_BL_QCIF15_AAC");
          }
        }
        else if (videoCodec == VideoCodec.MsMpeg4 || videoCodec == VideoCodec.Mpeg4)
        {
          if (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac)
          {
            valuesProfiles.Add("MPEG4_P2_3GPP_SP_L0B_AAC");
          }
          else if (audioCodec == AudioCodec.Amr)
          {
            valuesProfiles.Add("MPEG4_P2_3GPP_SP_L0B_AMR");
          }
        }
        else if (videoCodec == VideoCodec.H263 && audioCodec == AudioCodec.Amr)
        {
          valuesProfiles.Add("MPEG4_H263_3GPP_P0_L10_AMR");
        }
        else
        {
          throw new Exception("3GP video file does not match any supported DLNA profile");
        }
      }
      else if (container == VideoContainer.RealMedia)
      {
        valuesProfiles.Add("REAL_VIDEO");
      }
      else if (container == VideoContainer.Ogg)
      {
        valuesProfiles.Add("OGV");
      }
      else if (container == VideoContainer.Hls)
      {
        if (videoCodec == VideoCodec.H264 && (audioCodec == AudioCodec.Unknown || audioCodec == AudioCodec.Aac))
        {
          valuesProfiles.Add("HLS");
        }
      }
      else if (container == VideoContainer.Rtsp)
      {
        valuesProfiles.Add("RTSP");
      }
      else if (container == VideoContainer.Rtp)
      {
        valuesProfiles.Add("RTP");
      }
      else
      {
        throw new Exception("Video does not match any supported DLNA profile");
      }
      return valuesProfiles;
    }

    public static bool FindCompatibleProfile(EndPointSettings client, List<string> resolvedList, ref string DlnaProfile, ref string Mime)
    {
      foreach (MediaMimeMapping map in client.Profile.MediaMimeMap.Values)
      {
        if (resolvedList.Contains(map.MappedMediaFormat) == true)
        {
          DlnaProfile = map.MappedMediaFormat;
          Mime = map.MIME;
          return true;
        }
      }
      foreach (MediaMimeMapping map in client.Profile.MediaMimeMap.Values)
      {
        if (string.IsNullOrEmpty(map.MIMEName) == false)
        {
          List<string> renamedMaps = new List<string>();
          renamedMaps.AddRange(map.MIMEName.Split(','));
          foreach (string submap in renamedMaps)
          {
            if (resolvedList.Contains(submap) == true)
            {
              DlnaProfile = map.MappedMediaFormat;
              Mime = map.MIME;
              return true;
            }
          }
        }
      }
      if (resolvedList.Count > 0)
      {
        DlnaProfile = resolvedList[0];
        return true;
      }
      return false;
    }
  }
}
