#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.MediaServer.Profiles;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Extensions.MediaServer.Objects.Basic
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
      VideoStreamAspect.ASPECT_ID, //For detecting editions
      AudioAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID,
      MovieAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID,
      SubtitleAspect.ASPECT_ID
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
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_MUSIC_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
    };

    protected static readonly Guid[] NECESSARY_ALBUM_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      AudioAlbumAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_ALBUM_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
    };

    protected static readonly Guid[] NECESSARY_EPISODE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      VideoStreamAspect.ASPECT_ID, //For detecting editions
      EpisodeAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_EPISODE_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
      SubtitleAspect.ASPECT_ID
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
    protected static readonly Guid[] OPTIONAL_SERIES_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
    };

    protected static readonly Guid[] NECESSARY_IMAGE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_IMAGE_MIA_TYPE_IDS = null;

    protected static readonly Guid[] NECESSARY_MOVIE_MIA_TYPE_IDS = {
      ImporterAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      VideoStreamAspect.ASPECT_ID, //For detecting editions
      MovieAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_MOVIE_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
      SubtitleAspect.ASPECT_ID
    };

    protected static readonly Guid[] NECESSARY_PERSON_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      PersonAspect.ASPECT_ID,
    };
    protected static readonly Guid[] OPTIONAL_PERSON_MIA_TYPE_IDS = null;

    protected readonly List<BasicObject> _children = new List<BasicObject>();

    private string _containerClass = "object.container";
    private bool _containersInitialized = false;

    public ICollection<BasicObject> Children => _children;

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
      node.Parent = this;
      if (!_children.Contains(node))
      {
        _children.Add(node);
      }
      Logger.Debug("MediaServer added {0} ({3}) to {1}, now {2} children", node.Key, Key, _children.Count, node.Title);
    }

    public virtual BasicObject FindObject(string key)
    {
      //Check if it is this item
      if (Key == key)
        return this;

      //Needs to initialize children
      if (!_containersInitialized)
        InitialiseContainers();

      //Check child items
      var obj = _children.FirstOrDefault(c => c.Key == key && !c.Placeholder);
      if (obj != null)
        return obj;

      //Check child dirs
      foreach (BasicContainer dir in _children.Where(c => c is BasicContainer))
      {
        obj = dir.FindObject(key);
        if (obj != null)
          return obj;
      }
      return null;
    }

    public virtual List<IDirectoryObject> Browse()
    {
      return _children.OfType<IDirectoryObject>().ToList();
    }

    public override void Initialise(string sortCriteria, uint? offset = null, uint? count = null)
    {
      _containersInitialized = true;
    }

    public virtual void InitialiseContainers()
    {
      _containersInitialized = true;
    }

    public void ContainerUpdated()
    {
      UpdateId++;
      LastUpdate = DateTime.Now;
    }

    public IFilter AppendUserFilter(IFilter filter, ICollection<Guid> necessaryMias)
    {
      IFilter userFilter = null;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      var res = userProfileDataManagement.GetProfileAsync(UserId).Result;
      if (res.Success)
        userFilter = res.Result.GetUserFilter(necessaryMias);

      return filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : filter;
    }

    protected ICollection<Share> GetAllowedShares()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      ICollection<Share> shares = library.GetShares(null)?.Values;

      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      var res = userProfileDataManagement.GetProfileAsync(UserId).Result;
      if (!res.Success || !res.Result.RestrictShares)
        return shares;

      var allowedShareIds = res.Result.GetAllowedShares();
      return shares.Where(s => allowedShareIds.Contains(s.ShareId)).ToList();
    }

    protected IList<IChannelGroup> FilterGroups(IList<IChannelGroup> channelGroups)
    {
      UserProfile userProfile = null;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userProfileDataManagement != null)
      {
        userProfile = (userProfileDataManagement.GetProfileAsync(UserId).Result)?.Result;
        if (userProfile != null)
        {
          IList<IChannelGroup> filteredGroups = new List<IChannelGroup>();
          foreach (IChannelGroup channelGroup in channelGroups)
          {
            IUserRestriction restriction = channelGroup as IUserRestriction;
            if (restriction != null && !userProfile.CheckUserAccess(restriction))
              continue;
            filteredGroups.Add(channelGroup);
          }
          return filteredGroups;
        }
      }
      return channelGroups;
    }

    public override string Class
    {
      get { return _containerClass; }
      set { _containerClass = value; }
    }

    protected Guid UserId
    {
      get => Client.UserId ?? Client.ClientId;
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
