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

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.Plugins.SlimTv.Client.TvHandler
{
  public class SlimTvHandler : ITvHandler
  {
    private struct TVSlotContext
    {
      public bool IsPiP;
      public bool CardChanging;
      public string AccessorPath;
      public IChannel Channel;
    }

    private ITvProvider _tvProvider
    {
      get { return ServiceRegistration.Get<ITvProvider>(false); }
    }

    private bool _initialized;
    private readonly TVSlotContext[] _slotContexes = new TVSlotContext[2];

    public const string RES_ERROR_NO_TVPROVIDER = "[SlimTvClient.ErrorNoTvProvider]";

    public void Initialize()
    {
      if (_tvProvider != null && _initialized)
        return;

      if (_tvProvider != null)
        _initialized = _tvProvider.Init();
      else
      {
        string message = ServiceRegistration.Get<ILocalization>().ToString(RES_ERROR_NO_TVPROVIDER);
        ServiceRegistration.Get<ILogger>().Warn("SlimTvHandler: {0}", message);
        ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, "Error", message, true);
      }
    }

    //public ITimeshiftControl TimeshiftControl
    //{
    //  get { return _tvProvider as ITimeshiftControl; }
    //}

    public ITimeshiftControlAsync TimeshiftControl
    {
      get { return _tvProvider as ITimeshiftControlAsync; }
    }

    public IChannelAndGroupInfoAsync ChannelAndGroupInfo
    {
      get { return _tvProvider as IChannelAndGroupInfoAsync; }
    }

    //public IProgramInfo ProgramInfo
    //{
    //  get { return _tvProvider as IProgramInfo; }
    //}

    public IProgramInfoAsync ProgramInfo
    {
      get { return _tvProvider as IProgramInfoAsync; }
    }

    public IScheduleControlAsync ScheduleControl
    {
      get { return _tvProvider as IScheduleControlAsync; }
    }

    //public IProgram CurrentProgram
    //{
    //  get { return GetCurrentProgram(GetChannel(PlayerContextIndex.PRIMARY)); }
    //}

    //public IProgram NextProgram
    //{
    //  get { return GetNextProgram(GetChannel(PlayerContextIndex.PRIMARY)); }
    //}

    //public IProgram GetCurrentProgram(IChannel channel)
    //{
    //  IProgram currentProgram;
    //  IProgram nextProgram;
    //  if (ProgramInfo != null && ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
    //    return currentProgram;

    //  return null;
    //}

    //public IProgram GetNextProgram(IChannel channel)
    //{
    //  IProgram currentProgram;
    //  IProgram nextProgram;
    //  if (ProgramInfo != null && ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
    //    return nextProgram;

    //  return null;
    //}

    /// <summary>
    /// Gets a value how many slots are currently used for timeshifting (0..2).
    /// </summary>
    public int NumberOfActiveSlots
    {
      get { return (_slotContexes[0].Channel == null ? 0 : 1) + (_slotContexes[1].Channel == null ? 0 : 1); }
    }

    private int GetMatchingSlotIndex(int requestedSlotIndex)
    {
      // requested index 0: master video
      // requested index 1: PiP video

      // if only one stream is active, reset all PiP information here
      if (NumberOfActiveSlots == 1)
      {
        _slotContexes[0].IsPiP = false;
        _slotContexes[1].IsPiP = false;
      }

      // when both are active, return the index that matches to master/PiP
      if (NumberOfActiveSlots == 2)
      {
        if (requestedSlotIndex == 0)
          return _slotContexes[0].IsPiP ? 1 : 0;

        if (requestedSlotIndex == 1)
          return _slotContexes[0].IsPiP ? 0 : 1;
      }
      // when one is active and PiP requested, return the free slot
      if (requestedSlotIndex == 1 && NumberOfActiveSlots >= 1)
      {
        if (_slotContexes[0].Channel != null)
        {
          _slotContexes[0].IsPiP = false;
          _slotContexes[1].IsPiP = true;
          return 1;
        }
        if (_slotContexes[1].Channel != null)
        {
          _slotContexes[1].IsPiP = false;
          _slotContexes[0].IsPiP = true;
          return 0;
        }
      }

      // when one is active and master is requested, return the used slot
      if (requestedSlotIndex == 0 && NumberOfActiveSlots >= 1)
        return _slotContexes[0].Channel != null ? 0 : 1;

      return 0;
    }

    public PlayerContextConcurrencyMode GetMatchingPlayMode()
    {
      // no tv slots active? then stop all and play.
      if (NumberOfActiveSlots == 0)
        return PlayerContextConcurrencyMode.None;

      return PlayerContextConcurrencyMode.ConcurrentVideo;
    }

    // Note: the slotIndex represents the server side stream, which is not related to the PlayerSlot.
    public IChannel GetChannel(int slotIndex)
    {
      return TimeshiftControl?.GetChannel(GetMatchingSlotIndex(slotIndex));
    }

    public async Task<bool> StartTimeshiftAsync(int slotIndex, IChannel channel)
    {
      if (TimeshiftControl == null || channel == null)
        return false;

      ServiceRegistration.Get<ILogger>().Debug("SlimTvHandler: StartTimeshift slot {0} for channel '{1}'", slotIndex, channel.Name);

      int newSlotIndex = GetMatchingSlotIndex(slotIndex);
      var result = await TimeshiftControl.StartTimeshiftAsync(newSlotIndex, channel);
      IList<MultipleMediaItemAspect> pras;
      MediaItem timeshiftMediaItem = result.Result;
      if (result.Success && timeshiftMediaItem != null && MediaItemAspect.TryGetAspects(timeshiftMediaItem.Aspects, ProviderResourceAspect.Metadata, out pras))
      {
        string newAccessorPath = (string)pras[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

        // if slot was empty, start a new player
        if (_slotContexes[newSlotIndex].AccessorPath == null)
        {
          AddOrUpdateTimeshiftContext(timeshiftMediaItem as LiveTvMediaItem, channel);
          PlayerContextConcurrencyMode playMode = GetMatchingPlayMode();
          await PlayItemsModel.PlayOrEnqueueItem(timeshiftMediaItem, true, playMode);
        }
        else
        {
          try
          {
            _slotContexes[newSlotIndex].CardChanging = true;
            UpdateExistingMediaItem(timeshiftMediaItem);
          }
          finally
          {
            _slotContexes[newSlotIndex].CardChanging = false;
          }
        }
        _slotContexes[newSlotIndex].AccessorPath = newAccessorPath;
        _slotContexes[newSlotIndex].Channel = channel;
      }
      return result.Success;
    }

    /// <summary>
    /// Creates a new <see cref="TimeshiftContext"/> and fills the <see cref="LiveTvMediaItem.TimeshiftContexes"/> with it.
    /// A new context is created for each channel change or for changed programs on same channel.
    /// </summary>
    /// <param name="timeshiftMediaItem">MediaItem</param>
    /// <param name="channel">Current channel.</param>
    private void AddOrUpdateTimeshiftContext(LiveTvMediaItem timeshiftMediaItem, IChannel channel)
    {
      TimeshiftContext tsContext = new TimeshiftContext { Channel = channel };
      // Remove the newly tuned channel from history if present
      timeshiftMediaItem.TimeshiftContexes.Where(tc=>tc.Channel.ChannelId == channel.ChannelId).ToList().
        ForEach(context => timeshiftMediaItem.TimeshiftContexes.Remove(context));
      // Then add the new context to the end
      timeshiftMediaItem.TimeshiftContexes.Add(tsContext);
      timeshiftMediaItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] = channel;
    }

    private void UpdateExistingMediaItem(MediaItem timeshiftMediaItem)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
      {
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
        if (playerContext == null)
          continue;
        LiveTvMediaItem liveTvMediaItem = playerContext.CurrentMediaItem as LiveTvMediaItem;
        LiveTvMediaItem newLiveTvMediaItem = timeshiftMediaItem as LiveTvMediaItem;
        // Check if this is "our" media item to update.
        if (liveTvMediaItem == null || newLiveTvMediaItem == null || !IsSameSlot(liveTvMediaItem, newLiveTvMediaItem))
          continue;

        if (!IsSameLiveTvItem(liveTvMediaItem, newLiveTvMediaItem))
        {
          // Switch MediaItem in current slot, the LiveTvPlayer implements IReusablePlayer and will change its source without need to change full player.
          playerContext.DoPlay(newLiveTvMediaItem);
          // Copy old channel history into new item
          liveTvMediaItem.TimeshiftContexes.ToList().ForEach(tc => newLiveTvMediaItem.TimeshiftContexes.Add(tc));
          // Use new MediaItem, so new context will be added to new instance.
          liveTvMediaItem = newLiveTvMediaItem;
        }
        // Add new timeshift context
        AddOrUpdateTimeshiftContext(liveTvMediaItem, newLiveTvMediaItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel);
      }
    }

    /// <summary>
    /// Checks if both <see cref="LiveTvMediaItem"/> are representing the same player slot.
    /// </summary>
    /// <param name="oldItem"></param>
    /// <param name="newItem"></param>
    /// <returns><c>true</c> if same</returns>
    protected bool IsSameSlot(LiveTvMediaItem oldItem, LiveTvMediaItem newItem)
    {
      return ((int)oldItem.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX] == (int)newItem.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX]);
    }

    /// <summary>
    /// Checks if both <see cref="LiveTvMediaItem"/> are of same type, which includes mimeType and streaming url. This check is used to detected
    /// card changes on server and switching between radio and tv channels.
    /// </summary>
    /// <param name="oldItem"></param>
    /// <param name="newItem"></param>
    /// <returns></returns>
    protected bool IsSameLiveTvItem(LiveTvMediaItem oldItem, LiveTvMediaItem newItem)
    {
      IList<MultipleMediaItemAspect> oldPras;
      if (!MediaItemAspect.TryGetAspects(oldItem.Aspects, ProviderResourceAspect.Metadata, out oldPras))
        return false;

      IList<MultipleMediaItemAspect> newPras;
      if (!MediaItemAspect.TryGetAspects(newItem.Aspects, ProviderResourceAspect.Metadata, out newPras))
        return false;

      string oldPath = oldPras[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH).ToString();
      string newPath = newPras[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH).ToString();
      if (oldPath != newPath)
        return false;

      IChannel oldChannel = oldItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel;
      IChannel newChannel = newItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel;
      if (oldChannel != null && newChannel != null && oldChannel.MediaType != newChannel.MediaType)
        return false;

      string oldMimeType;
      string oldTitle;
      string newMimeType;
      string newTitle;
      return oldItem.GetPlayData(out oldMimeType, out oldTitle) && newItem.GetPlayData(out newMimeType, out newTitle) && oldMimeType == newMimeType;
    }

    public async Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      if (TimeshiftControl == null)
        return false;

      _slotContexes[slotIndex].AccessorPath = null;
      _slotContexes[slotIndex].Channel = null;

      return await TimeshiftControl.StopTimeshiftAsync(slotIndex);
    }

    public bool DisposeSlot(int slotIndex)
    {
      // when we change the channel and the card was changed, don't need to stop the 
      if (_slotContexes[slotIndex].CardChanging)
        return true;
      return StopTimeshiftAsync(slotIndex).Result;
    }

    public async Task<bool> WatchRecordingFromBeginningAsync(IProgram program)
    {
      var result = await ScheduleControl.GetRecordingFileOrStreamAsync(program);
      if (result.Success)
      {
        string fileOrStream = result.Result;

        var channelResult = await ChannelAndGroupInfo.GetChannelAsync(program.ChannelId);
        if (channelResult.Success)
        {

          MediaItem recordig = SlimTvMediaItemBuilder.CreateRecordingMediaItem(0, fileOrStream, program, channelResult.Result);
          PlayItemsModel.CheckQueryPlayAction(recordig);
          return true;
        }
      }
      return false;
    }

    #region IDisposable Member

    public void Dispose()
    {
      DeInit();
    }

    private void DeInit()
    {
      ITvProvider provider = _tvProvider;
      if (provider != null)
        provider.DeInit();
    }

    #endregion
  }
}
