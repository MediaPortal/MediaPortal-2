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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.Workflow;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal abstract class BaseParser
  {
    protected static T GetMessageValue<T>(JObject message, string property)
    {
      return GetMessageValue(message, property, default(T));
    }

    protected static T GetMessageValue<T>(JObject message, string property, T defaultValue)
    {
      if (message.TryGetValue(property, StringComparison.InvariantCultureIgnoreCase, out var val))
        return (T)Convert.ChangeType(val, typeof(T));

      return defaultValue;
    }

    protected static async Task<Guid?> GetIdFromNameAsync(RemoteClient client, string name, string id, Func<Guid?, string, Task<MediaItem>> idFromNameFunc)
    {
      if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(id))
      {
        var item = await idFromNameFunc(client.UserId, name);
        id = item?.MediaItemId.ToString();
      }

      if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(id) && idFromNameFunc != Helper.GetMediaItemByFileNameAsync)
      {
        var item = await Helper.GetMediaItemByFileNameAsync(client.UserId, name);
        id = item?.MediaItemId.ToString();
      }

      if (!Guid.TryParse(id, out Guid mediaItemGuid))
        return null;

      return mediaItemGuid;
    }

    protected static async Task<bool> PlayChannelAsync(int channelId)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: Play Channel: No tv handler");
        return false;
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channel = await tvHandler.ChannelAndGroupInfo.GetChannelAsync(channelId);
      if (!channel.Success)
      {
        Logger.Info($"WifiRemote: Play Channel: Channel with id '{0}' not found", channelId);
        return false;
      }

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
      if (model != null)
        await model.Tune(channel.Result);

      return true;
    }

    protected static async Task<IList<ISchedule>> GetSchedulesAsync()
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: List Schedules: No tv handler");
        return new List<ISchedule>();
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var schedules = await tvHandler.ScheduleControl.GetSchedulesAsync();
      if (!schedules.Success)
      {
        Logger.Error($"WifiRemote: List Schedules: Error getting schedules");
        return new List<ISchedule>();
      }

      return schedules.Result;
    }

    protected static async Task<bool> RemoveSchedulesAsync(int scheduleId)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: Remove Schedule: No tv handler");
        return false;
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var schedules = await tvHandler.ScheduleControl.GetSchedulesAsync();
      if (!schedules.Success)
      {
        Logger.Error($"WifiRemote: Remove Schedule: Error getting schedules");
        return false;
      }

      var schedule = schedules.Result.FirstOrDefault(s => s.ScheduleId == scheduleId);
      if (schedule == null)
      {
        Logger.Error($"WifiRemote: Remove Schedule: Schedule with id '{0}' not found", scheduleId);
        return false;
      }

      return await tvHandler.ScheduleControl.RemoveScheduleAsync(schedule);
    }

    protected static async Task<IList<IChannelGroup>> GetChannelGroupsAsync(bool isTv)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: List Channel Groups: No tv handler");
        return new List<IChannelGroup>();
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channelGroups = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();
      if (!channelGroups.Success)
      {
        Logger.Error($"WifiRemote: List Channel Groups: Error getting groups");
        return new List<IChannelGroup>();
      }

      return channelGroups.Result.Where(g => g.MediaType == (isTv ? MediaType.TV : MediaType.Radio)).ToList();
    }

    protected static async Task<IList<IChannel>> GetChannelsAsync(int channelGroupId, bool isTv)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: List Channels: No tv handler");
        return new List<IChannel>();
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channelGroups = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();
      if (!channelGroups.Success)
      {
        Logger.Error($"WifiRemote: List Channels: Error getting groups");
        return new List<IChannel>();
      }

      var channelGroup = channelGroups.Result.FirstOrDefault(g => g.ChannelGroupId == channelGroupId && g.MediaType == (isTv ? MediaType.TV : MediaType.Radio));
      if (channelGroup == null)
      {
        Logger.Info($"WifiRemote: List Channels: Channel group with id '{0}' not found", channelGroupId);
        return new List<IChannel>();
      }

      var channels = await tvHandler.ChannelAndGroupInfo.GetChannelsAsync(channelGroup);
      if (!channels.Success)
      {
        Logger.Error($"WifiRemote: List Channels: Error getting channels");
        return new List<IChannel>();
      }

      return channels.Result;
    }

    protected static async Task<IList<IProgram>> GetChannelEpgAsync(int channelId, int hours, bool isTv)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: List Channel EPG: No tv handler");
        return new List<IProgram>();
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channel = await tvHandler.ChannelAndGroupInfo.GetChannelAsync(channelId);
      if (!channel.Success)
      {
        Logger.Info($"WifiRemote: List Channel EPG: Channel with id '{0}' not found", channelId);
        return new List<IProgram>();
      }

      var programs = await tvHandler.ProgramInfo.GetProgramsAsync(channel.Result, DateTime.Now, DateTime.Now.AddHours(hours));
      if (!programs.Success)
      {
        Logger.Error($"WifiRemote: List Channel EPG: Error getting programs");
        return new List<IProgram>();
      }

      return programs.Result;
    }

    protected static async Task<IList<IProgram>> GetEpgAsync(int channelGroupId, int hours, bool isTv)
    {
      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Error($"WifiRemote: List EPG: No tv handler");
        return new List<IProgram>();
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channelGroups = await tvHandler.ChannelAndGroupInfo.GetChannelGroupsAsync();
      if (!channelGroups.Success)
      {
        Logger.Error($"WifiRemote: List EPG: Error getting groups");
        return new List<IProgram>();
      }

      var channelGroup = channelGroups.Result.FirstOrDefault(g => g.ChannelGroupId == channelGroupId && g.MediaType == (isTv ? MediaType.TV : MediaType.Radio));
      if (channelGroup == null)
      {
        Logger.Info($"WifiRemote: List EPG: Channel group with id '{0}' not found", channelGroupId);
        return new List<IProgram>();
      }

      var programs = await tvHandler.ProgramInfo.GetProgramsGroupAsync(channelGroup, DateTime.Now, DateTime.Now.AddHours(hours));
      if (!programs.Success)
      {
        Logger.Error($"WifiRemote: List EPG: Error getting programs");
        return new List<IProgram>();
      }

      return programs.Result;
    }

    protected static async Task<List<T>> GetItemListAsync<T>(RemoteClient client, string search, uint? limit, uint? offset, Func<Guid?, string, uint?, uint?, Task<IList<MediaItem>>> searchFunc)
    {
      var items = await searchFunc(client.UserId, search, limit, offset);
      List<object> listItems = new List<object>();
      if (typeof(T) == typeof(MovingPicturesInfo))
      {
        foreach(var item in items.Where(i => i.Aspects.ContainsKey(MovieAspect.ASPECT_ID)))
          listItems.Add(new MovingPicturesInfo(item));
      }
      else if (typeof(T) == typeof(MusicAlbumInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID)))
          listItems.Add(new MusicAlbumInfo(item));
      }
      else if (typeof(T) == typeof(MusicInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(AudioAspect.ASPECT_ID)))
          listItems.Add(new MusicInfo(item));
      }
      else if (typeof(T) == typeof(SeriesEpisodeInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID)))
          listItems.Add(new SeriesEpisodeInfo(item));
      }
      else if (typeof(T) == typeof(SeriesSeasonInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(SeasonAspect.ASPECT_ID)))
          listItems.Add(new SeriesSeasonInfo(item));
      }
      else if (typeof(T) == typeof(SeriesShowInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(SeriesAspect.ASPECT_ID)))
          listItems.Add(new SeriesShowInfo(item));
      }
      else if (typeof(T) == typeof(VideoInfo))
      {
        foreach (var item in items.Where(i => !i.Aspects.ContainsKey(MovieAspect.ASPECT_ID) && i.Aspects.ContainsKey(VideoAspect.ASPECT_ID)))
          listItems.Add(new VideoInfo(item));
      }
      else if (typeof(T) == typeof(ImageInfo))
      {
        foreach (var item in items.Where(i => i.Aspects.ContainsKey(ImageAspect.ASPECT_ID)))
          listItems.Add(new ImageInfo(item));
      }
      return listItems.Cast<T>().ToList();
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
