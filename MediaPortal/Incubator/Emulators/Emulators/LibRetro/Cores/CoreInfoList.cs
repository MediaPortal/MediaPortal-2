using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  class CoreInfoList : IHtmlDeserializable
  {
    protected const string URLS_REGEX_PATTERN = @"<td[^>]*><a href='([^']*?\.info)'";
    protected static readonly Regex URLS_REGEX = new Regex(URLS_REGEX_PATTERN);
    protected List<string> _coreInfoUrls = new List<string>();

    public List<string> CoreInfoUrls
    {
      get { return _coreInfoUrls; }
    }

    public bool Deserialize(string html)
    {
      MatchCollection matches = URLS_REGEX.Matches(html);
      if (matches.Count == 0)
        return false;

      foreach (Match match in matches)
        _coreInfoUrls.Add(match.Groups[1].Value);
      return true;
    }
  }
}
