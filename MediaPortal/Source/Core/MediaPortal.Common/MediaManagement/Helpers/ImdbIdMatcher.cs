#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Text.RegularExpressions;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="ImdbIdMatcher"/> tries to match IMDB ids from a given string using regular expressions.
  /// </summary>
  public class ImdbIdMatcher
  {
    public static string GROUP_IMDBID = "imdbid";
    public static Regex REGEXP_IMDBID = new Regex(@"(?<imdbid>tt\d{7})", RegexOptions.IgnoreCase);

    /// <summary>
    /// Tries to match IMDB ids from a given <param name="textOrfolderOrFileName">string</param> using regular expressions.
    /// </summary>
    /// <param name="textOrfolderOrFileName">This can be a text, a file or folder name.</param>
    /// <param name="imdbId">Returns the IMDB id.</param>
    /// <returns><c>true</c> if successful.</returns>
    public static bool TryMatchImdbId(string textOrfolderOrFileName, out string imdbId)
    {
      imdbId = null;
      if (string.IsNullOrEmpty(textOrfolderOrFileName))
        return false;

      Match match = REGEXP_IMDBID.Match(textOrfolderOrFileName);
      Group group = match.Groups[GROUP_IMDBID];
      if (group.Length == 0)
        return false;

      imdbId = group.Value;
      return true;
    }
  }
}
