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
using System.Linq;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.UserProfileDataManagement;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserTVSeries
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = (string)message["Action"];

      if (!string.IsNullOrEmpty(action))
      {
        // Show movie details for this episode
        if (action == "episodetails")
        {
          // TODO: implement?
        }
        // Play a episode
        else if (action == "playepisode")
        {
          string seriesName = (string)message["SeriesName"];
          int seasonNum = (int)message["SeasonNumber"];
          int episodeNum = (int)message["EpisodeNumber"];
          string id = (string)message["SeriesId"];
          int startPos = (message["StartPosition"] != null) ? (int)message["StartPosition"] : 0;

          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Episode: SeriesName: {0}, SeriesId: {1}, SeasonNumber: {2}, EpisodeNumber: {3}, StartPos: {4}", seriesName, id, seasonNum, episodeNum, startPos);

          if (!string.IsNullOrEmpty(seriesName) && string.IsNullOrEmpty(id))
          {
            var item = await Helper.GetMediaItemBySeriesNameAsync(seriesName);
            if (item != null)
              id = item.MediaItemId.ToString();
          }

          Guid mediaItemGuid;
          if (!Guid.TryParse(id, out mediaItemGuid))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episode = await Helper.GetMediaItemsByEpisodeAsync(mediaItemGuid, seasonNum, episodeNum);
          if (!(episode?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't find episode");
            return false;
          }

          await Helper.PlayMediaItemAsync(episode.First().MediaItemId, startPos);
        }
        else if (action == "playunwatchedepisode")
        {
          string seriesName = (string)message["SeriesName"];
          string id = (string)message["SeriesId"];

          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Episode: SeriesName: {0}, SeriesId: {1}", seriesName, id);

          if (!string.IsNullOrEmpty(seriesName) && string.IsNullOrEmpty(id))
          {
            var item = await Helper.GetMediaItemBySeriesNameAsync(seriesName);
            if (item != null)
              id = item.MediaItemId.ToString();
          }

          Guid mediaItemGuid;
          if (!Guid.TryParse(id, out mediaItemGuid))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episodes = await Helper.GetMediaItemsBySeriesAsync(mediaItemGuid);
          if (!(episodes?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't find episodes");
            return false;
          }

          var sortedEpisodes = episodes.Select(e => new Tuple<Guid, SingleMediaItemAspect, int>(e.MediaItemId, MediaItemAspect.GetAspect(e.Aspects, EpisodeAspect.Metadata), Convert.ToInt32(e.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0"))).
            Select(e => new Tuple<Guid, int, int, int>(e.Item1, e.Item2.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON), e.Item2.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE).First(), e.Item3)).OrderBy(e => e.Item2).ThenBy(e => e.Item3);
          var episode = sortedEpisodes.FirstOrDefault(e => e.Item4 < 100);
          if (episode == null)
            episode = sortedEpisodes.LastOrDefault();
          if (episode == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't find episodes");
            return false;
          }

          await Helper.PlayMediaItemAsync(episode.Item1, 0);
        }
        else if (action == "playrandomepisode")
        {
          string seriesName = (string)message["SeriesName"];
          string id = (string)message["SeriesId"];

          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Episode: SeriesName: {0}, SeriesId: {1}", seriesName, id);

          if (!string.IsNullOrEmpty(seriesName) && string.IsNullOrEmpty(id))
          {
            var item = await Helper.GetMediaItemBySeriesNameAsync(seriesName);
            if (item != null)
              id = item.MediaItemId.ToString();
          }

          Guid mediaItemGuid;
          if (!Guid.TryParse(id, out mediaItemGuid))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't convert SeriesId '{0} to Guid", id);
            return false;
          }

          var episodes = await Helper.GetMediaItemsBySeriesAsync(mediaItemGuid);
          if (!(episodes?.Count > 0))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Episode: Couldn't find episodes");
            return false;
          }

          var episodeList = episodes?.ToList();
          var episodeIndex = new Random().Next(0, episodeList.Count - 1);
          await Helper.PlayMediaItemAsync(episodeList[episodeIndex].MediaItemId, 0);
        }
        else if (action == "playseason")
        {
          // TODO: implement? With play list?
        }
        else if (action == "playseries")
        {
          // TODO: implement? With play list?
        }
      }

      return true;
    }
  }
}
