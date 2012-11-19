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

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryImageItem : MediaLibraryItem, IDirectoryImageItem
  {
    public MediaLibraryImageItem(string baseKey, MediaItem item) : base(baseKey, item)
    {
      Publisher = new List<string>();
      Rights = new List<string>();
    }

    public override string Class
    {
      get { return "object.item.imageItem"; }
    }

    public string LongDescription { get; set; }

    public string StorageMedium { get; set; }

    public string Rating { get; set; }

    public string Description { get; set; }

    public IList<string> Publisher { get; set; }

    public string Date { get; set; }

    public IList<string> Rights { get; set; }
  }
}