using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseH264Profile
  {
    internal static H264Profile ParseH264Profile(string token)
    {
      if (token != null)
      {
        if (token.Equals("constrained baseline", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Baseline;
        if (token.Equals("baseline", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Baseline;
        if (token.Equals("main", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.Main;
        if (token.Equals("high", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high10", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high422", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
        if (token.Equals("high444", StringComparison.InvariantCultureIgnoreCase))
          return H264Profile.High;
      }
      return H264Profile.Unknown;
    }
  }
}
