using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseAudioContainer
  {
    internal static AudioContainer ParseAudioContainer(string token)
    {
      if (token != null)
      {
        if (token.Equals("ac3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ac3;
        if (token.Equals("adts", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Adts;
        if (token.Equals("ape", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ape;
        if (token.Equals("asf", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Asf;
        if (token.Equals("flac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flac;
        if (token.Equals("flv", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Flv;
        if (token.Equals("lpcm", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Lpcm;
        if (token.Equals("mov", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mp4", StringComparison.InvariantCultureIgnoreCase) ||
          token.Equals("aac", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp4;
        if (token.Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp3;
        if (token.Equals("mp2", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Mp2;
        if (token.Equals("musepack", StringComparison.InvariantCultureIgnoreCase) || token.Equals("mpc", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.MusePack;
        if (token.Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Ogg;
        if (token.Equals("rtp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtp;
        if (token.Equals("rtsp", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.Rtsp;
        if (token.Equals("wavpack", StringComparison.InvariantCultureIgnoreCase))
          return AudioContainer.WavPack;
      }
      return AudioContainer.Unknown;
    }
  }
}
