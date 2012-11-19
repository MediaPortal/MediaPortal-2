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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MediaServer.Objects.Basic;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryContainer : BasicContainer
  {
    public MediaItem Item { get; protected set; }

    public MediaLibraryContainer(string baseKey, MediaItem item)
      : base(baseKey + ":" + item.MediaItemId)
    {
      Item = item;
    }

    public override int ChildCount { get; set; }

    public override void Initialise()
    {
      Title = Item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_TITLE).ToString();
      ChildCount = MediaLibraryBrowse().Count;
    }

    private ICollection<MediaItem> MediaLibraryBrowse()
    {
      var necessaryMIATypeIDs = new Guid[]
                                  {
                                    ProviderResourceAspect.ASPECT_ID,
                                    MediaAspect.ASPECT_ID,
                                  };
      var optionalMIATypeIDs = new Guid[]
                                 {
                                   DirectoryAspect.ASPECT_ID,
                                   VideoAspect.ASPECT_ID,
                                   AudioAspect.ASPECT_ID,
                                   ImageAspect.ASPECT_ID
                                 };

      var library = ServiceRegistration.Get<IMediaLibrary>();
      return library.Browse(Item.MediaItemId, necessaryMIATypeIDs, optionalMIATypeIDs);
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      var result = (from item in MediaLibraryBrowse()
                    select
                      MediaLibraryHelper.InstansiateMediaLibraryObject(item, MediaLibraryHelper.GetBaseKey(Key), this)).
        ToList();

      return result;
    }
  }
}