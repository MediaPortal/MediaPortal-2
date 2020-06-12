using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.Common.MobyGames
{
  class MobyGamesCoverArt : IHtmlDeserializable
  {
    protected static readonly Regex FRONT_REGEX = new Regex(@"background-image:url\(([^\)]*)[^>]*>(<[^>]*>\s*){4}Front Cover");
    protected static readonly Regex BACK_REGEX = new Regex(@"background-image:url\(([^\)]*)[^>]*>(<[^>]*>\s*){4}Back Cover");
    protected static readonly Regex MEDIA_REGEX = new Regex(@"background-image:url\(([^\)]*)[^>]*>(<[^>]*>\s*){4}Media");

    public string Front { get; set; }
    public string Back { get; set; }
    public string Media { get; set; }

    public bool Deserialize(string response)
    {
      Match m = FRONT_REGEX.Match(response);
      if (m.Success)
        Front = m.Groups[1].Value.Replace("/s/", "/l/");
      m = BACK_REGEX.Match(response);
      if (m.Success)
        Back = m.Groups[1].Value.Replace("/s/", "/l/");
      m = MEDIA_REGEX.Match(response);
      if (m.Success)
        Media = m.Groups[1].Value.Replace("/s/", "/l/");
      return true;
    }
  }
}
