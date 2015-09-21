using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Converters
{
  internal class FFMpegGetSubtitleContainer
  {
    public static string GetSubtitleContainer(SubtitleCodec codec)
    {
      switch (codec)
      {
        case SubtitleCodec.Srt:
          return "srt";
        case SubtitleCodec.MicroDvd:
          return "microdvd";
        case SubtitleCodec.SubView:
          return "subviewer";
        case SubtitleCodec.Ass:
          return "ass";
        case SubtitleCodec.Ssa:
          return "ssa";
        case SubtitleCodec.Smi:
          return "sami";
        case SubtitleCodec.MovTxt:
          return "mov_text";
        case SubtitleCodec.DvbSub:
          return "dvbsub";
      }
      return "copy";
    }
  }
}
