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
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Common.General;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using System.Collections.Generic;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MediaServer.Tree;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  internal class MediaLibraryMusicGenreContainer : BasicContainer
  {
    protected Guid ObjectId { get; set; }
    protected string BaseKey { get; set; }

    private bool _initialised = false;
    private Dictionary<string, MediaLibraryMusicGenreItem> _genreDictionary = new Dictionary<string, MediaLibraryMusicGenreItem>();

    public MediaLibraryMusicGenreContainer(string id, EndPointSettings client)
      : base(id, client)
    {
      BaseKey = MediaLibraryHelper.GetBaseKey(id);
      ObjectId = MediaLibraryHelper.GetObjectId(id);
    }

    public override void Initialise()
    {
      _genreDictionary.Clear();
      HomogenousMap items = MusicGenres();
      foreach (var item in items)
      {
        try
        {
          string title = (string)item.Key;
          if (title == null)
            title = "<Unknown>";
          string key = Id + ":" + title;

          _genreDictionary.Add(key, new MediaLibraryMusicGenreItem(key, title, Client));
          _initialised = true;
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Music genre initialise failed", e);
        }
      }
    }

    public override int ChildCount
    {
      get
      {
        if (!_initialised) Initialise();
        return ChildCount = _genreDictionary.Count;
      }
      set { }
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      if (!_initialised) Initialise();
      MediaLibraryMusicGenreItem container;
      _genreDictionary.TryGetValue(key, out container);
      return container;
    }

    private HomogenousMap MusicGenres()
    {
      var necessaryMiaTypeIDs = new Guid[]
                                  {
                                    MediaAspect.ASPECT_ID,
                                    TranscodeItemAudioAspect.ASPECT_ID
                                  };
      var library = ServiceRegistration.Get<IMediaLibrary>();

      return library.GetValueGroups(AudioAspect.ATTR_GENRES, null, ProjectionFunction.None, necessaryMiaTypeIDs, null, true);
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      if (!_initialised) Initialise();
      var result = new List<IDirectoryObject>();
      foreach (var item in _genreDictionary.Values)
      {
        try
        {
          string key = (string)item.Key;
          if (key == null)
            key = "<Unknown>";

          item.Initialise();
          result.Add(item);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("Music genre search failed", e);
        }
      }
      return result;
    }
  }
}
