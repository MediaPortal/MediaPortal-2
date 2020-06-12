using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Emulators.Common.MobyGames
{
  class MobyGamesResult : AbstractMobyGamesResult
  {
    protected static readonly Regex TITLE_REGEX = new Regex(@"<a href=""/game/([^""]*)"">([^<]*)</a>[^\(]*\(<a[^>]*>([^<]*)");
    protected static readonly Regex RELEASE_REGEX = new Regex(@"Published by(<[^>]*>){3}([^<]*)(<[^>]*>){3}Developed by(<[^>]*>){3}([^<]*)(<[^>]*>){3}Released(<[^>]*>){3}([^<]*)");
    protected static readonly Regex GENRE_REGEX = new Regex(@"ESRB Rating(<[^>]*>){3}([^<]*)(<[^>]*>){3}Genre(<[^>]*>){3}([^<]*)");
    protected static readonly Regex RATING_REGEX = new Regex(@"<div[^>]*>(\d+)</div>[\r\n\s]*<div><b>Critic Score");
    protected static readonly Regex OVERVIEW_REGEX = new Regex(@"Description</h2>(.*?)<div");
    protected static readonly Regex BREAK_REGEX = new Regex(@"<br[^>]*>");
    protected static readonly Regex TAGS_REGEX = new Regex(@"<[^>]*>");

    public string Id { get; set; }
    public string Title { get; set; }
    public string Platform { get; set; }
    public string ReleaseDate { get; set; }
    public string Overview { get; set; }
    public string ESRB { get; set; }
    public HashSet<string> Genres { get; set; }
    public string Players { get; set; }
    public string Coop { get; set; }
    public string Publisher { get; set; }
    public string Developer { get; set; }
    public double Rating { get; set; }

    public override bool Deserialize(string response)
    {
      Match m = TITLE_REGEX.Match(response);
      if (!m.Success)
        return false;
      Id = m.Groups[1].Value;
      Title = Decode(m.Groups[2].Value);
      Platform = Decode(m.Groups[3].Value);

      m = RELEASE_REGEX.Match(response);
      if (m.Success)
      {
        Publisher = Decode(m.Groups[2].Value);
        Developer = Decode(m.Groups[5].Value);
        ReleaseDate = Decode(m.Groups[8].Value);
      }

      m = GENRE_REGEX.Match(response);
      if (m.Success)
      {
        ESRB = Decode(m.Groups[2].Value);
        Genres = new HashSet<string> { m.Groups[5].Value };
      }

      m = RATING_REGEX.Match(response);
      if (m.Success)
        Rating = int.Parse(m.Groups[1].Value) / 10d;

      m = OVERVIEW_REGEX.Match(response);
      if (m.Success)
      {
        string overview = BREAK_REGEX.Replace(m.Groups[1].Value, Environment.NewLine);
        Overview = Decode(TAGS_REGEX.Replace(overview, string.Empty));
      }
      return true;
    }
  }
}