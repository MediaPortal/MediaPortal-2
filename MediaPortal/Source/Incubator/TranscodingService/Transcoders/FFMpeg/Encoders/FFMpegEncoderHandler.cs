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

using System.Collections.Generic;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders
{
  class FFMpegEncoderHandler
  {
    public enum EncoderHandler
    {
      Software,
      HardwareIntel,
      HardwareNvidia
    }

    private Dictionary<EncoderHandler, List<string>> _currentEncoderTranscodes = new Dictionary<EncoderHandler, List<string>>();
    private Dictionary<EncoderHandler, FFMpegEncoderConfig> _registeredEncoders = new Dictionary<EncoderHandler, FFMpegEncoderConfig>();
    private FFMpegEncoderConfig _softwareEncoder = null;
    private object _syncLock = new object();

    public void RegisterEncoder(EncoderHandler handler, int maximumStreams, List<VideoCodec> supportedCodecs)
    {
      lock (_syncLock)
      {
        if (_registeredEncoders.ContainsKey(handler) == true) return;
        if (handler == EncoderHandler.HardwareIntel)
        {
          if (_currentEncoderTranscodes.ContainsKey(handler) == false)
          {
            _currentEncoderTranscodes.Add(handler, new List<string>());
          }
          _registeredEncoders.Add(handler, new FFMpegEncoderConfig()
          {
            MaximumStreams = maximumStreams,
            SupportedCodecs = supportedCodecs,
            Presets = new Dictionary<VideoCodec, Dictionary<EncodingPreset, string>>()
            {
              {
                VideoCodec.Mpeg2,
                new Dictionary<EncodingPreset, string>()
                {
                  { EncodingPreset.Default, "-preset fast" },
                  { EncodingPreset.Ultrafast, "-preset fast" },
                  { EncodingPreset.Superfast, "-preset fast" },
                  { EncodingPreset.Veryfast, "-preset fast" },
                  { EncodingPreset.Faster, "-preset fast" },
                  { EncodingPreset.Fast, "-preset fast" },
                  { EncodingPreset.Medium, "-preset medium" },
                  { EncodingPreset.Slow, "-preset slow" },
                  { EncodingPreset.Slower, "-preset slow" },
                  { EncodingPreset.Veryslow, "-preset slow" }
                }
              },
              {
                VideoCodec.H264,
                new Dictionary<EncodingPreset, string>()
                {
                  { EncodingPreset.Default, "-preset veryfast" },
                  { EncodingPreset.Ultrafast, "-preset veryfast" },
                  { EncodingPreset.Superfast, "-preset veryfast" },
                  { EncodingPreset.Veryfast, "-preset veryfast" },
                  { EncodingPreset.Faster, "-preset faster" },
                  { EncodingPreset.Fast, "-preset fast" },
                  { EncodingPreset.Medium, "-preset medium" },
                  { EncodingPreset.Slow, "-preset slow" },
                  { EncodingPreset.Slower, "-preset slower" },
                  { EncodingPreset.Veryslow, "-preset veryslow" }
                }
              },
              {
                VideoCodec.H265,
                new Dictionary<EncodingPreset, string>()
                {
                  { EncodingPreset.Default, "-preset fast" },
                  { EncodingPreset.Ultrafast, "-preset fast" },
                  { EncodingPreset.Superfast, "-preset fast" },
                  { EncodingPreset.Veryfast, "-preset fast" },
                  { EncodingPreset.Faster, "-preset fast" },
                  { EncodingPreset.Fast, "-preset fast" },
                  { EncodingPreset.Medium, "-preset medium" },
                  { EncodingPreset.Slow, "-preset slow" },
                  { EncodingPreset.Slower, "-preset slow" },
                  { EncodingPreset.Veryslow, "-preset slow" }
                }
              }
            },
            Profiles = new Dictionary<VideoCodec, Dictionary<EncodingProfile, string>>()
            {
              {
                VideoCodec.Mpeg2,
                new Dictionary<EncodingProfile, string>() 
                {
                  { EncodingProfile.Unknown, "-profile:v main" },
                  { EncodingProfile.Simple, "-profile:v simple" },
                  { EncodingProfile.Main, "-profile:v main" },
                  { EncodingProfile.High, "-profile:v high" }
                }
              },
              {
                VideoCodec.H264,
                new Dictionary<EncodingProfile, string>() 
                {
                  { EncodingProfile.Unknown, "-profile:v baseline" },
                  { EncodingProfile.Baseline, "-profile:v baseline" },
                  { EncodingProfile.Main, "-profile:v main" },
                  { EncodingProfile.High, "-profile:v high" },
                  { EncodingProfile.High422, "-profile:v high" },
                  { EncodingProfile.High444, "-profile:v high" }
                }
              },
              {
                VideoCodec.H265,
                new Dictionary<EncodingProfile, string>() 
                {
                  { EncodingProfile.Unknown, "-profile:v main" },
                  { EncodingProfile.Main, "-profile:v main" },
                  { EncodingProfile.Main10, "-profile:v main10" }
                }
              }
            }
          });
        }
        else if (handler == EncoderHandler.HardwareNvidia)
        {
          if (_currentEncoderTranscodes.ContainsKey(handler) == false)
          {
            _currentEncoderTranscodes.Add(handler, new List<string>());
          }
          _registeredEncoders.Add(handler, new FFMpegEncoderConfig()
          {
            MaximumStreams = maximumStreams,
            SupportedCodecs = supportedCodecs,
            Presets = new Dictionary<VideoCodec, Dictionary<EncodingPreset, string>>()
            {
              {
                VideoCodec.H264,
                new Dictionary<EncodingPreset, string>()
                {
                  { EncodingPreset.Default, "-preset hp" },
                  { EncodingPreset.Ultrafast, "-preset hp" },
                  { EncodingPreset.Superfast, "-preset hp" },
                  { EncodingPreset.Veryfast, "-preset hp" },
                  { EncodingPreset.Faster, "-preset hp" },
                  { EncodingPreset.Fast, "-preset fast" },
                  { EncodingPreset.Medium, "-preset medium" },
                  { EncodingPreset.Slow, "-preset slow" },
                  { EncodingPreset.Slower, "-preset hq" },
                  { EncodingPreset.Veryslow, "-preset hq" }
                }
              },
              {
                VideoCodec.H265,
                new Dictionary<EncodingPreset, string>()
                {
                  { EncodingPreset.Default, "-preset hp" },
                  { EncodingPreset.Ultrafast, "-preset hp" },
                  { EncodingPreset.Superfast, "-preset hp" },
                  { EncodingPreset.Veryfast, "-preset hp" },
                  { EncodingPreset.Faster, "-preset hp" },
                  { EncodingPreset.Fast, "-preset fast" },
                  { EncodingPreset.Medium, "-preset medium" },
                  { EncodingPreset.Slow, "-preset slow" },
                  { EncodingPreset.Slower, "-preset hq" },
                  { EncodingPreset.Veryslow, "-preset hq" }
                }
              }
            },
            Profiles = new Dictionary<VideoCodec, Dictionary<EncodingProfile, string>>()
            {
              {
                VideoCodec.H264,
                new Dictionary<EncodingProfile, string>() 
                {
                  { EncodingProfile.Unknown, "-profile:v baseline" },
                  { EncodingProfile.Baseline, "-profile:v baseline" },
                  { EncodingProfile.Main, "-profile:v main" },
                  { EncodingProfile.High, "-profile:v high" },
                  { EncodingProfile.High422, "-profile:v high" },
                  { EncodingProfile.High444, "-profile:v high" }
                }
              }
            }
          });
        }
      }
    }

    public bool IsEncoderRegistered(EncoderHandler handler)
    {
      lock (_syncLock)
      {
        return _registeredEncoders.ContainsKey(handler);
      }
    }

    public void UnregisterEncoder(EncoderHandler handler)
    {
      lock (_syncLock)
      {
        if (_currentEncoderTranscodes.ContainsKey(handler))
        {
          _currentEncoderTranscodes.Remove(handler);
        }
        if (_registeredEncoders.ContainsKey(handler))
        {
          _registeredEncoders.Remove(handler);
        }
      }
    }

    public FFMpegEncoderHandler()
    {
      _currentEncoderTranscodes.Add(EncoderHandler.Software, new List<string>());
      _softwareEncoder = new FFMpegEncoderConfig()
      {
        MaximumStreams = 0,
        SupportedCodecs = new List<VideoCodec>()
        {
          VideoCodec.H264,
          VideoCodec.H265
        },
        Presets = new Dictionary<VideoCodec, Dictionary<EncodingPreset, string>>()
        {
          {
            VideoCodec.H264,
            new Dictionary<EncodingPreset, string>()
            {
              { EncodingPreset.Default, "-preset veryfast" },
              { EncodingPreset.Ultrafast, "-preset ultrafast" },
              { EncodingPreset.Superfast, "-preset superfast" },
              { EncodingPreset.Veryfast, "-preset veryfast" },
              { EncodingPreset.Faster, "-preset faster" },
              { EncodingPreset.Fast, "-preset fast" },
              { EncodingPreset.Medium, "-preset medium" },
              { EncodingPreset.Slow, "-preset slow" },
              { EncodingPreset.Slower, "-preset slower" },
              { EncodingPreset.Veryslow, "-preset veryslow" },
              { EncodingPreset.Placebo, "-preset placebo" }
            }
          },
          {
            VideoCodec.H265,
            new Dictionary<EncodingPreset, string>()
            {
              { EncodingPreset.Default, "-preset veryfast" },
              { EncodingPreset.Ultrafast, "-preset ultrafast" },
              { EncodingPreset.Superfast, "-preset superfast" },
              { EncodingPreset.Veryfast, "-preset veryfast" },
              { EncodingPreset.Faster, "-preset faster" },
              { EncodingPreset.Fast, "-preset fast" },
              { EncodingPreset.Medium, "-preset medium" },
              { EncodingPreset.Slow, "-preset slow" },
              { EncodingPreset.Slower, "-preset slower" },
              { EncodingPreset.Veryslow, "-preset veryslow" },
              { EncodingPreset.Placebo, "-preset placebo" }
            }
          }
        },
        Profiles = new Dictionary<VideoCodec, Dictionary<EncodingProfile, string>>()
        {
          {
            VideoCodec.H264,
            new Dictionary<EncodingProfile, string>() 
            {
              { EncodingProfile.Unknown, "-profile:v baseline" },
              { EncodingProfile.Baseline, "-profile:v baseline" },
              { EncodingProfile.Main, "-profile:v main" },
              { EncodingProfile.High, "-profile:v high" },
              { EncodingProfile.High422, "-profile:v high422" },
              { EncodingProfile.High444, "-profile:v high444" }
            }
          }
        }
      };
    }

    public EncoderHandler StartEncoding(string transcodeId, VideoCodec codec)
    {
      EncoderHandler handler = EncoderHandler.Software;
      lock (_syncLock)
      {
        foreach (KeyValuePair<EncoderHandler, FFMpegEncoderConfig> encoder in _registeredEncoders)
        {
          if (encoder.Value.SupportedCodecs.Contains(codec) && //Check codec support
            (encoder.Value.MaximumStreams <= 0 || encoder.Value.MaximumStreams <= _currentEncoderTranscodes[encoder.Key].Count)) //Check maximum stream support
          {
            handler = encoder.Key;
            break;
          }
        }
        if (_currentEncoderTranscodes[handler].Contains(transcodeId) == false)
        {
          _currentEncoderTranscodes[handler].Add(transcodeId);
        }
        return handler;
      }
    }

    public FFMpegEncoderConfig GetEncoderConfig(EncoderHandler handler)
    {
      lock (_syncLock)
      {
        FFMpegEncoderConfig config;
        if(_registeredEncoders.TryGetValue(handler, out config))
        {
          return config;
        }
        return _softwareEncoder;
      }
    }

    public void EndEncoding(EncoderHandler handler, string transcodeId)
    {
      lock (_syncLock)
      {
        if (_currentEncoderTranscodes[handler].Contains(transcodeId) == true)
        {
          _currentEncoderTranscodes[handler].Remove(transcodeId);
        }
      }
    }
  }
}
