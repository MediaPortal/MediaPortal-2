#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryVideoItem : MediaLibraryItem, IDirectoryVideoItem
  {
    public MediaLibraryVideoItem(string baseKey, MediaItem item) : base(baseKey, item)
    {
      Genre = new List<string>();
      Producer = new List<string>();
      Actor = new List<string>();
      Director = new List<string>();
      Publisher = new List<string>();

      var videoAspect = item.Aspects[VideoAspect.ASPECT_ID];
      var genreObj = videoAspect.GetAttributeValue(VideoAspect.ATTR_GENRES);
      if (genreObj != null) Genre.Add(genreObj.ToString());

      var resource = new MediaLibraryResource(item);
      resource.Initialise();
      Resources.Add(resource);
    }

    public override string Class
    {
      get { return "object.item.videoItem"; }
    }

    public IList<string> Genre { get; set; }

    public string LongDescription { get; set; }

    public IList<string> Producer { get; set; }

    public string Rating { get; set; }

    public IList<string> Actor { get; set; }

    public IList<string> Director { get; set; }

    public string Description { get; set; }

    public IList<string> Publisher { get; set; }

    public string Language { get; set; }

    public string Relation { get; set; }
  }
}