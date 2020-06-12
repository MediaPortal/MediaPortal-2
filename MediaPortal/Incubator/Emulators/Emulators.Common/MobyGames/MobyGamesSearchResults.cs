using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.Common.MobyGames
{
  class MobyGamesSearchResults : AbstractMobyGamesResult
  {
    protected static readonly Regex REGEX = new Regex(@"<a href=""/game/([^""]*)"">([^<]*)</a>(\s*\([^\)]*\))*(<[^>]*>){3}([^\(]*)\(<em>(\d+)");

    public List<SearchResult> Results { get; set; }

    public override bool Deserialize(string response)
    {
      if (string.IsNullOrEmpty(response))
        return false;
      MatchCollection matches = REGEX.Matches(response);
      if (matches.Count == 0)
        return false;

      List<SearchResult> results = new List<SearchResult>();
      foreach (Match m in REGEX.Matches(response))
      {
        results.Add(new SearchResult()
        {
          Id = m.Groups[1].Value,
          Title = Decode(m.Groups[2].Value),
          Platform = Decode(m.Groups[5].Value),
          Year = int.Parse(m.Groups[6].Value)
        });
      }
      Results = results;
      return true;
    }
  }

  class SearchResult
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Platform { get; set; }
    public int Year { get; set; }
  }
}
