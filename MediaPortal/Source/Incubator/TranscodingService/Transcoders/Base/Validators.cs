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

using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.Base
{
  internal class Validators
  {
    private readonly static List<long> VALID_AUDIO_BITRATES = new List<long>(new long[]
    {
      32,
      48,
      56,
      64,
      80,
      96,
      112,
      128,
      160,
      192,
      224,
      256,
      320,
      384,
      448,
      512,
      576,
      640
    });

    private static readonly Dictionary<AudioCodec, int> MAX_CHANNEL_NUMBER = new Dictionary<AudioCodec, int>()
    {
      { AudioCodec.Ac3, 6 },
      { AudioCodec.Dts, 6 },
      { AudioCodec.DtsHd, 6 },
      { AudioCodec.Mp1, 2 },
      { AudioCodec.Mp2, 2 },
      { AudioCodec.Mp3, 2 },
      { AudioCodec.Wma, 2 },
      { AudioCodec.WmaPro, 6 },
      { AudioCodec.Lpcm, 2 }
    };
    
    
    internal static string GetValidFramerate(double validFramerate)
    {
      string normalizedFps = validFramerate.ToString();
      if (validFramerate < 23.99)
        normalizedFps = "23.976";
      else if (validFramerate >= 23.99 && validFramerate < 24.1)
        normalizedFps = "24";
      else if (validFramerate >= 24.99 && validFramerate < 25.1)
        normalizedFps = "25";
      else if (validFramerate >= 29.9 && validFramerate < 29.99)
        normalizedFps = "29.97";
      else if (validFramerate >= 29.99 && validFramerate < 30.1)
        normalizedFps = "30";
      else if (validFramerate >= 49.9 && validFramerate < 50.1)
        normalizedFps = "50";
      else if (validFramerate >= 59.9 && validFramerate < 59.99)
        normalizedFps = "59.94";
      else if (validFramerate >= 59.99 && validFramerate < 60.1)
        normalizedFps = "60";

      if (normalizedFps == "23.976")
        return "24000/1001";
      if (normalizedFps == "29.97")
        return "30000/1001";
      if (normalizedFps == "59.94")
        return "60000/1001";

      return normalizedFps;
    }

    internal static int GetMaxNumberOfChannels(AudioCodec codec)
    {
      if (codec != AudioCodec.Unknown && MAX_CHANNEL_NUMBER.ContainsKey(codec))
      {
        return MAX_CHANNEL_NUMBER[codec];
      }
      return 2;
    }

    internal static int GetAudioNumberOfChannels(AudioCodec sourceCodec, AudioCodec targetCodec, int sourceChannels, bool forceStereo)
    {
      bool downmixingSupported = sourceCodec != AudioCodec.Flac;
      if (sourceChannels <= 0)
      {
        if (forceStereo)
          return 2;
      }
      else
      {
        int maxChannels = GetMaxNumberOfChannels(targetCodec);
        if (sourceChannels > 2 && forceStereo && downmixingSupported)
        {
          return 2;
        }
        if (maxChannels > 0 && maxChannels < sourceChannels)
        {
          return maxChannels;
        }
        return sourceChannels;
      }
      return -1;
    }

    internal static long GetAudioBitrate(long sourceBitrate, long targetBitrate)
    {
      if (targetBitrate > 0)
      {
        return targetBitrate;
      }
      long bitrate = sourceBitrate;
      if (bitrate > 0 && VALID_AUDIO_BITRATES.Contains(bitrate) == false)
      {
        bitrate = FindNearestValidBitrate(bitrate);
      }
      long maxBitrate = 192;
      if (bitrate > 0 && bitrate < maxBitrate)
      {
        return bitrate;
      }
      return maxBitrate;
    }

    internal static int FindNearestValidBitrate(double itemBitrate)
    {
      if (itemBitrate < 0)
      {
        itemBitrate = 0;
      }
      int nearest = -1;
      double smallestDiff = double.MaxValue;
      foreach (int validRate in VALID_AUDIO_BITRATES)
      {
        double d = Math.Abs(itemBitrate - validRate);
        if (d < smallestDiff)
        {
          nearest = validRate;
          smallestDiff = d;
        }
      }
      return nearest;
    }

    internal static long GetAudioFrequency(AudioCodec sourceCodec, AudioCodec targetCodec, long sourceFrequency, long targetSampleRate)
    {
      if (targetSampleRate > 0)
      {
        return targetSampleRate;
      }
      bool isLPCM = sourceCodec == AudioCodec.Lpcm || targetCodec == AudioCodec.Lpcm;
      long minfrequency = 48000;
      bool frequencyRequired = true;
      if (sourceFrequency >= 44100)
      {
        minfrequency = sourceFrequency;
        frequencyRequired = false;
      }
      if (isLPCM || frequencyRequired)
      {
        return minfrequency;
      }
      return -1;
    }
  }
}
