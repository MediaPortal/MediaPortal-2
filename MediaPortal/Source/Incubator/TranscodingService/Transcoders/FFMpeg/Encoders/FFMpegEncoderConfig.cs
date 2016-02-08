using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Encoders
{
  class FFMpegEncoderConfig
  {
    public Dictionary<VideoCodec, Dictionary<EncodingProfile, string>> Profiles { get; set; }
    public Dictionary<VideoCodec, Dictionary<EncodingPreset, string>> Presets { get; set; }
    public List<VideoCodec> SupportedCodecs { get; set; }
    public int MaximumStreams { get; set; }

    public FFMpegEncoderConfig()
    {
      Profiles = new Dictionary<VideoCodec, Dictionary<EncodingProfile, string>>();
      Presets = new Dictionary<VideoCodec, Dictionary<EncodingPreset, string>>();
      SupportedCodecs = new List<VideoCodec>();
      MaximumStreams = 0;
    }

    public string GetEncoderPreset(VideoCodec codec, EncodingPreset preset)
    {
      if (Presets.ContainsKey(codec))
      {
        string presetArg;
        Presets[codec].TryGetValue(preset, out presetArg);
        return presetArg;
      }
      return null;
    }

    public string GetEncoderProfile(VideoCodec codec, EncodingProfile profile)
    {
      if (Profiles.ContainsKey(codec))
      {
        string profileArg;
        Profiles[codec].TryGetValue(profile, out profileArg);
        return profileArg;
      }
      return null;
    }
  }
}
