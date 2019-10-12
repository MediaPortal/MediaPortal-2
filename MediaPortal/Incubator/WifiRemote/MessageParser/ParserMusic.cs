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
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.UiComponents.Media.Models;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserMusic : BaseParser
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = GetMessageValue<string>(message, "Action");
      var client = sender.GetRemoteClient();

      if (!string.IsNullOrEmpty(action))
      {
        string search = GetMessageValue<string>(message, "Search");
        int count = GetMessageValue<int>(message, "Count", 10);
        int offset = GetMessageValue<int>(message, "Offset");
        string albumName = GetMessageValue<string>(message, "AlbumName");
        string id = GetMessageValue<string>(message, "AlbumId");
        int discNum = GetMessageValue<int>(message, "DiscNumber");
        int trackNum = GetMessageValue<int>(message, "TrackNumber");
        int startPos = GetMessageValue<int>(message, "StartPosition");
        bool onlyUnwatched = GetMessageValue<bool>(message, "OnlyUnplayedTracks");

        // Search for album
        if (action.Equals("albumsearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<MusicAlbumInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetAlbumsByAlbumSearchAsync);
          SendMessageToClient.Send(new MessageAlbums { Albums = list }, sender, true);
        }
        // Show album list
        else if (action.Equals("albumlist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<MusicAlbumInfo>(client, null, Convert.ToUInt32(count), Convert.ToUInt32(offset), Helper.GetAlbumsByAlbumSearchAsync);
          SendMessageToClient.Send(new MessageAlbums { Albums = list }, sender, true);
        }
        // Search for track
        if (action.Equals("tracksearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<MusicInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetTracksByTrackSearchAsync);
          SendMessageToClient.Send(new MessageMusic { Music  = list }, sender, true);
        }
        // Show track list for album
        else if (action.Equals("tracklist", StringComparison.InvariantCultureIgnoreCase))
        {
          var mediaItemGuid = await GetIdFromNameAsync(client, albumName, id, Helper.GetAlbumByAlbumNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: List Tracks: Couldn't convert AlbumId '{0} to Guid", id);
            return false;
          }
          var list = await Helper.GetTracksByAlbumIdAsync(client.UserId, mediaItemGuid.Value);
          SendMessageToClient.Send(new MessageMusic { Music = list.Select(i => new MusicInfo(i)).ToList() }, sender, true);
        }
        // Show track details for this track
        else if (action.Equals("tracktails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a track
        else if (action.Equals("playtrack", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Track: AlbumName: {0}, AlbumId: {1}, DiscNumber: {2}, TrackNumber: {3}, StartPos: {4}", albumName, id, discNum, trackNum, startPos);

          var mediaItemGuid = await GetIdFromNameAsync(client, albumName, id, Helper.GetAlbumByAlbumNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't convert AlbumId '{0} to Guid", id);
            return false;
          }

          var episode = await Helper.GetTrackByAlbumTrackAsync(client.UserId, mediaItemGuid.Value, discNum, trackNum);
          if (!(episode?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't find track");
            return false;
          }

          await Helper.PlayMediaItemAsync(episode.First().MediaItemId, startPos);
        }
        else if (action.Equals("playunplayedtrack", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Track: AlbumName: {0}, AlbumId: {1}", albumName, id);

          var mediaItemGuid = await GetIdFromNameAsync(client, albumName, id, Helper.GetAlbumByAlbumNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't convert AlbumId '{0} to Guid", id);
            return false;
          }

          var tracks = await Helper.GetTracksByAlbumIdAsync(client.UserId, mediaItemGuid.Value);
          if (!(tracks?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't find track");
            return false;
          }

          var track = tracks.FirstOrDefault(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") < 100);
          if (track == null)
            track = tracks.LastOrDefault(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") == 100);
          if (track == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't find tracks");
            return false;
          }

          await Helper.PlayMediaItemAsync(track.MediaItemId, 0);
        }
        else if (action.Equals("playrandomtrack", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Track: AlbumName: {0}, AlbumId: {1}", albumName, id);

          var mediaItemGuid = await GetIdFromNameAsync(client, albumName, id, Helper.GetAlbumByAlbumNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't convert AlbumId '{0} to Guid", id);
            return false;
          }

          var tracks = await Helper.GetTracksByAlbumIdAsync(client.UserId, mediaItemGuid.Value);
          if (!(tracks?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Track: Couldn't find tracks");
            return false;
          }

          var trackList = tracks?.ToList();
          var episodeIndex = new Random().Next(0, trackList.Count - 1);
          await Helper.PlayMediaItemAsync(trackList[episodeIndex].MediaItemId, 0);
        }
        else if (action.Equals("playalbum", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Album: AlbumName: {0}, AlbumId: {1}, OnlyUnplayedTracks: {2}", albumName, id, onlyUnwatched);

          var mediaItemGuid = await GetIdFromNameAsync(client, albumName, id, Helper.GetAlbumByAlbumNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Album: Couldn't convert AlbumId '{0} to Guid", id);
            return false;
          }

          var items = await Helper.GetTracksByAlbumIdAsync(client.UserId, mediaItemGuid.Value);
          IEnumerable<MediaItem> tracks = null;
          if (onlyUnwatched)
            tracks = items.Where(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") < 100);
          else
            tracks = items;

          if (!(tracks?.Count() > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Album: Couldn't find any tracks");
            return false;
          }

          PlayItemsModel.CheckQueryPlayAction(() => tracks, UI.Presentation.Players.AVType.Audio);
        }
      }

      return true;
    }
  }
}
