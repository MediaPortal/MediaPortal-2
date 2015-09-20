using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg.Parsers
{
  public class FFMpegParseProfile
  {
    internal static EncodingProfile ParseProfile(string token)
    {
      if (token != null)
      {
        if (token.Equals("constrained baseline", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.Baseline;
        if (token.Equals("baseline", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.Baseline;
        if (token.Equals("main", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.Main;
        if (token.Equals("high", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.High;
        if (token.Equals("high10", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.High;
        if (token.Equals("high422", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.High;
        if (token.Equals("high444", StringComparison.InvariantCultureIgnoreCase))
          return EncodingProfile.High;
      }
      return EncodingProfile.Unknown;
    }
  }
}
