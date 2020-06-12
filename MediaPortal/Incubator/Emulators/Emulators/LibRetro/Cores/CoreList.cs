using Emulators.Common.WebRequests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Cores
{
  class CoreList : IHtmlDeserializable
  {
    protected const string URLS_REGEX_PATTERN = @"<td[^>]*><a href='([^']*?\.zip)'[^>]*>([^<]*)";
    protected static readonly Regex URLS_REGEX = new Regex(URLS_REGEX_PATTERN);

    protected List<OnlineCore> _coreUrls = new List<OnlineCore>();

    public List<OnlineCore> CoreUrls
    {
      get { return _coreUrls; }
    }

    public bool Deserialize(string html)
    {
      string line = null;
      using (StringReader sr = new StringReader(html))
        while ((line = sr.ReadLine()) != null)
        {
          string[] coreInfo = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          if (coreInfo.Length > 2)
          _coreUrls.Add(new OnlineCore { Date = coreInfo[0], Name = coreInfo[2] });
        }
      return _coreUrls.Count > 0;
    }
  }
}
