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
using MediaPortal.Plugins.MediaServer.Profiles;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.Basic
{
  public class BasicContainer : BasicObject, IDirectoryContainer
  {
    protected static readonly Guid[] NECSSARY_GENERIC_MIA_TYPE_IDS = {
      ProviderResourceAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
    };
    protected static readonly Guid[] OPTIONAL_GENERIC_MIA_TYPE_IDS = {
      DirectoryAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      AudioAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID,
      TranscodeItemAudioAspect.ASPECT_ID,
      TranscodeItemImageAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID,
      TranscodeItemVideoAudioAspect.ASPECT_ID,
      TranscodeItemVideoEmbeddedAspect.ASPECT_ID
    };

    protected static readonly Guid[] NECESSARY_SHARE_MIA_TYPE_IDS = {
      ProviderResourceAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
    };

    protected static readonly Guid[] OPTIONAL_SHARE_MIA_TYPE_IDS = {
       DirectoryAspect.ASPECT_ID
     };

    protected static readonly Guid[] NECESSARY_MUSIC_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      AudioAspect.ASPECT_ID,
      TranscodeItemAudioAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_MUSIC_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_ALBUM_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      AudioAlbumAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_ALBUM_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_EPISODE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID,
      TranscodeItemVideoAudioAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_EPISODE_MIA_TYPE_IDS = {
      TranscodeItemVideoEmbeddedAspect.ASPECT_ID
    };

    protected static readonly Guid[] NECESSARY_SEASON_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      SeasonAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_SEASON_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_SERIES_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      SeriesAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_SERIES_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_IMAGE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID,
      TranscodeItemImageAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_IMAGE_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_MOVIE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID,
      TranscodeItemVideoAudioAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_MOVIE_MIA_TYPE_IDS = {
      TranscodeItemVideoEmbeddedAspect.ASPECT_ID
    };

    protected readonly List<BasicObject> _children = new List<BasicObject>();

    private string _containerClass = "object.container";

    public BasicContainer(string id, EndPointSettings client) 
      : base(id, client)
    {
      Restricted = true;
      Searchable = false;
      SearchClass = new List<IDirectorySearchClass>();
      CreateClass = new List<IDirectoryCreateClass>();
      WriteStatus = "NOT_WRITABLE";
    }

    public void Add(BasicObject node)
    {
      if (node == null) return;
      Console.WriteLine("BasicContainer::Add entry, {0} to {1}", node.Key, Key);
      node.Parent = this;
      if (!_children.Contains(node))
      {
        _children.Add(node);
      }
      Console.WriteLine("BasicContainer::Add exit, {0} children", _children.Count);
    }

    public virtual BasicObject FindObject(string key)
    {
      return Key == key ? this : _children
        .Where(node => node is BasicContainer)
        .Select(node => ((BasicContainer)node).FindObject(key))
        .FirstOrDefault(n => n != null);
    }

    public void Sort()
    {
      _children.Sort();
      //TODO: Sort children of children?
      //foreach (BasicContainer container in _children)
      //{
      //  container.Sort();
      //}
    }

    public virtual List<IDirectoryObject> Browse(string sortCriteria)
    {
      // TODO: Need to sort based on sortCriteria.
      _children.Sort();
      return _children.Cast<IDirectoryObject>().ToList();
    }

    public override void Initialise()
    {
    }

    public void ContainerUpdated()
    {
      UpdateId++;
      LastUpdate = DateTime.Now;
    }

    public override string Class
    {
      get { return _containerClass; }
      set { _containerClass = value; }
    }

    public virtual IList<IDirectoryCreateClass> CreateClass { get; set; }

    public virtual IList<IDirectorySearchClass> SearchClass { get; set; }

    public virtual bool Searchable { get; set; }

    public virtual int ChildCount
    {
      get { return _children.Count; }
      set { } //Meaningless in this implementation
    }

    public int UpdateId { get; set; }

    public DateTime LastUpdate { get; set; }
  }
}
