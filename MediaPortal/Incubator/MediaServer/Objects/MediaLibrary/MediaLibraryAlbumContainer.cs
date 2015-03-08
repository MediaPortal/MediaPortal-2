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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Tree;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumContainer : BasicContainer
  {
    protected Guid ObjectId { get; set; }
    protected string BaseKey { get; set; }

    public MediaLibraryAlbumContainer(string id)
      : base(id)
    {
      BaseKey = MediaLibraryHelper.GetBaseKey(id);
      ObjectId = MediaLibraryHelper.GetObjectId(id);
    }

    public override int ChildCount
    {
      get
      {
        // This is some what inefficient
        return ChildCount = Albums().Count;
      }
      set { }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      ServiceRegistration.Get<ILogger>().Error("No idea how to find " + key);
      return null;
    }

    private HomogenousMap Albums()
    {
        var necessaryMiaTypeIDs = new Guid[]
                                  {
                                    MediaAspect.ASPECT_ID,
                                    AudioAspect.ASPECT_ID,
                                  };
        var library = ServiceRegistration.Get<IMediaLibrary>();

        return library.GetValueGroups(AudioAspect.ATTR_ALBUM, null, ProjectionFunction.None, necessaryMiaTypeIDs, null, true);
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      var parent = new BasicContainer(Id);
	  HomogenousMap items = Albums();
      var result = new List<IDirectoryObject>();
	  foreach(var item in items)
	  {
	      try
	      {
              //ServiceRegistration.Get<ILogger>().Info("Item {0}={1}", item.Key, item.Value);
              object title = item.Key;
              if (title == null)
                  title = "<no name>";
              MediaLibraryAlbumItem album = new MediaLibraryAlbumItem(item.Key as string, title as string);
              album.Initialise();
              result.Add(album);
	      }
	      catch (Exception e)
	      {
	        ServiceRegistration.Get<ILogger>().Error("Album search failed", e);
	      }
	  }
	  return result;
    }
  }
}