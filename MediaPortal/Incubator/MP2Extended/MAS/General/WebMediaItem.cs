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
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  public class WebMediaItem : WebObject, IDateAddedSortable, ITitleSortable, ITypeSortable, IArtwork
  {
    public WebMediaItem()
    {
      DateAdded = new DateTime(1970, 1, 1);
      Path = new List<string>();
      Artwork = new List<WebArtwork>();
    }

    public string Id { get; set; }
    public IList<string> Path { get; set; }
    public DateTime DateAdded { get; set; }
    public string Title { get; set; }
    public IList<WebArtwork> Artwork { get; set; }
    public virtual WebMediaType Type { get; set; }
  }
}
