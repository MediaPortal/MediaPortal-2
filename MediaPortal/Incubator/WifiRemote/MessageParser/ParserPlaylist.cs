#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.Playlist;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlaylist : BaseParser
  {
    private static string LastLoadedPlayList = null;

    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = GetMessageValue<string>(message, "PlaylistAction");
      string playlistType = GetMessageValue<string>(message, "PlaylistType", "music");
      bool autoPlay = GetMessageValue<bool>(message, "AutoPlay");
      int index = GetMessageValue<int>(message, "Index");
      var playList = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Playlist;
      var client = sender.GetRemoteClient();

      if (action.Equals("new", StringComparison.InvariantCultureIgnoreCase) || action.Equals("append", StringComparison.InvariantCultureIgnoreCase))
      {
        //new playlist or append to playlist
        int insertIndex = GetMessageValue<int>(message, "InsertIndex");
        bool shuffle = GetMessageValue<bool>(message, "Shuffle");

        // Add items from JSON or SQL
        JArray array = GetMessageValue<JArray>(message, "PlaylistItems");
        
        if (array != null)
        {
          if (action.Equals("new", StringComparison.InvariantCultureIgnoreCase))
            playList.Clear();

          int idx = insertIndex;
          if (array != null)
          {
            playList.StartBatchUpdate();

            // Add items from JSON
            foreach (JObject o in array)
            {
              string fileName = GetMessageValue<string>(o, "FileName");
              string id = GetMessageValue<string>(o, "FileId");

              var mediaItemGuid = await GetIdFromNameAsync(client, fileName, id, Helper.GetMediaItemByFileNameAsync);
              if (mediaItemGuid == null)
              {
                ServiceRegistration.Get<ILogger>().Error("WifiRemote Playlist: Couldn't convert FileId '{0} to Guid", id);
                return false;
              }

              MediaItem item = await Helper.GetMediaItemByIdAsync(client.UserId, mediaItemGuid.Value);
              if (item == null)
              {
                ServiceRegistration.Get<ILogger>().Warn("WifiRemote Playlist: Not media item found");
                continue;
              }

              playList.Insert(idx, item);

              idx++;
            }
            playList.EndBatchUpdate();

            playList.PlayMode = PlayMode.Continuous;
            if (shuffle)
              playList.PlayMode = PlayMode.Shuffle;
          }

          if (autoPlay)
          {
            playList.ItemListIndex = 0;
            ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Play();
          }
        }
      }
      else if (action.Equals("load", StringComparison.InvariantCultureIgnoreCase))
      {
        //load a playlist
        string playlistName = GetMessageValue<string>(message, "PlayListName");
        string playlistPath = GetMessageValue<string>(message, "PlaylistPath");
        bool shuffle = GetMessageValue<bool>(message, "Shuffle");
        if (string.IsNullOrEmpty(playlistPath))
        {
          List<PlaylistInformationData> playLists = ServerPlaylists.GetPlaylists().ToList();
          playlistPath = playLists.FirstOrDefault(p => p.Name == playlistName)?.PlaylistId.ToString();
        }

        if (Guid.TryParse(playlistPath, out Guid playlistId))
        {
          var data = await Helper.LoadPlayListAsync(playlistId);
          LastLoadedPlayList = data.Name;
          playList.StartBatchUpdate();
          playList.Clear();
          foreach(var item in data.Items)
            playList.Add(item);
          playList.EndBatchUpdate();

          if (autoPlay)
          {
            playList.ItemListIndex = 0;
            ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Play();
          }
        }
      }
      else if (action.Equals("get", StringComparison.InvariantCultureIgnoreCase))
      {
        //get all playlist items of the currently active playlist
        IList<MediaItem> items = playList.ItemList;
        MessagePlaylistDetails returnPlaylist = new MessagePlaylistDetails
        {
          PlaylistName = LastLoadedPlayList ?? "Play list",
          PlaylistRepeat = playList.RepeatMode != RepeatMode.None,
          PlaylistItems = new List<PlaylistEntry>()
        };
        foreach (var mediaItem in playList.ItemList)
        {
          var ple = new PlaylistEntry
          {
            FileId = mediaItem.MediaItemId.ToString(),
          };

          if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          {
            if (returnPlaylist.PlaylistType != "video")
            {
              if (returnPlaylist.PlaylistType == null)
                returnPlaylist.PlaylistType = "video";
              else
                continue;
            }

            IList<MultipleMediaItemAspect> videoStreamAspects;
            MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoStreamAspect.Metadata, out videoStreamAspects);
            var mediaAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, MediaAspect.Metadata);
            var movieAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, MovieAspect.Metadata);
            var episodeAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, EpisodeAspect.Metadata);

            TimeSpan duration = TimeSpan.FromSeconds(0);
            int? setNo = videoStreamAspects?.FirstOrDefault()?.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET);
            if (setNo.HasValue)
            {
              foreach (var stream in videoStreamAspects.Where(s => s.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == setNo.Value))
              {
                long? durSecs = stream.GetAttributeValue<long?>(VideoStreamAspect.ATTR_DURATION);
                if (durSecs.HasValue)
                  duration.Add(TimeSpan.FromSeconds(durSecs.Value));
              }
            }

            ple.MpExtMediaType = (int)MpExtendedMediaTypes.Movie;
            ple.MpExtProviderId = (int)MpExtendedProviders.MPVideo;
            ple.MediaType = returnPlaylist.PlaylistType;
            ple.Name = movieAspect?.GetAttributeValue<string>(MovieAspect.ATTR_MOVIE_NAME) ?? episodeAspect?.GetAttributeValue<string>(EpisodeAspect.ATTR_EPISODE_NAME) ?? mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
            ple.Name2 = episodeAspect?.GetAttributeValue<string>(EpisodeAspect.ATTR_SERIES_NAME);
            ple.Duration = Convert.ToInt32(duration.TotalSeconds);
            ple.Played = Convert.ToInt32(mediaItem.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") == 100;
            returnPlaylist.PlaylistItems.Add(ple);
          }
          else if (mediaItem.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          {
            if (returnPlaylist.PlaylistType != "music")
            {
              if (returnPlaylist.PlaylistType == null)
                returnPlaylist.PlaylistType = "music";
              else
                continue;
            }

            var mediaAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, MediaAspect.Metadata);
            var audioAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, AudioAspect.Metadata);

            ple.MpExtMediaType = (int)MpExtendedMediaTypes.MusicTrack;
            ple.MpExtProviderId = (int)MpExtendedProviders.MPMusic;
            ple.MediaType = returnPlaylist.PlaylistType;
            ple.Name = audioAspect.GetAttributeValue<string>(AudioAspect.ATTR_TRACKNAME);
            ple.Name2 = audioAspect.GetAttributeValue<string>(AudioAspect.ATTR_ALBUM);
            ple.AlbumArtist = string.Join(", ", audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS));
            ple.Duration = Convert.ToInt32(audioAspect.GetAttributeValue<long>(AudioAspect.ATTR_DURATION));
            ple.Played = Convert.ToInt32(mediaItem.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") == 100;
          }
        }
        SendMessageToClient.Send(returnPlaylist, sender, true);
      }
      else if (action.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
      {
        
        //remove an item from the playlist
        playList.RemoveAt(index);
      }
      else if (action.Equals("move", StringComparison.InvariantCultureIgnoreCase))
      {
        //move a playlist item to a new index
        int oldIndex = GetMessageValue<int>(message, "OldIndex");
        int newIndex = GetMessageValue<int>(message, "NewIndex");
        var mediaItem = playList.ItemList[oldIndex];
        playList.RemoveAt(oldIndex);
        playList.Insert(newIndex, mediaItem);
      }
      else if (action.Equals("play", StringComparison.InvariantCultureIgnoreCase))
      {
        //start playback of a playlist item
        playList.ItemListIndex = index;
        ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.Play();
      }
      else if (action.Equals("clear", StringComparison.InvariantCultureIgnoreCase))
      {
        //clear the playlist
        playList.Clear();
      }
      else if (action.Equals("list", StringComparison.InvariantCultureIgnoreCase))
      {
        //get a list of all available playlists
        List<PlaylistInformationData> playLists = ServerPlaylists.GetPlaylists().ToList();
        MessagePlaylists returnList = new MessagePlaylists { PlayLists = playLists.Select(x => x.Name).ToList() };
        SendMessageToAllClients.Send(returnList, ref SocketServer.Instance.connectedSockets);
      }
      else if (action.Equals("save", StringComparison.InvariantCultureIgnoreCase))
      {
        //save the current playlist to file
        string name = GetMessageValue<string>(message, "Name");
        if (name != null)
        {
          await Helper.SavePlayListAsync(Guid.NewGuid(), name, playlistType, playList.ItemList.Select(i => i.MediaItemId));
        }
        else
        {
          Logger.Warn("WifiRemote Playlist: Must specify a name to save a playlist");
        }
      }
      else if (action.Equals("shuffle", StringComparison.InvariantCultureIgnoreCase))
      {
        var playMode = playList.PlayMode == PlayMode.Shuffle ? PlayMode.Continuous : PlayMode.Shuffle;
        playList.PlayMode = playMode;
      }
      else if (action.Equals("repeat", StringComparison.InvariantCultureIgnoreCase))
      {
        Logger.Debug("Playlist action repeat");
        bool repeat = GetMessageValue<bool>(message, "Repeat");
        RepeatMode repeatMode;
        if (repeat)
          repeatMode = RepeatMode.All;
        else
          repeatMode = RepeatMode.None;
        playList.RepeatMode = repeatMode;
      }

      return true;
    }
  }
}
