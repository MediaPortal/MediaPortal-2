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
using MediaPortal.Plugins.MediaServer.Profiles;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common;
using MediaPortal.Common.Certifications;

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
      EpisodeAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_EPISODE_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
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
      MovieAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID
    };
    protected static readonly Guid[] OPTIONAL_MOVIE_MIA_TYPE_IDS = {
      GenreAspect.ASPECT_ID,
    };

    protected static readonly Guid[] NECESSARY_PERSON_MIA_TYPE_IDS = {
      MediaAspect.ASPECT_ID,
      PersonAspect.ASPECT_ID,
    };
    protected static readonly Guid[] OPTIONAL_PERSON_MIA_TYPE_IDS = null;

    protected readonly List<BasicObject> _children = new List<BasicObject>();
    protected readonly Guid? _userId;

    private string _containerClass = "object.container";

    public BasicContainer(string id, EndPointSettings client) 
      : base(id, client)
    {
      _userId = client.UserId ?? client.ClientId;

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
      Logger.Debug("MediaServer added {0} to {1}, now {2} children", node.Key, Key, _children.Count);
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

    protected IFilter AppendUserFilter(IFilter filter, IEnumerable<Guid> necessaryMias)
    {
      IFilter userFilter = GetUserCertificateFilter(necessaryMias);
      return filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : null;
    }

    private IFilter GetUserCertificateFilter(IEnumerable<Guid> necessaryMias)
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      ICollection<Share> shares = library.GetShares(null)?.Values;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      UserProfile userProfile;
      if (!_userId.HasValue || !userProfileDataManagement.GetProfile(_userId.Value, out userProfile))
      {
        return null;
      }

      int? allowedAge = null;
      bool? includeParentalGuidedContent = null;
      bool? includeUnratedContent = null;
      bool allowAllShares = true;
      bool allowAllAges = true;
      List<IFilter> shareFilters = new List<IFilter>();
      foreach (var key in userProfile.AdditionalData)
      {
        foreach (var val in key.Value)
        {
          if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_SHARES)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              allowAllShares = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOWED_SHARE)
          {
            Guid shareId = new Guid(val.Value);
            if (shares == null || !shares.Where(s => s.ShareId == shareId).Any())
              continue;
            shareFilters.Add(new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, shares.Where(s => s.ShareId == shareId).First().BaseResourcePath + "%", null, true));
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_AGES)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              allowAllAges = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_ALLOWED_AGE)
          {
            string age = val.Value;
            if (!string.IsNullOrEmpty(age) && Convert.ToInt32(age) >= 0)
            {
              allowedAge = Convert.ToInt32(age);
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              includeParentalGuidedContent = Convert.ToInt32(allow) > 0;
            }
          }
          else if (key.Key == UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT)
          {
            string allow = val.Value;
            if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
            {
              includeUnratedContent = Convert.ToInt32(allow) > 0;
            }
          }
        }
      }

      List<IFilter> filters = new List<IFilter>();

      // Shares filter
      if (allowAllShares == false)
      {
        if (shareFilters.Count > 0)
          filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, shareFilters.ToArray()));
        else
          filters.Add(new RelationalFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RelationalOperator.EQ, ""));
      }

      // Content filter
      if (allowedAge.HasValue && allowAllAges == false)
      {
        if (necessaryMias.Contains(MovieAspect.ASPECT_ID))
        {
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetMovieCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new InFilter(MovieAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)));
            else
              filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(MovieAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(MovieAspect.ATTR_CERTIFICATION)));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new NotFilter(new EmptyFilter(MovieAspect.ATTR_CERTIFICATION)));
          }
        }
        else if (necessaryMias.Contains(SeriesAspect.ASPECT_ID))
        {
          //TODO: Should series filters reset the share filter? Series have no share dependency
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetSeriesCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)));
            else
              filters.Add(BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION)));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new NotFilter(new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION)));
          }
        }
        else if (necessaryMias.Contains(EpisodeAspect.ASPECT_ID))
        {
          IEnumerable<CertificationMapping> certs = CertificationMapper.GetSeriesCertificationsForAge(allowedAge.Value, includeParentalGuidedContent ?? false);
          if (certs.Count() > 0)
          {
            if (!includeUnratedContent ?? false)
              filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId))));
            else
              filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES,
                BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,
                new InFilter(SeriesAspect.ATTR_CERTIFICATION, certs.Select(c => c.CertificationId)),
                new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION))));
          }
          else if (!includeUnratedContent ?? false)
          {
            filters.Add(new FilteredRelationshipFilter(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES,
                new NotFilter(new EmptyFilter(SeriesAspect.ATTR_CERTIFICATION))));
          }
        }
      }

      if (filters.Count > 1)
        return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filters.ToArray());
      else if (filters.Count > 0)
        return filters[0];

      return null;
    }

    protected ICollection<Guid> GetAllowedShares()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      UserProfile userProfile;
      bool allowAllShares = true;
      ICollection<Guid> allowedShares = new List<Guid>();
      if (_userId.HasValue && userProfileDataManagement.GetProfile(_userId.Value, out userProfile))
      {
        foreach (var key in userProfile.AdditionalData)
        {
          foreach (var val in key.Value)
          {
            if (key.Key == UserDataKeysKnown.KEY_ALLOW_ALL_SHARES)
            {
              string allow = val.Value;
              if (!string.IsNullOrEmpty(allow) && Convert.ToInt32(allow) >= 0)
              {
                allowAllShares = Convert.ToInt32(allow) > 0;
              }
            }
            else if (key.Key == UserDataKeysKnown.KEY_ALLOWED_SHARE)
            {
              Guid shareId = new Guid(val.Value);
              allowedShares.Add(shareId);
            }
          }
        }
      }
      if (allowAllShares)
        return null;
      return allowedShares;
    }

    public override string Class
    {
      get { return _containerClass; }
      set { _containerClass = value; }
    }

    protected Guid? UserId
    {
      get => _userId;
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
