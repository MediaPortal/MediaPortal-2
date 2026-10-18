#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Asks an online source whether a title the importer only knows a fragment of is really a movie,
  /// and fills in what the source knows about it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the piece of the online libraries that a local metadata extractor needs, kept as one
  /// method so that the extractor does not have to reference them. <c>OnlineLibraries</c> implements
  /// it - TMDB, SimApi, FanArtTV - and registers it as a service; an extractor that is handed no
  /// implementation is on a server that has no online source installed, which is a normal state
  /// rather than an error.
  /// </para>
  /// <para>
  /// <c>MediaPortal.Extensions.OnlineLibraries.Libraries.IOnlineMatcherService</c> is the full face of
  /// the same object: series, episodes, albums, tracks, subtitles, and the matcher registry. This
  /// interface exists because that one lives in an assembly built for Windows - it is where the
  /// tree's <c>System.Drawing</c> use is, resizing TVDb banners - while a plugin that reads a
  /// <c>.nfo</c> file from disk has no business being Windows-only.
  /// </para>
  /// </remarks>
  public interface IOnlineMovieMatcher
  {
    /// <summary>
    /// Looks <paramref name="movieInfo"/> up online and completes it in place.
    /// </summary>
    /// <param name="movieInfo">What the importer knows so far; filled in when the answer is
    /// <c>true</c>.</param>
    /// <param name="category">Media category the title belongs to, e.g. <c>movies</c>.</param>
    /// <returns><c>true</c> when an online source says this is a movie - as opposed to knowing the
    /// same title as a series, or not knowing it at all. <c>false</c> says nothing was filled
    /// in.</returns>
    Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo, string category);
  }
}
