using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseSubtitleCodec
  {
    internal static SubtitleCodec ParseSubtitleCodec(string token)
    {
      if (token != null)
      {
        if (token.Equals("ass", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ass;
        if (token.Equals("ssa", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Ssa;
        if (token.Equals("mov_text", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MovTxt;
        if (token.Equals("sami", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Smi;
        if (token.Equals("srt", StringComparison.InvariantCultureIgnoreCase) || token.Equals("subrip", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Srt;
        if (token.Equals("microdvd", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.MicroDvd;
        if (token.Equals("subviewer", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.SubView;
        if (token.Equals("webvtt", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.Vtt;
        if (token.Equals("dvb_subtitle", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbSub;
        if (token.Equals("dvb_teletext", StringComparison.InvariantCultureIgnoreCase))
          return SubtitleCodec.DvbTxt;
      }
      return SubtitleCodec.Unknown;
    }
  }
}
