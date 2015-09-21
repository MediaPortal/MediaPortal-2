using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetAudioContainer
  {
    public static string GetAudioContainer(AudioContainer container)
    {
      switch (container)
      {
        case AudioContainer.Unknown:
          return null;
        case AudioContainer.Mp3:
          return "mp3";
        case AudioContainer.Mp2:
          return "mp2";
        case AudioContainer.Asf:
          return "asf";
        case AudioContainer.Lpcm:
          return "lpcm";
        case AudioContainer.Mp4:
          return "mp4";
        case AudioContainer.Flac:
          return "flac";
        case AudioContainer.Ogg:
          return "ogg";
        case AudioContainer.Flv:
          return "flv";
        case AudioContainer.Rtp:
          return "rtp";
        case AudioContainer.Rtsp:
          return "rtsp";
        case AudioContainer.Adts:
          return "adts";
        case AudioContainer.WavPack:
          return "wavpack";
        case AudioContainer.Ape:
          return "ape";
        case AudioContainer.MusePack:
          return "musepack";
      }
      return null;
    }
  }
}
