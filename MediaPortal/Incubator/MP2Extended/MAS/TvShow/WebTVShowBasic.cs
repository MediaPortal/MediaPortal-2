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

using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.TvShow
{
  public class WebTVShowBasic : WebObject, ITitleSortable, IDateAddedSortable, IYearSortable, IGenreSortable, IRatingSortable, IArtwork, IActors
  {
    public WebTVShowBasic()
    {
      DateAdded = new DateTime(1970, 1, 1);
      Genres = new List<string>();
      Artwork = new List<WebArtwork>();
      ExternalId = new List<WebExternalId>();
      Actors = new List<WebActor>();
    }

    public string Id { get; set; }
    public bool IsProtected { get; set; }
    public DateTime DateAdded { get; set; }
    public IList<string> Genres { get; set; }
    public IList<WebArtwork> Artwork { get; set; }
    public IList<WebActor> Actors { get; set; }

    public string Title { get; set; }
    public int Year { get; set; }
    public int EpisodeCount { get; set; }
    public int UnwatchedEpisodeCount { get; set; }
    public int SeasonCount { get; set; }
    public float Rating { get; set; }
    public string ContentRating { get; set; }
    public IList<WebExternalId> ExternalId { get; set; }

    public override string ToString()
    {
      return Title;
    }
  }
}
