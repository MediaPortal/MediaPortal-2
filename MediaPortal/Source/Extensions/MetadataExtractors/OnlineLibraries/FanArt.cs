#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="FanArtType"/> defines FanArt types for metadata matchers.
  /// </summary>
  public static class FanArtType
  {
    public static readonly string Backdrops = "Backdrops";
    public static readonly string Banners = "Banners";
    public static readonly string Posters = "Posters";
    public static readonly string DiscArt = "DiscArt";
    public static readonly string ClearArt = "ClearArt";
    public static readonly string Logos = "Logos";
    public static readonly string Thumbnails = "Thumbnails";
    public static readonly string Covers = "Covers";
  }

  /// <summary>
  /// <see cref="FanArtScope"/> defines FanArt scopes for metadata matchers.
  /// </summary>
  public static class FanArtScope
  {
    public static readonly string Series = "Series";
    public static readonly string Season = "Season";
    public static readonly string Episode = "Episode";
    public static readonly string Actor = "Actor";
    public static readonly string Director = "Director";
    public static readonly string Character = "Character";
    public static readonly string Writer = "Writer";
    public static readonly string Company = "Company";
    public static readonly string Label = "Label";
    public static readonly string Network = "Network";
    public static readonly string Album = "Album";
    public static readonly string Artist = "Artist";
    public static readonly string Movie = "Movie";
    public static readonly string Collection = "Collection";
  }

  public class FanArtImageCollection<T>
  {
    public string Id = null;
    public List<T> Backdrops = new List<T>();
    public List<T> Banners = new List<T>();
    public List<T> Posters = new List<T>();
    public List<T> DiscArt = new List<T>();
    public List<T> ClearArt = new List<T>();
    public List<T> Logos = new List<T>();
    public List<T> Thumbnails = new List<T>();
    public List<T> Covers = new List<T>();
  }
}
