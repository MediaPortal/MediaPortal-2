using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Emulators.Common.MobyGames
{
  class MobyGamesSearchResults : AbstractMobyGamesResult
  {
    protected static readonly Regex REGEX = new Regex(@"<a href=""[^>]*/game/([^""]*)"">([^<]*)</a>(\s*\([^\)]*\))*(<[^>]*>){3}([^\(]*)\(<em>(\d+)<\/em>\)");

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
          Platform = m.Groups[1].Value.Substring(0, m.Groups[1].Value.IndexOf("/")),
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
