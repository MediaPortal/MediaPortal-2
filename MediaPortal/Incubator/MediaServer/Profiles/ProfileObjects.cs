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
using System.Linq;
using System.Reflection;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.DIDL;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Filters;
using MediaPortal.Extensions.MediaServer.Objects.Basic;
using MediaPortal.Extensions.MediaServer.Objects.MediaLibrary;
using MediaPortal.Extensions.MediaServer.Protocols;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup;
using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Common.Localization;
using MediaPortal.Utilities;
using System.Collections.Concurrent;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Common.UserProfileDataManagement;
using System.Threading.Tasks;
using MediaPortal.Extensions.MediaServer.Profiles;
using System.Net;

namespace MediaPortal.Extensions.MediaServer.Profiles
{
  #region Profile

  public class MediaMimeMapping
  {
    public string MIME = null;
    public string MIMEName = null;
    public string MappedMediaFormat = null;
  }

  public class UpnpDeviceInformation
  {
    public MediaServerUpnPDeviceInformation DeviceInformation = new MediaServerUpnPDeviceInformation();
    public string AdditionalElements = null;
  }

  #endregion

  #region Client settings

  public enum ThumbnailDelivery
  {
    None,
    All,
    Resource,
    AlbumArt,
    Icon
  }

  public enum MetadataDelivery
  {
    All,
    Required
  }

  public class ThumbnailSettings
  {
    public int MaxWidth = 160;
    public int MaxHeight = 160;
    public ThumbnailDelivery Delivery = ThumbnailDelivery.All;
  }

  public class CommunicationSettings
  {
    public bool AllowChunckedTransfer = true;
    public int DefaultBufferSize = 1500;
    public long InitialBufferSize = 0;
  }

  public class MetadataSettings
  {
    public MetadataDelivery Delivery = MetadataDelivery.All;
  }

  public class ProfileSettings
  {
    public ThumbnailSettings Thumbnails = new ThumbnailSettings();
    public CommunicationSettings Communication = new CommunicationSettings();
    public MetadataSettings Metadata = new MetadataSettings();
    public BasicContainer RootContainer = null;
  }

  public class Detection
  {
    public Dictionary<string, string> HttpHeaders = new Dictionary<string, string>();
    public UpnpSearch UPnPSearch = new UpnpSearch();
  }

  public class UpnpSearch
  {
    public UpnpSearch()
    {
      FriendlyName = null;
      ModelName = null;
      ModelNumber = null;
      ProductNumber = null;
      Server = null;
      Manufacturer = null;
    }

    public string FriendlyName { get; set; }
    public string ModelName { get; set; }
    public string ModelNumber { get; set; }
    public string ProductNumber { get; set; }
    public string Server { get; set; }
    public string Manufacturer { get; set; }

    public int Count()
    {
      PropertyInfo[] properties = typeof(UpnpSearch).GetProperties();
      return properties.Count(property => property.GetValue(this) != null);
    }
  }

  public class EndPointSettings
  {
    public EndPointProfile Profile { get; set; } = null;
    public IEnumerable<string> PreferredSubtitleLanguages { get; set; } = new List<string>();
    public IEnumerable<string> PreferredAudioLanguages { get; set; } = new List<string>();
    public Guid ClientId { get; set; } = Guid.Empty;
    public string ClientName { get; set; }
    public Guid? UserId { get; set; } = null;
    public bool EstimateTransodedSize { get; set; } = true;
    public bool AutoProfile { get; set; } = true;
    public BasicContainer RootContainer { get; private set; } = null;
    public ConcurrentDictionary<Guid, DlnaMediaItem> DlnaMediaItems { get; } = new ConcurrentDictionary<Guid, DlnaMediaItem>();
    public ConcurrentDictionary<int, DlnaMediaItem> DlnaLiveItems { get; } = new ConcurrentDictionary<int, DlnaMediaItem>();

    public static string GetClientName(IPAddress ip)
    {
      return $"DLNA ({ip.ToString()})";
    }

    public DlnaMediaItem GetDlnaItem(Guid mediaItemId)
    {
      DlnaMediaItem dlnaItem;
      if (DlnaMediaItems.TryGetValue(mediaItemId, out dlnaItem))
        return dlnaItem;

      return null;
    }

    public DlnaMediaItem GetDlnaItem(MediaItem item, int? edition = null)
    {
      DlnaMediaItem dlnaItem;
      if (DlnaMediaItems.TryGetValue(item.MediaItemId, out dlnaItem))
        return dlnaItem;

      dlnaItem = new DlnaMediaItem(this);
      dlnaItem.Initialize(item, edition).Wait();
      DlnaMediaItems.TryAdd(item.MediaItemId, dlnaItem);
      return dlnaItem;
    }

    public DlnaMediaItem GetLiveDlnaItem(int channelId)
    {
      DlnaMediaItem dlnaItem;
      if (DlnaLiveItems.TryGetValue(channelId, out dlnaItem))
        return dlnaItem;

      return null;
    }

    public MediaItem StoreLiveDlnaItem(int channelId)
    {
      var dlnaItem = new DlnaMediaItem(this);
      var mediaItem = dlnaItem.InitializeChannel(channelId).Result;
      DlnaLiveItems.TryRemove(channelId, out _);
      DlnaLiveItems.TryAdd(channelId, dlnaItem);
      return mediaItem;
    }

    private void InitialiseContainerTree()
    {
      if (Profile == null) return;

      ILocalization language = ServiceRegistration.Get<ILocalization>();

      List<BasicContainer> mediaRoots = new List<BasicContainer>();
      RootContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_ROOT_KEY, this)
      { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ROOT)) ?? "MediaPortal Media Library" };
      if (MediaServerPlugin.Settings.ShowUserLogin)
      {
        IUserProfileDataManagement userManager = ServiceRegistration.Get<IUserProfileDataManagement>();
        var users = userManager.GetProfilesAsync().Result?.Where(u => u.ProfileType == UserProfileType.UserProfile);
        if (users?.Count() > 0)
        {
          foreach (var user in users.Where(u => u.ProfileType == UserProfileType.UserProfile))
          {
            var userContainer = new BasicContainer($"{MediaLibraryHelper.CONTAINER_USERS_KEY}>{user.ProfileId}", this)
            { Title = user.Name };
            RootContainer.Add(userContainer);
            mediaRoots.Add(userContainer);
          }
        }
      }
      if (mediaRoots.Count == 0)
      {
        mediaRoots.Add(RootContainer);
      }

      foreach (var mediaRoot in mediaRoots)
      {
        var audioContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_AUDIO)) ?? "Audio" };
        audioContainer.Add(new MediaLibraryAlbumContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "AL", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ALBUM)) ?? "Albums" });
        audioContainer.Add(new MediaLibraryMusicRecentContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "RA", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_RECENT)) ?? "Recently Added" });
        audioContainer.Add(new MediaLibraryAlbumArtistContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "AR", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ALBUM_ARTIST)) ?? "Album Artists" });
        audioContainer.Add(new MediaLibraryMusicArtistContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "AAR", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ARTIST)) ?? "Artists" });
        audioContainer.Add(new MediaLibraryMusicGenreContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "G", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_GENRE)) ?? "Genres" });
        audioContainer.Add(new MediaLibraryMusicYearContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "AY", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_YEAR)) ?? "Year" });
        audioContainer.Add(new MediaLibraryShareContainer(MediaLibraryHelper.CONTAINER_AUDIO_KEY + "AS", this, "Audio")
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_SHARE)) ?? "Shares" });
        mediaRoot.Add(audioContainer);

        var pictureContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_IMAGES_KEY, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_IMAGE)) ?? "Images" };
        pictureContainer.Add(new MediaLibraryImageAlbumContainer(MediaLibraryHelper.CONTAINER_IMAGES_KEY + "IA", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ALBUM)) ?? "Albums" });
        pictureContainer.Add(new MediaLibraryImageRecentContainer(MediaLibraryHelper.CONTAINER_IMAGES_KEY + "RA", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_RECENT)) ?? "Recently Added" });
        pictureContainer.Add(new MediaLibraryImageYearContainer(MediaLibraryHelper.CONTAINER_IMAGES_KEY + "IY", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_YEAR)) ?? "Year" });
        pictureContainer.Add(new MediaLibraryShareContainer(MediaLibraryHelper.CONTAINER_IMAGES_KEY + "IS", this, "Image")
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_SHARE)) ?? "Shares" });
        mediaRoot.Add(pictureContainer);

        var videoContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_VIDEO)) ?? "Video" };
        var movieContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "M", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_MOVIE)) ?? "Movies" };
        movieContainer.Add(new MediaLibraryMovieContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MT", null, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_TITLE)) ?? "Titles" });
        movieContainer.Add(new MediaLibraryMovieRecentContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MRA", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_RECENT)) ?? "Recently Added" });
        movieContainer.Add(new MediaLibraryMovieUnwatchedContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MU", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_UNWATCHED)) ?? "Unwatched" });
        movieContainer.Add(new MediaLibraryMovieActorsContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MAR", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ACTOR)) ?? "Actors" });
        movieContainer.Add(new MediaLibraryMovieGenreContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MG", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_GENRE)) ?? "Genres" });
        movieContainer.Add(new MediaLibraryMovieYearContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "MY", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_YEAR)) ?? "Year" });
        videoContainer.Add(movieContainer);
        var seriesContainer = new BasicContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "S", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_SERIES)) ?? "Series" };
        seriesContainer.Add(new MediaLibrarySeriesContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "ST", null, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_TITLE)) ?? "Titles" });
        seriesContainer.Add(new MediaLibrarySeriesActorsContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "SAR", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_ACTOR)) ?? "Actors" });
        seriesContainer.Add(new MediaLibrarySeriesGenresContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "SG", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_GENRE)) ?? "Genres" });
        seriesContainer.Add(new MediaLibrarySeriesYearContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "SY", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_YEAR)) ?? "Year" });
        seriesContainer.Add(new MediaLibrarySeriesRecentContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "SRA", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_RECENT)) ?? "Recently Added" });
        seriesContainer.Add(new MediaLibrarySeriesUnwatchedContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "SU", this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_UNWATCHED)) ?? "Unwatched" });
        videoContainer.Add(seriesContainer);
        videoContainer.Add(new MediaLibraryShareContainer(MediaLibraryHelper.CONTAINER_VIDEO_KEY + "VS", this, "Video")
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_SHARE)) ?? "Shares" });
        mediaRoot.Add(videoContainer);

        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          mediaRoot.Add(new MediaLibraryBroadcastGroupContainer(MediaLibraryHelper.CONTAINER_BROADCAST_KEY, this)
          { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_BROADCAST)) ?? "Broadcasts" });
        }

        mediaRoot.Add(new MediaLibraryShareContainer(MediaLibraryHelper.CONTAINER_MEDIA_SHARES_KEY, this)
        { Title = StringUtils.TrimToNull(language.ToString(Consts.RES_SHARE)) ?? "Shares" });
      }
    }

    public async Task InitializeAsync(string id)
    {
      if (RootContainer == null)
      {
        IUserProfileDataManagement userManager = ServiceRegistration.Get<IUserProfileDataManagement>();
        ClientName = $"DLNA ({id})";
        var profile = await userManager.GetProfileByNameAsync(ClientName);
        if (profile.Success)
          ClientId = profile.Result.ProfileId;
        else
          ClientId = await userManager.CreateProfileAsync(ClientName, UserProfileType.ClientProfile, "");
        await userManager.LoginProfileAsync(ClientId);

        if (UserId.HasValue)
          await InitializeUserAsync();

        InitialiseContainerTree();
      }
    }

    public async Task InitializeUserAsync()
    {
      IUserProfileDataManagement userManager = ServiceRegistration.Get<IUserProfileDataManagement>();
      if (UserId.HasValue)
      {
        await userManager.LoginProfileAsync(UserId.Value);
        var audioList = await userManager.GetUserAdditionalDataListAsync(UserId.Value, UserDataKeysKnown.KEY_PREFERRED_AUDIO_LANGUAGE);
        PreferredAudioLanguages = audioList.Result?.Select(l => l.Item2);
        if (!(PreferredAudioLanguages?.Any() ?? false))
          PreferredAudioLanguages = new List<string>() { "EN" };
        var subtitleList = await userManager.GetUserAdditionalDataListAsync(UserId.Value, UserDataKeysKnown.KEY_PREFERRED_SUBTITLE_LANGUAGE);
        PreferredSubtitleLanguages = subtitleList.Result?.Select(l => l.Item2);
        if (!(PreferredSubtitleLanguages?.Any() ?? false))
          PreferredSubtitleLanguages = new List<string>() { "EN" };
      }
    }
  }

  public class EndPointProfile
  {
    public bool Active { get; set; } = false;
    public string ID { get; set; } = "";
    public string Name { get; set; } = "?";
    public UpnpDeviceInformation UpnpDevice { get; set; } = new UpnpDeviceInformation();
    public GenericDidlMessageBuilder.ContentBuilder DirectoryContentBuilder { get; set; } = GenericDidlMessageBuilder.ContentBuilder.GenericContentBuilder;
    public GenericAccessProtocol.ResourceAccessProtocol ResourceAccessHandler { get; set; } = GenericAccessProtocol.ResourceAccessProtocol.GenericAccessProtocol;
    public GenericContentDirectoryFilter.ContentFilter DirectoryContentFilter { get; set; } = GenericContentDirectoryFilter.ContentFilter.GenericContentFilter;
    public ProtocolInfoFormat ProtocolInfo { get; set; } = ProtocolInfoFormat.DLNA;
    public ProfileSettings Settings { get; set; } = new ProfileSettings();
    public TranscodingSetup MediaTranscoding
    {
      get
      {
        return TranscodeProfileManager.GetTranscodeProfile(ProfileManager.TRANSCODE_PROFILE_SECTION, ID);
      }
    }
    public Dictionary<string, MediaMimeMapping> MediaMimeMap { get; set; } = new Dictionary<string, MediaMimeMapping>();
    public List<Detection> Detections { get; set; } = new List<Detection>();

    public override string ToString()
    {
      return ID + " - " + Name;
    }

    private ITranscodeProfileManager TranscodeProfileManager
    {
      get { return ServiceRegistration.Get<ITranscodeProfileManager>(); }
    }
  }

  #endregion
}
