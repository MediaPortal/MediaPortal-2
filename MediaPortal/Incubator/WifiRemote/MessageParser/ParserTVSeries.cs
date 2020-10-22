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
  internal class ParserTVSeries : BaseParser
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = GetMessageValue<string>(message, "Action");
      var client = sender.GetRemoteClient();

      if (!string.IsNullOrEmpty(action))
      {
        string search = GetMessageValue<string>(message, "Search");
        int offset = GetMessageValue<int>(message, "Offset");
        int count = GetMessageValue<int>(message, "Count", 10);
        string seriesName = GetMessageValue<string>(message, "SeriesName");
        int seasonNum = GetMessageValue<int>(message, "SeasonNumber");
        string id = GetMessageValue<string>(message, "SeriesId");
        int episodeNum = GetMessageValue<int>(message, "EpisodeNumber");
        int startPos = GetMessageValue<int>(message, "StartPosition");
        bool onlyUnwatched = GetMessageValue<bool>(message, "OnlyUnwatchedEpisodes");

        // Search for series
        if (action.Equals("seriessearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<SeriesShowInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetSeriesBySeriesSearchAsync);
          SendMessageToClient.Send(new MessageSeries { Series = list }, sender, true);
        }
        // Show series list
        else if (action.Equals("serieslist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<SeriesShowInfo>(client, null, Convert.ToUInt32(count), Convert.ToUInt32(offset), Helper.GetSeriesBySeriesSearchAsync);
          SendMessageToClient.Send(new MessageSeries { Series = list }, sender, true);
        }
        // Show season list for series
        else if (action.Equals("seasonlist", StringComparison.InvariantCultureIgnoreCase))
        {
          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: List Seasons: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var list = await Helper.GetSeasonsBySeriesIdAsync(client.UserId, mediaItemGuid.Value);
          SendMessageToClient.Send(new MessageSeasons { Seasons = list.Select(i => new SeriesSeasonInfo(i)).ToList() }, sender, true);
        }
        // Show episode list for series season
        else if (action.Equals("episodelist", StringComparison.InvariantCultureIgnoreCase))
        {
          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: List Episodes: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var list = await Helper.GetEpisodesBySeriesSeasonAsync(client.UserId, mediaItemGuid.Value, seasonNum);
          SendMessageToClient.Send(new MessageEpisodes { Episodes = list.Select(i => new SeriesEpisodeInfo(i)).ToList() }, sender, true);
        }
        // Show movie details for this episode
        else if (action.Equals("episodetails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a episode
        else if (action.Equals("playepisode", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Episode: SeriesName: {0}, SeriesId: {1}, SeasonNumber: {2}, EpisodeNumber: {3}, StartPos: {4}", seriesName, id, seasonNum, episodeNum, startPos);

          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episode = await Helper.GetEpisodeBySeriesEpisodeAsync(client.UserId, mediaItemGuid.Value, seasonNum, episodeNum);
          if (episode == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't find episode");
            return false;
          }

          await Helper.PlayMediaItemAsync(episode.MediaItemId, startPos);
        }
        else if (action.Equals("playunwatchedepisode", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Episode: SeriesName: {0}, SeriesId: {1}", seriesName, id);

          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episodes = await Helper.GetEpisodesBySeriesIdAsync(client.UserId, mediaItemGuid.Value);
          if (!(episodes?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't find episodes");
            return false;
          }

          var episode = episodes.FirstOrDefault(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") < 100);
          if (episode == null)
            episode = episodes.LastOrDefault(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") == 100);
          if (episode == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't find episodes");
            return false;
          }

          await Helper.PlayMediaItemAsync(episode.MediaItemId, 0);
        }
        else if (action.Equals("playrandomepisode", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Episode: SeriesName: {0}, SeriesId: {1}", seriesName, id);

          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episodes = await Helper.GetEpisodesBySeriesIdAsync(client.UserId, mediaItemGuid.Value);
          if (!(episodes?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Episode: Couldn't find episodes");
            return false;
          }

          var episodeList = episodes?.ToList();
          var episodeIndex = new Random().Next(0, episodeList.Count - 1);
          await Helper.PlayMediaItemAsync(episodeList[episodeIndex].MediaItemId, 0);
        }
        else if (action.Equals("playseason", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Season: SeriesName: {0}, SeriesId: {1}, SeasonNumber: {2}, OnlyUnwatchedEpisodes: {3}", seriesName, id, seasonNum, onlyUnwatched);

          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Season: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var items = await Helper.GetEpisodesBySeriesSeasonAsync(client.UserId, mediaItemGuid.Value, seasonNum);
          IEnumerable<MediaItem> episodes = null;
          if (onlyUnwatched)
            episodes = items.Where(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") < 100);
          else
            episodes = items;

          if (!(episodes?.Count() > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Season: Couldn't find any episodes");
            return false;
          }

          PlayItemsModel.CheckQueryPlayAction(() => episodes, UI.Presentation.Players.AVType.Video);
        }
        else if (action.Equals("playseries", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Series: SeriesName: {0}, SeriesId: {1}, OnlyUnwatchedEpisodes: {2}", seriesName, id, onlyUnwatched);

          var mediaItemGuid = await GetIdFromNameAsync(client, seriesName, id, Helper.GetSeriesBySeriesNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Series: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var items = await Helper.GetEpisodesBySeriesIdAsync(client.UserId, mediaItemGuid.Value);
          IEnumerable<MediaItem> episodes = null;
          if (onlyUnwatched)
            episodes = items.Where(e => Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") < 100);
          else
            episodes = items;

          if (!(episodes?.Count() > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Series: Couldn't find any episodes");
            return false;
          }

          PlayItemsModel.CheckQueryPlayAction(() => episodes, UI.Presentation.Players.AVType.Video);
        }
      }

      return true;
    }
  }
}
