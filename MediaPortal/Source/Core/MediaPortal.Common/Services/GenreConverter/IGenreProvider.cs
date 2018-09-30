#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.Services.GenreConverter
{
  /// <summary>
  /// Interface to implement different genre converter providers, which will be used in the <see cref="IGenreConverter"/> service.
  /// </summary>
  public interface IGenreProvider
  {
    /// <summary>
    /// Determines a video genre for the specified <paramref name="genreName"/>.
    /// </summary>
    /// <param name="genreName">Genre name to detect a genre for.</param>
    /// <param name="genreCategory">The type of category to use for detecting the genre like e.g. Movie, Series or Music.</param>
    /// <param name="genreCulture">The culture to use for detecting the genre. A value of null, checks for first matching language.</param>
    /// <param name="genre">The matching genre for the genre name.</param>
    /// <returns><c>true</c>, if the genre could successfully be found, else <c>false</c>.</returns>
    bool GetGenreId(string genreName, string genreCategory, string genreCulture, out int genreId);

    /// <summary>
    /// Determines a genre name for the specified <paramref name="genreId"/>.
    /// </summary>
    /// <param name="genreId">The genre to fine a name for.</param>
    /// <param name="genreCategory">The type of category to use for detecting the genre like e.g. Movie, Series or Music.</param>
    /// <param name="genreCulture">The culture to use for the genre name. A value of null uses default culture.</param>
    /// <param name="genreName">The genre name determined from the specified parameters.</param>
    /// <returns><c>true</c>, if the genre name was successfully found, else <c>false</c>.</returns>
    bool GetGenreName(int genreId, string genreCategory, string genreCulture, out string genreName);

    /// <summary>
    /// Determines a genre type for the specified <paramref name="genreId"/>.
    /// </summary>
    /// <param name="genreId">The genre to fine a name for.</param>
    /// <param name="genreCategory">The type of category to use for detecting the genre like e.g. Movie, Series or Music.</param>
    /// <param name="genreType">The genre type determined from the specified parameters formatted so it identifies a unique genre type.</param>
    /// <returns><c>true</c>, if the genre type was successfully found, else <c>false</c>.</returns>
    bool GetGenreType(int genreId, string genreCategory, out string genreType);
  }
}
