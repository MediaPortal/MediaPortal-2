using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetAudioCodec
  {
    public static string GetAudioCodec(AudioCodec codec)
    {
      switch (codec)
      {
        case AudioCodec.Mp3:
          return "mp3";
        case AudioCodec.Mp2:
          return "mp2";
        case AudioCodec.Mp1:
          return "mp1";
        case AudioCodec.Aac:
          return "libvo_aacenc";
        case AudioCodec.Ac3:
          return "ac3";
        case AudioCodec.Lpcm:
          return "pcm_s16le";
        case AudioCodec.Dts:
          return "dts";
        case AudioCodec.DtsHd:
          return "dts-hd";
        case AudioCodec.Wma:
          return "wmav1";
        case AudioCodec.WmaPro:
          return "wmapro";
        case AudioCodec.Flac:
          return "flac";
        case AudioCodec.Vorbis:
          return "vorbis";
        case AudioCodec.TrueHd:
          return "truehd";
        case AudioCodec.Amr:
          return "amrnb";
        case AudioCodec.Real:
          return "ralf";
      }
      return null;
    }
  }
}
