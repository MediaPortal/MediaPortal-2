#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Presentation.Players;
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

    private ITvProvider _tvProvider;
    private readonly TVSlotContext[] _slotContexes = new TVSlotContext[2];

    public void Initialize()
    {
      if (_tvProvider != null)
        return;

      _tvProvider = ServiceRegistration.Get<ITvProvider>();
      if (_tvProvider != null)
        _tvProvider.Init();
    }

    public ITimeshiftControl TimeshiftControl
    {
      get { return _tvProvider as ITimeshiftControl; }
    }

    public IChannelAndGroupInfo ChannelAndGroupInfo
    {
      get { return _tvProvider as IChannelAndGroupInfo; }
    }

    public IProgramInfo ProgramInfo
    {
      get { return _tvProvider as IProgramInfo; }
    }

    public IScheduleControl ScheduleControl
    {
      get { return _tvProvider as IScheduleControl; }
    }

    public IProgram CurrentProgram
    {
      get { return GetCurrentProgram(GetChannel(PlayerManagerConsts.PRIMARY_SLOT)); }
    }

    public IProgram NextProgram
    {
      get { return GetNextProgram(GetChannel(PlayerManagerConsts.PRIMARY_SLOT)); }
    }

    public IProgram GetCurrentProgram(IChannel channel)
    {
      IProgram currentProgram;
      IProgram nextProgram;
      if (ProgramInfo != null && ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
        return currentProgram;

      return null;
    }

    public IProgram GetNextProgram(IChannel channel)
    {
      IProgram currentProgram;
      IProgram nextProgram;
      if (ProgramInfo != null && ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
        return nextProgram;

      return null;
    }

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

    private PlayerContextConcurrencyMode GetMatchingPlayMode()
    {
      // no tv slots active? then stop all and play.
      if (NumberOfActiveSlots == 0)
        return PlayerContextConcurrencyMode.None;

      return PlayerContextConcurrencyMode.ConcurrentVideo;
    }

    public IChannel GetChannel(int slotIndex)
    {
      if (TimeshiftControl == null)
        return null;

      return TimeshiftControl.GetChannel(GetMatchingSlotIndex(slotIndex));
    }

    public bool StartTimeshift(int slotIndex, IChannel channel)
    {
      if (TimeshiftControl == null || channel == null)
        return false;

      int newSlotIndex = GetMatchingSlotIndex(slotIndex);
      MediaItem timeshiftMediaItem;
      bool result = TimeshiftControl.StartTimeshift(newSlotIndex, channel, out timeshiftMediaItem);
      if (result && timeshiftMediaItem != null)
      {
        string newAccessorPath = (string) timeshiftMediaItem.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

        // if slot was empty, start a new player
        if (_slotContexes[newSlotIndex].AccessorPath == null)
        {
          AddTimeshiftContext(timeshiftMediaItem as LiveTvMediaItem, channel);
          PlayerContextConcurrencyMode playMode = GetMatchingPlayMode();
          PlayItemsModel.PlayOrEnqueueItem(timeshiftMediaItem, true, playMode);
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

      return result;
    }

    private void AddTimeshiftContext(LiveTvMediaItem timeshiftMediaItem, IChannel channel)
    {
      IProgram program = GetCurrentProgram(channel);
      TimeshiftContext tsContext = new TimeshiftContext
                                     {
                                       Channel = channel,
                                       Program = program,
                                       TuneInTime = DateTime.Now
                                     };

      int tc = timeshiftMediaItem.TimeshiftContexes.Count;
      if (tc > 0)
      {
        ITimeshiftContext lastContext = timeshiftMediaItem.TimeshiftContexes[tc - 1];
        lastContext.TimeshiftDuration = DateTime.Now - lastContext.TuneInTime;
      }
      timeshiftMediaItem.TimeshiftContexes.Add(tsContext);
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
          // Switch MediaItem in current slot
          playerContext.DoPlay(newLiveTvMediaItem);
          // Clear former timeshift contexes (card change cause loss of buffer in rtsp mode).
          liveTvMediaItem.TimeshiftContexes.Clear();
        }
        // Add new timeshift context
        AddTimeshiftContext(liveTvMediaItem, newLiveTvMediaItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel);
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
      return ((int) oldItem.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX] == (int) newItem.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX]);
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
      if (oldItem.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH).ToString() !=
               newItem.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH).ToString())
        return false;

      IChannel oldChannel = oldItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel;
      IChannel newChannel=newItem.AdditionalProperties[LiveTvMediaItem.CHANNEL] as IChannel;
      if (oldChannel != null && newChannel != null && oldChannel.MediaType != newChannel.MediaType)
        return false;

      string oldMimeType;
      string oldTitle;
      string newMimeType;
      string newTitle;
      return oldItem.GetPlayData(out oldMimeType, out oldTitle) && newItem.GetPlayData(out newMimeType, out newTitle) && oldMimeType == newMimeType;
    }

    public bool StopTimeshift(int slotIndex)
    {
      if (TimeshiftControl == null)
        return false;

      _slotContexes[slotIndex].AccessorPath = null;
      _slotContexes[slotIndex].Channel = null;

      return TimeshiftControl.StopTimeshift(slotIndex);
    }

    public bool DisposeSlot(int slotIndex)
    {
      // when we change the channel and the card was changed, don't need to stop the 
      if (_slotContexes[slotIndex].CardChanging)
        return true;
      return StopTimeshift(slotIndex);
    }

    #region IDisposable Member

    public void Dispose()
    {
      if (_tvProvider != null)
      {
        _tvProvider.DeInit();
        _tvProvider = null;
      }
    }

    #endregion
  }
}