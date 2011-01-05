#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Providers;
using MediaPortal.Plugins.SlimTvClient.Interfaces;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.Plugins.SlimTvClient
{
  public class SlimTvHandler: ITvHandler
  {
    private ITvProvider _tvProvider;
    private string _activeAccessorPath;

    public SlimTvHandler()
    {
      _tvProvider = new SlimTv4HomeProvider();
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

    public IProgram CurrentProgram
    {
      get { return GetCurrentProgram(TimeshiftControl.GetChannel(PlayerManagerConsts.PRIMARY_SLOT)); }
    }

    public IProgram NextProgram
    {
      get { return GetNextProgram(TimeshiftControl.GetChannel(PlayerManagerConsts.PRIMARY_SLOT)); }
    }

    public IProgram GetCurrentProgram(IChannel channel)
    {
      IProgram currentProgram;
      if (ProgramInfo != null && ProgramInfo.GetCurrentProgram(channel, out currentProgram))
        return currentProgram;

      return null;
    }

    public IProgram GetNextProgram(IChannel channel)
    {
      IProgram nextProgram;
      if (ProgramInfo != null && ProgramInfo.GetNextProgram(channel, out nextProgram))
        return nextProgram;

      return null;
    }


    public bool StartTimeshift(int slotIndex, IChannel channel)
    {
      if (TimeshiftControl == null || channel == null)
        return false;

      MediaItem timeshiftMediaItem;
      bool result = TimeshiftControl.StartTimeshift(slotIndex, channel, out timeshiftMediaItem);
      if (result && timeshiftMediaItem != null)
      {
        string newAccessorPath =
          (string) timeshiftMediaItem.Aspects[ProviderResourceAspect.ASPECT_ID].GetAttributeValue(
            ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

        if (_activeAccessorPath != newAccessorPath)
        {
          PlayerContextConcurrencyMode playMode = (slotIndex == PlayerManagerConsts.PRIMARY_SLOT)
                                                    ? PlayerContextConcurrencyMode.None
                                                    : PlayerContextConcurrencyMode.ConcurrentVideo;

          PlayItemsModel.PlayOrEnqueueItem(timeshiftMediaItem, true, playMode);
        }

        _activeAccessorPath = newAccessorPath;
      }

      return result;
    }


    public bool StopTimeshift()
    {
      _activeAccessorPath = null;
      if (TimeshiftControl == null)
        return false;

      return TimeshiftControl.StopTimeshift(0);
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