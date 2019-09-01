#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.Plugins;
using MediaPortal.Plugins.WifiRemote.Settings;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.WifiRemote
{
  [TestFixture]
  public class WifiRemoteTests
  {
    private const int WIFIREMOTE_PORT = 8017;
    private const string WIFIREMOTE_PASSCODE = "mediaportal";

    private WifiRemotePlugin _wifi = null;
    private Socket _socket = null;
    private byte[] _receiveBuffer = new byte[4096];
    private AutoResetEvent _messageReceived = new AutoResetEvent(false);
    private ManualResetEvent _welcomeReceived = new ManualResetEvent(false);
    private ManualResetEvent _loginReceived = new ManualResetEvent(false);
    private ManualResetEvent _statusReceived = new ManualResetEvent(false);
    private ConcurrentDictionary<string, BaseReceiveMessage> _receivedResponses = new ConcurrentDictionary<string, BaseReceiveMessage>();
    private MessageWelcomeResponse _welcomeResponse;
    private MessageAuthenticationResponse _loginResponse;
    private MessageStatusResponse _statusResponse;

    [OneTimeSetUp]
    public void SetUp()
    {
      SetMockServices();

      _wifi = new WifiRemotePlugin();
      _wifi.Activated(null);

      _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      _socket.Connect("127.0.0.1", WIFIREMOTE_PORT);
      if (_socket.Connected)
      {
        _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, new AsyncCallback(OnData), null);

        //Send passcode
        var id = new MessageIdentify
        {
          Name = "Test",
          Description = "Test description",
          Application = "Test application",
          Version = "1.0",
          Authenticate = new Auth { AuthMethod = "passcode", PassCode = WIFIREMOTE_PASSCODE }
        };
        SendData(id, false);
      }
    }

    [OneTimeTearDown]
    public void CleanUp()
    {
      _socket.Close();
      _wifi.Shutdown();
    }

    [Test]
    public void TestWelcomeMessage()
    {
      _welcomeReceived.WaitOne(5000);
      Assert.IsNotNull(_welcomeResponse, "Welcome message failed");
    }

    [Test]
    public void TestMoviesMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("movies") { Action = "movielist" }, "movies");
      Assert.IsNotNull(response, "Movie list message failed");
      Assert.AreEqual(3, response.Movies.Count, "Unexpected number ({0}) of movies in list", response.Movies.Count);
    }

    [Test]
    public void TestVideosMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("videos") { Action = "videolist" }, "videos");
      Assert.IsNotNull(response, "Video list message failed");
      Assert.AreEqual(6, response.Videos.Count, "Unexpected number ({0}) of videos in list", response.Videos.Count);
    }

    [Test]
    public void TestSeriesMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("series") { Action = "serieslist" }, "series");
      Assert.IsNotNull(response, "Series list message failed");
      Assert.AreEqual(1, response.Series.Count, "Unexpected number ({0}) of series in list", response.Series.Count);
    }

    [Test]
    public void TestSeasonMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("series") { Action = "seasonlist", SeriesName = "Series 1" }, "seasons");
      Assert.IsNotNull(response, "Season list message failed");
      Assert.AreEqual(1, response.Seasons.Count, "Unexpected number ({0}) of seasons in list", response.Seasons.Count);
    }

    [Test]
    public void TestEpisodeMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("series") { Action = "episodelist", SeriesName = "Series 1", SeasonNumber = 1 }, "episodes");
      Assert.IsNotNull(response, "Episode list message failed");
      Assert.AreEqual(3, response.Episodes.Count, "Unexpected number ({0}) of episodes in list", response.Episodes.Count);
    }

    [Test]
    public void TestAlbumMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("music") { Action = "albumlist" }, "albums");
      Assert.IsNotNull(response, "Album list message failed");
      Assert.AreEqual(1, response.Albums.Count, "Unexpected number ({0}) of albums in list", response.Albums.Count);
    }

    [Test]
    public void TestTracksMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("music") { Action = "tracklist", AlbumName = "Album 1", SeasonNumber = 1 }, "music");
      Assert.IsNotNull(response, "Track list message failed");
      Assert.AreEqual(3, response.Music.Count, "Unexpected number ({0}) of tracks in list", response.Music.Count);
    }

    [Test]
    public void TestImagesMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("images") { Action = "imagelist" }, "images");
      Assert.IsNotNull(response, "Image list message failed");
      Assert.AreEqual(1, response.Images.Count, "Unexpected number ({0}) of images in list", response.Images.Count);
    }

    [Test]
    public void TestPlaylistMessage()
    {
      var contentDir = ServiceRegistration.Get<IContentDirectory>();
      var items = contentDir.SearchAsync(new MediaItemQuery(new Guid[] { AudioAspect.ASPECT_ID }, null), false, null, true).Result;
      List<object> itemList = new List<object>();
      foreach (var item in items)
      {
        var j = new JObject();
        j.Add("FileName", "");
        j.Add("FileId", item.MediaItemId.ToString());
        itemList.Add(j);
      }
      string playListName = "Test";

      ReadData<object>(new MessageAction("playlist") { Action = "new", PlaylistItems = itemList }, "");
      var response = ReadData<MessagePlaylistDetails>(new MessageAction("playlist") { Action = "get" }, "playlistdetails");
      Assert.IsNotNull(response, "Playlist detailes message failed");
      Assert.IsTrue(response.PlaylistItems.Count == itemList.Count, "Test playlist not containing correct number of items ({0})", response.PlaylistItems.Count);

      ReadData<object>(new MessageAction("playlist") { Action = "save", PlayListName = playListName }, "");

      ReadData<object>(new MessageAction("playlist") { Action = "clear" }, "");
      response = ReadData<MessagePlaylistDetails>(new MessageAction("playlist") { Action = "get" }, "playlistdetails");
      Assert.IsNotNull(response, "Playlist detailes message failed");
      Assert.IsTrue(response.PlaylistItems.Count == 0, "Test playlist not cleared");

      var playLists = ReadData<MessageListResponse>(new MessageAction("playlist") { Action = "list" }, "playlists");
      Assert.IsNotNull(playLists, "Playlist message failed");
      Assert.IsTrue(playLists.PlayLists.Contains(playListName), "Test playlist is missing");

      ReadData<MessageListResponse>(new MessageAction("playlist") { Action = "load", PlayListName = playListName }, "");
      response = ReadData<MessagePlaylistDetails>(new MessageAction("playlist") { Action = "get" }, "playlistdetails");
      Assert.IsNotNull(response, "Playlist detailes message failed");
      Assert.IsTrue(response.PlaylistItems.Select(p => p.FileId).SequenceEqual(itemList.Select(i => ((JObject)i)["FileId"].ToString())), "Test playlist doesn't contain the correct items");

      ReadData<MessageListResponse>(new MessageAction("playlist") { Action = "remove", Index = 1 }, ""); //Index
      ReadData<MessageListResponse>(new MessageAction("playlist") { Action = "move", OldIndex = 0, NewIndex = 1 }, "");
      response = ReadData<MessagePlaylistDetails>(new MessageAction("playlist") { Action = "get" }, "playlistdetails");
      Assert.IsNotNull(response, "Playlist detailes message failed");
      var first = ((JObject)itemList.First())["FileId"].ToString();
      var last = ((JObject)itemList.Last())["FileId"].ToString();
      Assert.IsTrue(response.PlaylistItems.Select(p => p.FileId).SequenceEqual(new[] { last, first }), "Test playlist manipulation failed");
    }

    [Test]
    public void TestStatusMessage()
    {
      _statusResponse = null;
      _statusReceived.Reset();
      ReadData<MessageListResponse>(new MessageAction("requeststatus"), "");
      _statusReceived.WaitOne(5000);
      Assert.IsNotNull(_statusResponse, "Status message failed");
    }

    [Test]
    public void TestNowPlayingMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("requestnowplaying"), "nowplaying");
      Assert.IsNotNull(response, "Now playing message failed");
    }

    [Test]
    public void TestRecordingsMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("recordings") { Action = "recordinglist" }, "recordings");
      Assert.IsNotNull(response, "Recording list message failed");
      Assert.AreEqual(0, response.Recordings.Count, "Unexpected number ({0}) of recordings in list", response.Recordings.Count);
    }

    [Test]
    public void TestTvMessage()
    {
      var groups = ReadData<MessageListResponse>(new MessageAction("tv") { Action = "grouplist" }, "channelgroups");
      Assert.IsNotNull(groups, "TV group list message failed");
      Assert.AreEqual(1, groups.ChannelGroups.Count, "Unexpected number ({0}) of groups in TV group list", groups.ChannelGroups.Count);

      var programs = ReadData<MessageListResponse>(new MessageAction("tv") { Action = "groupepglist", ChannelGroupId = 1, Hours = 12 }, "programs");
      Assert.IsNotNull(programs, "TV group EPG list message failed");
      Assert.AreEqual(3, programs.Programs.Count, "Unexpected number ({0}) of programs in TV group EPG list", programs.Programs.Count);

      var channels = ReadData<MessageListResponse>(new MessageAction("tv") { Action = "channellist", ChannelGroupId = 1 }, "channels");
      Assert.IsNotNull(channels, "TV channel list message failed");
      Assert.AreEqual(1, channels.Channels.Count, "Unexpected number ({0}) of channels TV channel in list", channels.Channels.Count);

      programs = ReadData<MessageListResponse>(new MessageAction("tv") { Action = "channelepglist", ChannelId = 1, Hours = 12 }, "programs");
      Assert.IsNotNull(programs, "TV channel EPG list message failed");
      Assert.AreEqual(3, programs.Programs.Count, "Unexpected number ({0}) of programs in TV channel EPG list", programs.Programs.Count);
    }

    [Test]
    public void TestSchedulesMessage()
    {
      var response = ReadData<MessageListResponse>(new MessageAction("schedules") { Action = "schedulelist" }, "schedules");
      Assert.IsNotNull(response, "Schedule list message failed");
      Assert.AreEqual(1, response.Schedules.Count, "Unexpected number ({0}) of schedules in list", response.Schedules.Count);
    }

    #region Messages

    private interface IMessage
    {
      /// <summary>
      /// Type is a required attribute for all messages. 
      /// The client decides by this attribute what message was sent.
      /// </summary>
      string Type
      {
        get;
      }
    }

    private abstract class BaseSendMessage : IMessage
    {
      public abstract string Type { get; }
      public string AutologinKey
      {
        get;
        set;
      }
    }

    private abstract class BaseReceiveMessage : IMessage
    {
      public string Type
      {
        get;
        set;
      }
    }

    private class MessageCommand : BaseSendMessage
    {
      public override string Type => "command";
      public string Command { get; set; }
    }

    private class MessageAction : BaseSendMessage
    {
      private string _type;

      public MessageAction(string type)
      {
        _type = type;
      }

      public List<object> PlaylistItems { get; set; }
      public string PlayListName { get; set; }
      public int Index { get; set; }
      public int OldIndex { get; set; }
      public int NewIndex { get; set; }

      public override string Type => _type;
      public string Action { get; set; }
      public string Search { get; set; }

      public string ImagePath { get; set; }
      public string ImageId { get; set; }

      public string MovieName { get; set; }
      public string MovieId { get; set; }

      public string VideoName { get; set; }
      public string VideoId { get; set; }

      public string SeriesName { get; set; }
      public string SeriesId { get; set; }
      public int SeasonNumber { get; set; }
      public bool OnlyUnwatchedEpisodes { get; set; }

      public string AlbumName { get; set; }
      public string AlbumId { get; set; }
      public int DiscNumber { get; set; }
      public bool OnlyUnplayedTracks { get; set; }

      public string RecordingName { get; set; }
      public string RecordingId { get; set; }

      public int ChannelGroupId { get; set; }
      public int ChannelId { get; set; }
      public int Hours { get; set; }
    }

    private class MessageIdentify : BaseSendMessage
    {
      public override string Type => "identify";
      public string Name { get; set; }
      public string Description { get; set; }
      public string Application { get; set; }
      public string Version { get; set; }
      public Auth Authenticate { get; set; }
    }

    private class Auth
    {
      public string AuthMethod { get; set; }
      public string User { get; set; }
      public string Password { get; set; }
      public string PassCode { get; set; }
    }

    private class MessageWelcomeResponse : BaseReceiveMessage
    {
      public int Server_Version { get; set; }
      public AuthMethod AuthMethod { get; set; }
      public bool TvPluginInstalled { get; set; }
    }

    private class MessageAuthenticationResponse : BaseReceiveMessage
    {
      public bool Success { get; set; }
      public string ErrorMessage { get; set; }
      public string AutologinKey { get; set; }
    }

    private class MessageStatusResponse : BaseReceiveMessage
    {
      public bool IsPlaying { get; set; }
      public bool IsPaused { get; set; }
      public bool IsPlayerOnTop { get; set; }
      public string Title { get; set; }
      public string CurrentModule { get; set; }
    }

    private class MessageListResponse : BaseReceiveMessage
    {
      public List<JObject> Movies { get; set; }
      public List<JObject> Images { get; set; }
      public List<JObject> Videos { get; set; }
      public List<JObject> Series { get; set; }
      public List<JObject> Seasons { get; set; }
      public List<JObject> Schedules { get; set; }
      public List<JObject> Recordings { get; set; }
      public List<JObject> Programs { get; set; }
      public List<string> PlayLists { get; set; }
      public List<JObject> Music { get; set; }
      public List<JObject> Episodes { get; set; }
      public List<JObject> Channels { get; set; }
      public List<JObject> ChannelGroups { get; set; }
      public List<JObject> Albums { get; set; }
    }

    private class MessagePlaylistDetails : BaseReceiveMessage
    {
      public bool PlaylistRepeat { get; set; }
      public String PlaylistName { get; set; }
      public String PlaylistType { get; set; }
      public List<PlaylistEntry> PlaylistItems { get; set; }
    }

    private class PlaylistEntry
    {
      public string MediaType { get; set; }
      public String Id { get; set; }
      public int MpMediaType { get; set; }
      public int MpProviderId { get; set; }
      public String Name { get; set; }
      public String Name2 { get; set; }
      public String AlbumArtist { get; set; }
      public String FileName { get; set; }
      public String FileId { get; set; }
      public int Duration { get; set; }
      public bool Played { get; set; }
    }

    #endregion

    #region Test Classes

    private class TestContentDirectory : IContentDirectory
    {
      private List<MediaItem> library = new List<MediaItem>();
      private List<PlaylistRawData> playlists = new List<PlaylistRawData>(); 

      public TestContentDirectory()
      {
        library.Add(CreateVideoItem("Video 1", 0));
        library.Add(CreateVideoItem("Video 2", 50));
        library.Add(CreateVideoItem("Video 3", 100));

        library.Add(CreateMovieItem(1, "Movie 1", 0));
        library.Add(CreateMovieItem(2, "Movie 2", 100));
        library.Add(CreateMovieItem(3, "Movie 3", 100));

        library.Add(CreateSeriesItem(1, "Series 1", 1, 3, 66));
        library.Add(CreateSeasonItem(10, "Season 1", 1, "Series 1", 1, 3, 66));
        library.Add(CreateEpisodeItem(100, "Episode 1", 1, "Series 1", 1, 1, 100));
        library.Add(CreateEpisodeItem(101, "Episode 2", 1, "Series 1", 1, 2, 100));
        library.Add(CreateEpisodeItem(102, "Episode 3", 1, "Series 1", 1, 3, 20));

        library.Add(CreateAlbumItem(1, "Album 1", 3, 33));
        library.Add(CreateAlbumTrackItem(10, "Track 1", 1, "Album 1", 1, 3, 100));
        library.Add(CreateAlbumTrackItem(11, "Track 2", 1, "Album 1", 2, 3, 70));
        library.Add(CreateAlbumTrackItem(12, "Track 3", 1, "Album 1", 3, 3, 20));

        library.Add(CreateImageItem("Image 1", 100));
      }

      public static MediaItem CreateBaseItem(string name, int watchPercentage)
      {
        MediaItem item = new MediaItem(Guid.NewGuid());
        MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, name);
        MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_SORT_TITLE, name);
        MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_RECORDINGTIME, DateTime.Now);
        MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_ISVIRTUAL, false);
        MediaItemAspect.SetAttribute(item.Aspects, ImporterAspect.ATTR_DATEADDED, DateTime.Now);
        MediaItemAspect.SetAttribute(item.Aspects, ImporterAspect.ATTR_DIRTY, false);
        MediaItemAspect.SetAttribute(item.Aspects, ImporterAspect.ATTR_LAST_IMPORT_DATE, DateTime.Now);
        if (watchPercentage > 0)
        {
          item.UserData.Add(UserDataKeysKnown.KEY_PLAY_COUNT, UserDataKeysKnown.GetSortablePlayCountString(1));
          item.UserData.Add(UserDataKeysKnown.KEY_PLAY_DATE, UserDataKeysKnown.GetSortablePlayDateString(DateTime.Now));
          item.UserData.Add(UserDataKeysKnown.KEY_PLAY_PERCENTAGE, UserDataKeysKnown.GetSortablePlayPercentageString(watchPercentage));
        }
        return item;
      }

      public static MediaItem CreateVideoItem(string name, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        SingleMediaItemAspect videoAspect = MediaItemAspect.GetOrCreateAspect(item.Aspects, VideoAspect.Metadata);
        videoAspect.SetAttribute(VideoAspect.ATTR_ISDVD, false);

        MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(item.Aspects, ProviderResourceAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/mkv");
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, (long)1000);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath($@"C:\Folder\{name}.mkv").Serialize());

        int streamId = 0;
        MultipleMediaItemAspect videoStreamAspects = MediaItemAspect.CreateAspect(item.Aspects, VideoStreamAspect.Metadata);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, streamId++);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, "HD");
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, 16F/9F);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_FPS, (float)50);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1920);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1080);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_DURATION, (long)60);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOBITRATE, (long)100000);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, "AVC");
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, 1);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, -1);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, 0);
        videoStreamAspects.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME, "Set 1");

        MultipleMediaItemAspect audioAspect = MediaItemAspect.CreateAspect(item.Aspects, VideoAudioStreamAspect.Metadata);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, 0);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, streamId++);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, "AAC");
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, (long)10000);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 2);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, (long)48000);
        audioAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, "EN");

        MultipleMediaItemAspect subtitleAspect = MediaItemAspect.CreateAspect(item.Aspects, SubtitleAspect.Metadata);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, 0);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, 0);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, streamId++);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, "SRT");
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_DEFAULT, false);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_FORCED, false);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_INTERNAL, true);
        subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, "EN");

        return item;
      }

      public static MediaItem CreateMovieItem(int id, string name, int watchPercentage)
      {
        var item = CreateVideoItem(name, watchPercentage);

        MovieInfo movie = new MovieInfo();
        movie.FromMetadata(item.Aspects);

        movie.MovieDbId = id;
        movie.ImdbId = "tt" + id;

        movie.Budget = 0;
        movie.CollectionMovieDbId = 0;
        movie.CollectionName = null;
        movie.Genres = new List<GenreInfo> { new GenreInfo { Id = 1, Name = "Action" } };
        movie.MovieName = new SimpleTitle(name, false);
        movie.OriginalName = name;
        movie.Summary = new SimpleTitle("Movie summery", false);
        movie.Popularity = 0;
        movie.ProductionCompanies = new List<CompanyInfo>();
        movie.Rating = new SimpleRating(5, 1000);
        movie.ReleaseDate = new DateTime(2000, 1, 1);
        movie.Revenue = 0;
        movie.Tagline = "Tagline";
        movie.Runtime = 3600;
        movie.Certification = "PG_13";

        movie.Actors = new List<PersonInfo>();
        movie.Writers = new List<PersonInfo>();
        movie.Directors = new List<PersonInfo>();
        movie.Characters = new List<CharacterInfo>();

        movie.SetMetadata(item.Aspects);

        return item;

      }

      public static MediaItem CreateSeriesItem(int id, string name, int seasons, int episodes, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        SeriesInfo series = new SeriesInfo();
        series.FromMetadata(item.Aspects);

        series.MovieDbId = id;
        series.TvdbId = id;
        series.TvRageId = id;
        series.ImdbId = "tt" + id;

        series.SeriesName = new SimpleTitle(name, false);
        series.OriginalName = name;
        series.FirstAired = new DateTime(2000, 1, 1);
        series.Description = new SimpleTitle("Description", false);
        series.Popularity = 0;
        series.Rating = new SimpleRating(6, 1000);
        series.Genres = new List<GenreInfo> { new GenreInfo { Id = 1, Name = "Action" } };
        series.Networks = new List<CompanyInfo>();
        series.ProductionCompanies = new List<CompanyInfo>();
        series.IsEnded = true;
        series.Certification = "TV_G";
        series.Actors = new List<PersonInfo>();
        series.Characters = new List<CharacterInfo>();

        series.NextEpisodeName = new SimpleTitle("Next episode", false);
        series.NextEpisodeAirDate = DateTime.Today.AddDays(7);
        series.NextEpisodeSeasonNumber = seasons + 1;
        series.NextEpisodeNumber = 1;

        series.TotalSeasons = seasons;
        series.TotalEpisodes = episodes;

        series.SetMetadata(item.Aspects);

        return item;

      }

      public static MediaItem CreateSeasonItem(int id, string name, int seriesId, string seriesName, int seasonNo, int episodes, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        SeasonInfo season = new SeasonInfo();
        season.MovieDbId = id;
        season.ImdbId = "tt" + id;
        season.TvdbId = id;
        season.TvRageId = id;

        season.SeriesMovieDbId = seriesId;
        season.SeriesImdbId = "tt" + seriesId;
        season.SeriesTvdbId = seriesId;
        season.SeriesTvRageId = seriesId;
        season.SeriesName = new SimpleTitle(seriesName, false);

        season.FirstAired = new DateTime(2000, 1, 1);
        season.Description = new SimpleTitle("Description", false);
        season.TotalEpisodes = episodes;
        season.SeasonNumber = seasonNo;

        season.SetMetadata(item.Aspects);

        return item;
      }

      public static MediaItem CreateEpisodeItem(int id, string name, int seriesId, string seriesName, int seasonNo, int episodeNo, int watchPercentage)
      {
        var item = CreateVideoItem(name, watchPercentage);

        EpisodeInfo epsiode = new EpisodeInfo();
        epsiode.MovieDbId = id;
        epsiode.ImdbId = "tt" + id;
        epsiode.TvdbId = id;
        epsiode.TvRageId = id;

        epsiode.SeriesMovieDbId = seriesId;
        epsiode.SeriesImdbId = "tt" + seriesId;
        epsiode.SeriesTvdbId = seriesId;
        epsiode.SeriesTvRageId = seriesId;
        epsiode.SeriesName = new SimpleTitle(seriesName, false);
        epsiode.SeriesFirstAired = new DateTime(2000, 1, 1);

        epsiode.FirstAired = new DateTime(2000, 1, 1).AddDays(7 * (episodeNo - 1));
        epsiode.SeasonNumber = seasonNo;
        epsiode.EpisodeNumbers = new List<int>(new int[] { episodeNo });
        epsiode.Rating = new SimpleRating(4.5, 2000);
        epsiode.EpisodeName = new SimpleTitle("Episode " + episodeNo, false);
        epsiode.Summary = new SimpleTitle("Summary", false);
        epsiode.Genres = new List<GenreInfo> { new GenreInfo { Id = 1, Name = "Action" } };

        epsiode.SetMetadata(item.Aspects);

        return item;
      }

      public static MediaItem CreateAlbumItem(int id, string name, int tracks, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        AlbumInfo album = new AlbumInfo();
        album.FromMetadata(item.Aspects);

        album.AudioDbId = id;
        album.AmazonId = id.ToString();
        album.MusicBrainzGroupId = new Guid(id, 0, 0, new byte[8]).ToString();
        album.MusicBrainzId = new Guid(id, 0, 0, new byte[8]).ToString();

        album.Album = name;
        album.Description = new SimpleTitle("Description", false);
        album.ReleaseDate = new DateTime(2000, 1, 1);
        album.Genres = new List<GenreInfo> { new GenreInfo { Id = 1, Name = "Music" } };
        album.Rating = new SimpleRating(7, 1000);

        album.Artists = new List<PersonInfo>();
        album.MusicLabels = new List<CompanyInfo>();

        album.TotalTracks = tracks;

        album.SetMetadata(item.Aspects);

        return item;
      }

      public static MediaItem CreateAlbumTrackItem(int id, string name, int albumId, string albumName, int trackNo, int tracks, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(item.Aspects, ProviderResourceAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, (long)1000);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath($@"C:\Folder\{name}.mp3").Serialize());
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "audio/mp3");
        
        MediaItemAspect.SetAttribute(item.Aspects, MediaAspect.ATTR_COMMENT, "Comment");

        TrackInfo track = new TrackInfo();
        track.FromMetadata(item.Aspects);

        track.AudioDbId = id;
        track.MusicBrainzId = new Guid(id, 0, 0, new byte[8]).ToString();
        track.AlbumAmazonId = albumId.ToString();
        track.AlbumMusicBrainzGroupId = new Guid(albumId, 0, 0, new byte[8]).ToString();
        track.AlbumMusicBrainzId = new Guid(albumId, 0, 0, new byte[8]).ToString();
        track.Album = albumName;

        track.Compilation = false;
        track.TrackName = name;
        track.TrackNum = trackNo;
        track.DiscNum = 1;
        track.TotalDiscs = 1;
        track.Encoding = "MP3";
        track.Duration = 60;
        track.BitRate = 100000;
        track.Channels = 2;
        track.SampleRate = 48000;
        track.ReleaseDate = new DateTime(2000, 1, 1);
        track.Genres = new List<GenreInfo> { new GenreInfo { Id = 1, Name = "Music" } };
        track.Rating = new SimpleRating(7, 1000);

        track.Artists = new List<PersonInfo>();
        track.AlbumArtists = new List<PersonInfo>();
        track.MusicLabels = new List<CompanyInfo>();
        track.Composers = new List<PersonInfo>();
        track.Conductors = new List<PersonInfo>();

        track.TotalTracks = tracks;

        track.SetMetadata(item.Aspects);

        return item;
      }

      public static MediaItem CreateImageItem(string name, int watchPercentage)
      {
        var item = CreateBaseItem(name, watchPercentage);

        SingleMediaItemAspect imageAspect = MediaItemAspect.GetOrCreateAspect(item.Aspects, ImageAspect.Metadata);
        imageAspect.SetAttribute(ImageAspect.ATTR_HEIGHT, 1920);
        imageAspect.SetAttribute(ImageAspect.ATTR_ORIENTATION, 0);
        imageAspect.SetAttribute(ImageAspect.ATTR_WIDTH, 1080);

        return item;
      }


      public Task AddMediaItemAspectStorageAsync(MediaItemAspectMetadata miam)
      {
        throw new NotImplementedException();
      }

      public Task<Guid> AddOrUpdateMediaItemAsync(Guid parentDirectoryId, string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects)
      {
        MediaItem item = new MediaItem(Guid.NewGuid(), MediaItemAspect.GetAspects(mediaItemAspects));
        library.Add(item);
        return Task.FromResult(item.MediaItemId);
      }

      public Task<Guid> AddOrUpdateMediaItemAsync(Guid parentDirectoryId, string systemId, ResourcePath path, Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
      {
        MediaItem item = library.FirstOrDefault(i => i.MediaItemId == mediaItemId);
        if (item == null)
        {
          item = new MediaItem(Guid.NewGuid(), MediaItemAspect.GetAspects(mediaItemAspects));
          library.Add(item);
        }
        else
        {
          item.Aspects.Clear();
          foreach (var aspect in MediaItemAspect.GetAspects(mediaItemAspects))
            item.Aspects.Add(aspect.Key, aspect.Value);
        }
        return Task.FromResult(item.MediaItemId);
      }

      public Task<IList<MediaItem>> BrowseAsync(Guid parentDirectoryId, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null)
      {
        var items = library.Where(i => necessaryMIATypes.All(na => i.Aspects.ContainsKey(na)));
        if (offset != null)
          items = items.Skip(Convert.ToInt32(offset.Value));
        if (limit != null)
          items = items.Take(Convert.ToInt32(limit.Value));
        return Task.FromResult<IList<MediaItem>>(items.ToList());
      }

      public Task ClientCompletedShareImportAsync(Guid shareId)
      {
        throw new NotImplementedException();
      }

      public Task ClientStartedShareImportAsync(Guid shareId)
      {
        throw new NotImplementedException();
      }

      public async Task<int> CountMediaItemsAsync(IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
      {
        var items = await SearchAsync(new MediaItemQuery(necessaryMIATypes, filter), onlyOnline, null, includeVirtual);
        return items.Count;
      }

      public Task DeleteMediaItemOrPathAsync(string systemId, ResourcePath path, bool inclusive)
      {
        return Task.CompletedTask;
      }

      public Task<bool> DeletePlaylistAsync(Guid playlistId)
      {
        var playlist = playlists.FirstOrDefault(p => p.PlaylistId == playlistId);
        if (playlist != null)
        {
          playlists.Remove(playlist);
          return Task.FromResult(true);
        }
        return Task.FromResult(false);
      }

      public Task<PlaylistRawData> ExportPlaylistAsync(Guid playlistId)
      {
        var playlist = playlists.FirstOrDefault(p => p.PlaylistId == playlistId);
        return Task.FromResult(playlist);
      }

      public Task<IDictionary<Guid, DateTime>> GetAllManagedMediaItemAspectCreationDatesAsync()
      {
        throw new NotImplementedException();
      }

      public Task<ICollection<Guid>> GetAllManagedMediaItemAspectTypesAsync()
      {
        throw new NotImplementedException();
      }

      public Task<ICollection<Guid>> GetCurrentlyImportingSharesAsync()
      {
        throw new NotImplementedException();
      }

      public Task<Tuple<HomogenousMap, HomogenousMap>> GetKeyValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType, IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
      {
        throw new NotImplementedException();
      }

      public Task<MediaItemAspectMetadata> GetMediaItemAspectMetadataAsync(Guid miamId)
      {
        throw new NotImplementedException();
      }

      public Task<ICollection<PlaylistInformationData>> GetPlaylistsAsync()
      {
        return Task.FromResult<ICollection<PlaylistInformationData>>(playlists.Select(p => new PlaylistInformationData(p.PlaylistId, p.Name, p.PlaylistType, p.NumItems)).ToList());
      }

      public Task<Share> GetShareAsync(Guid shareId)
      {
        throw new NotImplementedException();
      }

      public Task<ICollection<Share>> GetSharesAsync(string systemId, SharesFilter sharesFilter)
      {
        return Task.FromResult<ICollection<Share>>(new List<Share>());
      }

      public Task<HomogenousMap> GetValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
      {
        throw new NotImplementedException();
      }

      public Task<IList<MLQueryResultGroup>> GroupValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, GroupingFunction groupingFunction, bool includeVirtual)
      {
        throw new NotImplementedException();
      }

      public Task<IList<MediaItem>> LoadCustomPlaylistAsync(IList<Guid> mediaItemIds, ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes, uint? offset = null, uint? limit = null)
      {
        return SearchAsync(new MediaItemQuery(necessaryMIATypes, optionalMIATypes, new MediaItemIdFilter(mediaItemIds)), true, null, true);
      }

      public Task<MediaItem> LoadItemAsync(string systemId, ResourcePath path, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile)
      {
        throw new NotImplementedException();
      }

      public Task<MediaItem> LoadItemAsync(string systemId, Guid mediItemId, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile)
      {
        MediaItem item = library.FirstOrDefault(i => i.MediaItemId == mediItemId);
        return Task.FromResult(item);
      }

      public Task NotifyPlaybackAsync(Guid mediaItemId, bool watched)
      {
        return Task.CompletedTask;
      }

      public Task NotifyUserPlaybackAsync(Guid userId, Guid mediaItemId, int percentage, bool updatePlayDate)
      {
        return Task.CompletedTask;
      }

      public Task<IList<MediaItem>> ReconcileMediaItemRelationshipsAsync(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, IEnumerable<RelationshipItem> relationshipItems)
      {
        throw new NotImplementedException();
      }

      public Task RefreshMediaItemMetadataAsync(Guid mediaItemId, bool clearMetadata)
      {
        throw new NotImplementedException();
      }

      public Task RegisterShareAsync(Share share)
      {
        throw new NotImplementedException();
      }

      public Task ReimportMediaItemMetadataAsync(Guid mediaItemId, IEnumerable<MediaItemAspect> matchedAspects)
      {
        throw new NotImplementedException();
      }

      public Task ReImportShareAsync(Guid guid)
      {
        throw new NotImplementedException();
      }

      public Task RemoveMediaItemAspectStorageAsync(Guid aspectId)
      {
        throw new NotImplementedException();
      }

      public Task RemoveShareAsync(Guid shareId)
      {
        throw new NotImplementedException();
      }

      public Task SavePlaylistAsync(PlaylistRawData playlistData)
      {
        var playList = playlists.FirstOrDefault(p => p.PlaylistId == playlistData.PlaylistId);
        if (playList != null)
          playlists.Remove(playList);

        playlists.Add(playlistData);
        return Task.CompletedTask;
      }

      public Task<IList<MediaItem>> SearchAsync(MediaItemQuery query, bool onlyOnline, Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null)
      {
        ICollection<Guid> requidGuids = null;
        if (query.Filter is MediaItemIdFilter idFilter)
          requidGuids = idFilter.MediaItemIds;

        var items = library.Where(i => query.NecessaryRequestedMIATypeIDs.All(na => i.Aspects.ContainsKey(na)) && (requidGuids == null || requidGuids.Contains(i.MediaItemId)));
        if (offset != null)
          items = items.Skip(Convert.ToInt32(offset.Value));
        if (limit != null)
          items = items.Take(Convert.ToInt32(limit.Value));
        return Task.FromResult<IList<MediaItem>>(items.ToList());
      }

      public Task SetupDefaultServerSharesAsync()
      {
        throw new NotImplementedException();
      }

      public Task<IList<MediaItem>> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive, Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null)
      {
        throw new NotImplementedException();
      }

      public Task<int> UpdateShareAsync(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher, IEnumerable<string> mediaCategories, RelocationMode relocationMode)
      {
        throw new NotImplementedException();
      }

      public Task<bool> DownloadMetadataAsync(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
      {
        throw new NotImplementedException();
      }
    }

    private class TestPlayer : IPlayer, IMediaPlaybackControl
    {
      private MediaItem _playingItem = null;

      public TestPlayer()
      {
        State = PlayerState.Ended;
      }

      public string Name => "Test Player";
      public PlayerState State { get; set; }
      public string MediaItemTitle { get; set; }
      public TimeSpan CurrentTime { get; set; }
      public TimeSpan Duration { get; private set; }
      public double PlaybackRate { get; private set; }
      public bool IsPlayingAtNormalRate => PlaybackRate == 1;
      public bool IsSeeking { get; set; }
      public bool IsPaused { get; set; }
      public bool CanSeekForwards => true;
      public bool CanSeekBackwards => true;

      public void Pause()
      {
        IsPaused = true;
        State = PlayerState.Active;
      }

      public void Restart()
      {
        IsPaused = false;
        PlaybackRate = 1;
        State = PlayerState.Active;
      }

      public void Resume()
      {
        IsPaused = false;
        State = PlayerState.Active;
      }

      public bool SetPlaybackRate(double value)
      {
        PlaybackRate = value;
        return true;
      }

      public void Stop()
      {
        State = PlayerState.Stopped;
      }

      public void PlayItem(MediaItem item)
      {
        if (MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out string title))
          MediaItemTitle = title;
        Duration = TimeSpan.FromMinutes(60);
        CurrentTime = TimeSpan.FromMinutes(10);
        Restart();
      }
    }

    private class TestPlaylist : IPlaylist
    {
      private List<MediaItem> _playlistItems = new List<MediaItem>();

      public MediaItem this[int relativeIndex] => _playlistItems[relativeIndex];

      public object SyncObj => new object();

      public PlayMode PlayMode { get; set; }
      public RepeatMode RepeatMode { get; set; }

      public IList<MediaItem> ItemList => _playlistItems;

      public int PlayableItemsCount => _playlistItems.Count;

      public int ItemListIndex { get; set; }

      public MediaItem Current => _playlistItems[ItemListIndex];

      public bool AllPlayed => false;

      public bool InBatchUpdateMode { get; set; }

      public bool HasPrevious => ItemListIndex > 0;

      public bool HasNext => ItemListIndex < _playlistItems.Count - 1;

      public void Add(MediaItem mediaItem)
      {
        _playlistItems.Add(mediaItem);
      }

      public void AddAll(IEnumerable<MediaItem> mediaItems)
      {
        _playlistItems.AddRange(mediaItems);
      }

      public void Clear()
      {
        _playlistItems.Clear();
      }

      public void EndBatchUpdate()
      {
        InBatchUpdateMode = false;
      }

      public void ExportPlaylistContents(PlaylistContents data)
      {
      }

      public void ExportPlaylistRawData(PlaylistRawData data)
      {
      }

      public bool Insert(int index, MediaItem mediaItem)
      {
        _playlistItems.Insert(index, mediaItem);
        return true;
      }

      public MediaItem MoveAndGetNext()
      {
        ItemListIndex++;
        return _playlistItems[ItemListIndex];
      }

      public MediaItem MoveAndGetPrevious()
      {
        ItemListIndex--;
        return _playlistItems[ItemListIndex];
      }

      public void Remove(MediaItem mediaItem)
      {
        _playlistItems.Remove(mediaItem);
      }

      public void RemoveAt(int index)
      {
        _playlistItems.RemoveAt(index);
      }

      public void RemoveRange(int fromIndex, int toIndex)
      {
        for(int index = fromIndex; index <= toIndex; index++)
          _playlistItems.RemoveAt(fromIndex);
      }

      public void ResetStatus()
      {
      }

      public void StartBatchUpdate()
      {
        InBatchUpdateMode = true;
      }

      public void Swap(int index1, int index2)
      {
        MediaItem tmp = _playlistItems[index1];
        _playlistItems[index1] = _playlistItems[index2];
        _playlistItems[index2] = tmp;
      }
    }

    #endregion

    #region Communication

    private int SendData(BaseSendMessage data, bool waitForLogin = true)
    {
      if (waitForLogin)
      {
        _loginReceived.WaitOne(5000);
        data.AutologinKey = _loginResponse?.AutologinKey;
      }
      var sendData = JsonConvert.SerializeObject(data) + "\r\n";
      var sendBuffer = Encoding.UTF8.GetBytes(sendData);
      return _socket.Send(sendBuffer);
    }

    private T ReadData<T>(BaseSendMessage data, string responseType, bool removeResponse = true, int timeoutInSeconds = 5)
    {
      _welcomeReceived.WaitOne(5000);
      DateTime start = DateTime.Now;
      _messageReceived.Reset();
      SendData(data);
      if (!string.IsNullOrEmpty(responseType))
      {
        while (!_receivedResponses.ContainsKey(responseType))
        {
          _messageReceived.WaitOne(10);
          if (_receivedResponses.TryGetValue(responseType, out var response))
          {
            if (removeResponse)
              _receivedResponses.TryRemove(responseType, out var _);
            return (T)Convert.ChangeType(response, typeof(T));
          }

          if ((DateTime.Now - start).TotalSeconds > timeoutInSeconds)
            break;
        }
      }
      return default(T);
    }

    private void OnData(IAsyncResult a)
    {
      int bytesReceived = 0;
      if (_socket.Connected)
        bytesReceived = _socket.EndReceive(a);

      if (bytesReceived > 0)
      {
        string receivedData = Encoding.UTF8.GetString(_receiveBuffer, 0, bytesReceived);
        StringReader reader = new StringReader(receivedData);
        string data = reader.ReadLine();
        while (!string.IsNullOrEmpty(data))
        {
          JObject message = JObject.Parse(data);
          string type = (string)message["Type"];
          if (type == "welcome")
          {
            _welcomeResponse = JsonConvert.DeserializeObject<MessageWelcomeResponse>(data);
            _welcomeReceived.Set();
          }
          else if (type == "authenticationresponse")
          {
            _loginResponse = JsonConvert.DeserializeObject<MessageAuthenticationResponse>(data);
            _loginReceived.Set();
          }
          else if (type == "status")
          {
            _statusResponse = JsonConvert.DeserializeObject<MessageStatusResponse>(data);
            _statusReceived.Set();
          }
          else if (type == "volume")
          {
          }
          else if (type == "playlistdetails")
          {
            _receivedResponses[type] = JsonConvert.DeserializeObject<MessagePlaylistDetails>(data);
            _messageReceived.Set();
          }
          else
          {
            _receivedResponses[type] = JsonConvert.DeserializeObject<MessageListResponse>(data);
            _messageReceived.Set();
          }
          data = reader.ReadLine();
        }
        _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, new AsyncCallback(OnData), null);
      }
    }

    #endregion

    #region Test Setup

    private void SetMockServices()
    {
      var mockSystemResolver = new Mock<ISystemResolver>();
      mockSystemResolver.Setup(x => x.LocalSystemId).Returns("");
      ServiceRegistration.Set<ISystemResolver>(mockSystemResolver.Object);

      //ServiceRegistration.Set<ISettingsManager>(new NoSettingsManager());

      var contentDir = new TestContentDirectory();
      ServiceRegistration.Set<IContentDirectory>(contentDir);

      var mockServerConnectionManager = new Mock<IServerConnectionManager>();
      mockServerConnectionManager.Setup(x => x.ContentDirectory).Returns(contentDir);
      ServiceRegistration.Set<IServerConnectionManager>(mockServerConnectionManager.Object);

      var user = new UserProfile(Guid.Empty, "Test User", UserProfileType.UserProfile, "test");
      user.RestrictAges = false;
      user.RestrictShares = false;
      var mockServerUserProfileManager = new Mock<IUserProfileDataManagement>();
      mockServerUserProfileManager.Setup(x => x.GetProfileAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new AsyncResult<UserProfile>(true, user)));
      var mockServerUserManager = new Mock<IUserManagement>();
      mockServerUserManager.Setup(x => x.CurrentUser).Returns(user);
      mockServerUserManager.Setup(x => x.UserProfileDataManagement).Returns(mockServerUserProfileManager.Object);
      ServiceRegistration.Set<IUserProfileDataManagement>(mockServerUserProfileManager.Object);
      ServiceRegistration.Set<IUserManagement>(mockServerUserManager.Object);

      var settings = new WifiRemoteSettings
      {
        AuthenticationMethod = (int)AuthMethod.Passcode,
        PassCode = WIFIREMOTE_PASSCODE,
        AutoLoginTimeout = 300,
        EnableBonjour = false,
        Port = WIFIREMOTE_PORT,
        ServiceName = "MP2 Wifi Remote"
      };
      var serverSettings = new ServerSettings
      {
        HttpServerPort = 80,
        UseIPv4 = true,
        UseIPv6 = false,
      };
      var mockSettingManager = new Mock<ISettingsManager>();
      mockSettingManager.Setup(x => x.Load<WifiRemoteSettings>()).Returns(settings);
      mockSettingManager.Setup(x => x.Load<ServerSettings>()).Returns(serverSettings);
      //mockSettingManager.Setup(x => x.Load(It.IsAny<Type>())).Returns(settings);
      ServiceRegistration.Set<ISettingsManager>(mockSettingManager.Object);

      ServiceRegistration.Set<ILogger>(new ConsoleLogger(LogLevel.Debug, true));

      var playingItem = TestContentDirectory.CreateMovieItem(1, "Playing Movie", 10);
      var player = new TestPlayer();
      player.PlayItem(playingItem);
      var mockPlayerContext = new Mock<IPlayerContext>();
      mockPlayerContext.Setup(x => x.CurrentPlayer).Returns(player);
      mockPlayerContext.Setup(x => x.CurrentMediaItem).Returns(playingItem);
      mockPlayerContext.Setup(x => x.Playlist).Returns(new TestPlaylist());
      var mockPlayerManager = new Mock<IPlayerManager>();
      mockPlayerManager.Setup(x => x.NumActiveSlots).Returns(1);
      var mockPalyerContextManager = new Mock<IPlayerContextManager>();
      mockPalyerContextManager.Setup(x => x.CurrentPlayerContext).Returns(mockPlayerContext.Object);
      mockPalyerContextManager.Setup(x => x.PrimaryPlayerContext).Returns(mockPlayerContext.Object);
      mockPalyerContextManager.Setup(x => x.SecondaryPlayerContext).Returns(mockPlayerContext.Object);
      mockPalyerContextManager.Setup(x => x.IsFullscreenContentWorkflowStateActive).Returns(true);
      ServiceRegistration.Set<IPlayerContext>(mockPlayerContext.Object);
      ServiceRegistration.Set<IPlayerManager>(mockPlayerManager.Object);
      ServiceRegistration.Set<IPlayerContextManager>(mockPalyerContextManager.Object);

      var mockWorkflowState = new WorkflowState(Guid.NewGuid(), "", "Module", true, "", false, true, null, WorkflowType.Workflow, "");
      var mockNavContext = new NavigationContext(mockWorkflowState, "Module", null, null);
      var mockWorkflowManager = new Mock<IWorkflowManager>();
      mockWorkflowManager.Setup(x => x.CurrentNavigationContext).Returns(mockNavContext);
      ServiceRegistration.Set<IWorkflowManager>(mockWorkflowManager.Object);

      var mockMessageBroker = new Mock<IMessageBroker>();
      ServiceRegistration.Set<IMessageBroker>(mockMessageBroker.Object);

      var mockLocalization = new Mock<ILocalization>();
      ServiceRegistration.Set<ILocalization>(mockLocalization.Object);

      var mockResourceServer = new Mock<IResourceServer>();
      mockResourceServer.Setup(x => x.GetServiceUrl(It.IsAny<IPAddress>())).Returns("127.0.0.1:80");
      ServiceRegistration.Set<IResourceServer>(mockResourceServer.Object);

      List<IChannelGroup> groups = new List<IChannelGroup>
      {
        new ChannelGroup() { ChannelGroupId = 1, MediaType = MediaType.TV, Name = "All TV Channels", ServerIndex = 0, SortOrder = 0 },
      };
      List<IChannel> channels = new List<IChannel>
      {
        new Channel() { ChannelId = 1, ChannelNumber = 1, MediaType = MediaType.TV, Name = "Channel 1", VisibleInGuide = true, ServerIndex = 0 },
      };
      List<IProgram> programs = new List<IProgram>
      {
        new Program() { ChannelId = 1, Classification = "TV_G", Description = "Test episode 1", ProgramId = 1, EpgGenreId = 1, Genre = "Action", SeasonNumber = "1", EpisodeNumber = "1", EpisodePart = "1", EpisodeTitle = "Episode 1", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(1), Title = "Series Episode 1", ServerIndex = 0 },
        new Program() { ChannelId = 1, Classification = "TV_G", Description = "Test movie 1", ProgramId = 2, EpgGenreId = 1, Genre = "Action", StartTime = DateTime.Today.AddHours(1), EndTime = DateTime.Today.AddHours(3), Title = "Movie 1", ServerIndex = 1 },
        new Program() { ChannelId = 1, Classification = "TV_G", Description = "Test episode 1", ProgramId = 3, EpgGenreId = 1, Genre = "Action", SeasonNumber = "1", EpisodeNumber = "1", EpisodePart = "1", EpisodeTitle = "Episode 1", StartTime = DateTime.Today.AddHours(3), EndTime = DateTime.Today.AddHours(4), Title = "Series Episode 1", ServerIndex = 2 },
      };
      List<ISchedule> schedules = new List<ISchedule>
      {
        new Schedule() { ScheduleId = 1, Name = "Movie 1", ChannelId = 1, StartTime = DateTime.Today.AddHours(1), EndTime = DateTime.Today.AddHours(3), RecordingType = ScheduleRecordingType.Once, Priority = PriorityType.High },
      };
      var mockChannelAndGroupInfo = new Mock<IChannelAndGroupInfoAsync>();
      mockChannelAndGroupInfo.Setup(x => x.GetChannelGroupsAsync()).Returns(Task.FromResult(new AsyncResult<IList<IChannelGroup>>(true, groups)));
      mockChannelAndGroupInfo.Setup(x => x.GetChannelsAsync(It.IsAny<IChannelGroup>())).Returns(Task.FromResult(new AsyncResult<IList<IChannel>>(true, channels)));
      mockChannelAndGroupInfo.Setup(x => x.GetChannelAsync(It.IsAny<int>())).Returns(Task.FromResult(new AsyncResult<IChannel>(true, channels.First())));
      var mockProgramInfo = new Mock<IProgramInfoAsync>();
      mockProgramInfo.Setup(x => x.GetProgramsGroupAsync(It.IsAny<IChannelGroup>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(new AsyncResult<IList<IProgram>>(true, programs)));
      mockProgramInfo.Setup(x => x.GetProgramsAsync(It.IsAny<IChannel>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(new AsyncResult<IList<IProgram>>(true, programs)));
      var mockScheduleControl = new Mock<IScheduleControlAsync>();
      mockScheduleControl.Setup(x => x.GetSchedulesAsync()).Returns(Task.FromResult(new AsyncResult<IList<ISchedule>>(true, schedules)));
      var mockTvHandler = new Mock<ITvHandler>();
      mockTvHandler.Setup(x => x.ChannelAndGroupInfo).Returns(mockChannelAndGroupInfo.Object);
      mockTvHandler.Setup(x => x.ProgramInfo).Returns(mockProgramInfo.Object);
      mockTvHandler.Setup(x => x.ScheduleControl).Returns(mockScheduleControl.Object);
      ServiceRegistration.Set<ITvHandler>(mockTvHandler.Object);
    }

    #endregion
  }
}
