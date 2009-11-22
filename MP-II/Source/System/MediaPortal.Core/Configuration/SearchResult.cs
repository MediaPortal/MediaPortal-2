#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Core.Configuration
{
  /// <summary>
  /// SearchResult holds an enumerable of matches for a search,
  /// and the best matching node for same that search.
  /// </summary>
  public class SearchResult
  {

    #region Variables

    /// <summary>
    /// All matched nodes.
    /// </summary>
    private IEnumerable<IConfigurationNode> _matches;
    /// <summary>
    /// The best matching node.
    /// </summary>
    private IConfigurationNode _bestMatch;

    #endregion

    #region Properties

    /// <summary>
    /// Gets all configuration nodes matching the searchterm.
    /// </summary>
    public IEnumerable<IConfigurationNode> Matches
    {
      get { return _matches; }
    }

    /// <summary>
    /// Gets the best matching instances of <seealso cref="IConfigurationNode"/>.
    /// </summary>
    public IConfigurationNode BestMatch
    {
      get { return _bestMatch; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of SearchResult
    /// based on the given <paramref name="matches"/> and the specified <paramref name="bestMatch"/>.
    /// </summary>
    /// <param name="matches">All matching instances of <see cref="IConfigurationNode"/>.</param>
    /// <param name="bestMatch">The best matching instance of <see cref="IConfigurationNode"/>.</param>
    public SearchResult(IEnumerable<IConfigurationNode> matches, IConfigurationNode bestMatch)
    {
      _matches = matches;
      _bestMatch = bestMatch;
    }

    #endregion

  }

}
