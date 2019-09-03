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

using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Movie
{
  public class WebMovieBasic : WebMediaItem, IYearSortable, IGenreSortable, IRatingSortable, IActors
  {
    public WebMovieBasic()
    {
      Genres = new List<string>();
      ExternalId = new List<WebExternalId>();
      Actors = new List<WebActor>();
    }

    public bool IsProtected { get; set; }
    public IList<string> Genres { get; set; }
    public IList<WebExternalId> ExternalId { get; set; }
    public IList<WebActor> Actors { get; set; }

    public int Year { get; set; }
    public float Rating { get; set; }
    public int Runtime { get; set; }

    public bool Watched { get; set; }

    public override WebMediaType Type
    {
      get { return WebMediaType.Movie; }
    }

    public override string ToString()
    {
      return Title;
    }
  }
}
