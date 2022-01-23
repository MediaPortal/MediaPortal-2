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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Utilities;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class ResourceAccessUtils
  {
    internal static Guid? DefaultGuid = null;
    internal static string DefaultUser = "MP2Extended";

    internal static byte[] GetBytes(string str)
    {
      //ASCIIEncoding enc = new ASCIIEncoding();
      UTF8Encoding enc = new UTF8Encoding();
      return enc.GetBytes(str);
    }

    internal static Guid? GetUser(IOwinContext context)
    {
      var claim = context.Authentication.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Sid);
      var id = claim?.Value != null ? Guid.Parse(claim.Value) : (Guid?)null;
      if (id != null)
        return id;
      if (DefaultGuid.HasValue)
        return DefaultGuid;

      var userManagement = ServiceRegistration.Get<IUserProfileDataManagement>(false);
      if (userManagement != null)
      {
        var user = userManagement.GetProfileByNameAsync(DefaultUser).Result;
        if (user.Success)
          DefaultGuid = user.Result.ProfileId;
        else
          DefaultGuid = userManagement.CreateProfileAsync(DefaultUser, UserProfileType.UserProfile, "").Result;
      }
      return DefaultGuid;
    }

    internal static IList<IChannelGroup> FilterGroups(Guid? userId, IList<IChannelGroup> channelGroups)
    {
      UserProfile userProfile = null;
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userProfileDataManagement != null)
      {
        userProfile = (userProfileDataManagement.GetProfileAsync(userId.Value).Result)?.Result;
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

    internal static bool CheckUserAccess(UserProfile userProfile, IUserRestriction restrictedElement)
    {
      if (!userProfile.EnableRestrictionGroups || string.IsNullOrEmpty(restrictedElement.RestrictionGroup))
        return true;

      foreach (var group in restrictedElement.RestrictionGroup.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
        if (userProfile.RestrictionGroups.Contains(group))
          return true;
      return false;
    }

    internal static IFilter AppendUserFilter(Guid? userId, IFilter filter, ICollection<Guid> filterMias)
    {
      IFilter userFilter = null;
      if (userId.HasValue)
      {
        IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
        var res = userProfileDataManagement.GetProfileAsync(userId.Value).Result;
        if (res.Success)
          userFilter = res.Result.GetUserFilter(filterMias);
      }
      return filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : filter;
    }

    public static async Task<bool> AddPreferredLanguagesAsync(Guid? userId, IList<string> preferredAudioLanguuages, IList<string> preferredSubtitleLanguuages)
    {
      IUserProfileDataManagement userProfileDataManagement = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (userId.HasValue)
      {
        var res = userProfileDataManagement.GetProfileAsync(userId.Value).Result;
        if (res.Success)
        {
          await userProfileDataManagement.LoginProfileAsync(userId.Value);
          foreach (var lang in res.Result.GetPreferredAudioLanguages())
            preferredAudioLanguuages.Add(lang);
          foreach (var lang in res.Result.GetPreferredSubtitleLanguages())
            preferredSubtitleLanguuages.Add(lang);
        }
      }
      if (preferredAudioLanguuages.Count == 0)
        preferredAudioLanguuages = new List<string>() { "EN" };
      if (preferredSubtitleLanguuages.Count == 0)
        preferredSubtitleLanguuages = new List<string>() { "EN" };

      return true;
    }

    internal static WebMediaType GetWebMediaType(MediaItem item)
    {
      string type;
      if (item.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return WebMediaType.MusicAlbum;
      else if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_ARTIST)
        return WebMediaType.MusicArtist;
      else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        return WebMediaType.MusicTrack;
      else if (item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        return WebMediaType.TVEpisode;
      else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        return WebMediaType.Picture;
      else if (item.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
        return WebMediaType.Movie;
      else if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return WebMediaType.TVShow;
      else if (item.Aspects.ContainsKey(SeasonAspect.ASPECT_ID))
        return WebMediaType.TVSeason;

      return WebMediaType.File;
    }

    internal static IList<WebArtwork> GetWebArtwork(MediaItem item)
    {
      string mediaItemId = item.MediaItemId.ToString();
      string fileType = FanArtMediaTypes.Undefined;
      string type;
      if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_ACTOR)
        fileType = FanArtMediaTypes.Actor;
      else if (item.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Album;
      else if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_ARTIST)
        fileType = FanArtMediaTypes.Artist;
      else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Audio;
      else if (item.Aspects.ContainsKey(CharacterAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Character;
      else if (item.Aspects.ContainsKey(CompanyAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, CompanyAspect.ATTR_COMPANY_TYPE, out type) && type == CompanyAspect.COMPANY_PRODUCTION)
        fileType = FanArtMediaTypes.Company;
      else if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_COMPOSER)
        fileType = FanArtMediaTypes.Composer;
      else if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_DIRECTOR)
        fileType = FanArtMediaTypes.Director;
      else if (item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Episode;
      else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Image;
      else if (item.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Movie;
      else if (item.Aspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.MovieCollection;
      else if (item.Aspects.ContainsKey(CompanyAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, CompanyAspect.ATTR_COMPANY_TYPE, out type) && type == CompanyAspect.COMPANY_MUSIC_LABEL)
        fileType = FanArtMediaTypes.MusicLabel;
      else if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.Series;
      else if (item.Aspects.ContainsKey(SeasonAspect.ASPECT_ID))
        fileType = FanArtMediaTypes.SeriesSeason;
      else if (item.Aspects.ContainsKey(CompanyAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, CompanyAspect.ATTR_COMPANY_TYPE, out type) && type == CompanyAspect.COMPANY_TV_NETWORK)
        fileType = FanArtMediaTypes.TVNetwork;
      else if (item.Aspects.ContainsKey(PersonAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(item.Aspects, PersonAspect.ATTR_OCCUPATION, out type) && type == PersonAspect.OCCUPATION_WRITER)
        fileType = FanArtMediaTypes.Writer;

      var cache = ServiceRegistration.Get<IFanArtCache>();
      var banners = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.Banner);
      var covers = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.Cover);
      var logos = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.Logo);
      var posters = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.Poster);
      var backdrops = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.Thumbnail);
      var content = cache.GetFanArtFiles(item.MediaItemId, FanArtTypes.FanArt);
      var list = new List<WebArtwork>();
      for (int idx = 0; idx < banners.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Banner, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < backdrops.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Backdrop, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < content.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Content, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < covers.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Cover, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < logos.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Logo, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < posters.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId, Type = Common.WebFileType.Poster, Filetype = fileType, Offset = idx });

      return list;
    }

    internal static IList<WebArtwork> GetWebArtwork(WebMediaType mediaType, Guid mediaItemId)
    {
      string fileType = FanArtMediaTypes.Undefined;
      string type;
      if (mediaType == WebMediaType.Movie)
        fileType = FanArtMediaTypes.Movie;
      else if (mediaType == WebMediaType.MusicAlbum)
        fileType = FanArtMediaTypes.Album;
      else if (mediaType == WebMediaType.MusicArtist)
        fileType = FanArtMediaTypes.Artist;
      else if (mediaType == WebMediaType.MusicTrack)
        fileType = FanArtMediaTypes.Audio;
      else if (mediaType == WebMediaType.Picture)
        fileType = FanArtMediaTypes.Image;
      else if (mediaType == WebMediaType.TVEpisode)
        fileType = FanArtMediaTypes.Episode;
      else if (mediaType == WebMediaType.TVSeason)
        fileType = FanArtMediaTypes.SeriesSeason;
      else if (mediaType == WebMediaType.TVShow)
        fileType = FanArtMediaTypes.Series;

      var cache = ServiceRegistration.Get<IFanArtCache>();
      var banners = cache.GetFanArtFiles(mediaItemId, FanArtTypes.Banner);
      var covers = cache.GetFanArtFiles(mediaItemId, FanArtTypes.Cover);
      var logos = cache.GetFanArtFiles(mediaItemId, FanArtTypes.Logo);
      var posters = cache.GetFanArtFiles(mediaItemId, FanArtTypes.Poster);
      var backdrops = cache.GetFanArtFiles(mediaItemId, FanArtTypes.Thumbnail);
      var content = cache.GetFanArtFiles(mediaItemId, FanArtTypes.FanArt);
      var list = new List<WebArtwork>();
      for (int idx = 0; idx < banners.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Banner, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < backdrops.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Backdrop, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < content.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Content, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < covers.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Cover, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < logos.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Logo, Filetype = fileType, Offset = idx });
      for (int idx = 0; idx < posters.Count; idx++)
        list.Add(new WebArtwork { Id = mediaItemId.ToString(), Type = Common.WebFileType.Poster, Filetype = fileType, Offset = idx });

      return list;
    }

    internal static IList<ResourcePath> GetResourcePaths(MediaItem item)
    {
      List<ResourcePath> paths = new List<ResourcePath>();
      IList<MultipleMediaItemAspect> providerResourceAspects;
      if (MediaItemAspect.TryGetAspects(item.Aspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
      {
        foreach (var res in providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY))
        {
          var resourcePathStr = res.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());
          paths.Add(resourcePath);
        }
      }
      return paths;
    }

    internal static IList<string> GetPaths(MediaItem item)
    {
      var resPaths = GetResourcePaths(item);

      List<string> paths = new List<string>();
      foreach (var res in resPaths)
      {
        string path = res.PathSegments.Count > 0 ? StringUtils.RemovePrefixIfPresent(res.LastPathSegment.Path, "/") : string.Empty;
        paths.Add(path);
      }
      return paths;
    }
  }
}
