using Emulators.Common.Games;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emulators.Common.NameProcessing
{
  public class NameProcessor
  {
    public static readonly IList<ITitleConverter> TITLE_CONVERTERS = new List<ITitleConverter>
    {
      new MameTitleConverter()
    };

    public static readonly IList<Regex> REGEXP_CLEANUPS = new List<Regex>
      {
        new Regex(@"[(\[].*?[)\]]", RegexOptions.IgnoreCase),
        new Regex(@"[/,'°]", RegexOptions.IgnoreCase),
        new Regex(@"(\s|-)*$", RegexOptions.IgnoreCase), 
        // Can be extended
      };

    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_-](\S|$)");
    protected static Regex _trimWhiteSpaces = new Regex(@"\s{2,}");

    public static bool CleanupTitle(GameInfo gameInfo)
    {
      string originalTitle = gameInfo.GameName;
      foreach (ITitleConverter converter in TITLE_CONVERTERS)
        converter.ConvertTitle(gameInfo);
      foreach (Regex regex in REGEXP_CLEANUPS)
        gameInfo.GameName = regex.Replace(gameInfo.GameName, "");
      gameInfo.GameName = CleanupWhiteSpaces(gameInfo.GameName);
      return originalTitle != gameInfo.GameName;
    }

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      if (string.IsNullOrEmpty(str))
        return str;
      str = _cleanUpWhiteSpaces.Replace(str, " $1");
      //replace multiple spaces with single space
      return _trimWhiteSpaces.Replace(str, " ").Trim(' ', '-');
    }

    public static bool AreStringsEqual(string name, string other)
    {
      return AreStringsEqual(name, other, 0);
    }

    public static bool AreStringsEqual(string name, string other, int distance)
    {
      if (name == other)
        return true;
      if (name == null || other == null)
        return false;
      if (name.ToLowerInvariant() == other.ToLowerInvariant())
        return true;
      return distance > 0 ? GetLevenshteinDistance(name, other) <= distance : false;
    }

    public static int GetLevenshteinDistance(string resultName, string searchName)
    {
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(resultName), RemoveCharacters(searchName));
    }

    /// <summary>
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    protected static string RemoveCharacters(string name)
    {
      name = name.ToLowerInvariant();
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'", "(", ")", "[", "]", "teil", "part" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result);
    }
  }
}
