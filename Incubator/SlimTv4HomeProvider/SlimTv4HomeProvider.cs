#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.ServiceModel;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Plugins.SlimTvClient.Interfaces;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.Plugins.SlimTvClient.Providers.Items;
using TV4Home.Server.TVEInteractionLibrary.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  public class SlimTv4HomeProvider : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo
  {
    private ITVEInteraction _tvServer;
    private IChannel[] _channels = new IChannel[2];

    #region ITvProvider Member

    public IChannel GetChannel(int slotIndex)
    {
      return _channels[slotIndex];
    }

    public string Name
    {
      get { return "TV4Home Provider"; }
    }

    #endregion

    #region ITimeshiftControl Member

    public bool Init()
    {
      try
      {
        _tvServer = ChannelFactory<ITVEInteraction>.CreateChannel(
          new NetNamedPipeBinding {MaxReceivedMessageSize = 10000000},
          new EndpointAddress("net.pipe://localhost/TV4Home.Server.CoreService/TVEInteractionService")
          );
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public bool DeInit()
    {
      if (_tvServer == null)
        return false;
     
      _tvServer.CancelCurrentTimeShifting();
      _tvServer.Disconnect();
      _tvServer = null;
      return true;
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      timeshiftMediaItem = null;

      String streamUrl = _tvServer.SwitchTVServerToChannelAndGetStreamingUrl(channel.ChannelId);
      if (String.IsNullOrEmpty(streamUrl))
        return false;

      _channels[slotIndex] = channel;

      // assign a MediaItem, can be null if streamUrl is the same.
      timeshiftMediaItem = CreateMediaItem(streamUrl);
      return true;
    }

    public bool StopTimeshift(int slotIndex)
    {
      _tvServer.CancelCurrentTimeShifting();
      return true;
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      groups = new List<IChannelGroup>();
      List<WebChannelGroup> tvGroups = _tvServer.GetGroups();
      foreach (WebChannelGroup webChannelGroup in tvGroups)
      {
        groups.Add(new ChannelGroup { ChannelGroupId = webChannelGroup.IdGroup, Name = webChannelGroup.GroupName });
      }
      return true;
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      channels = new List<IChannel>();
      if (group == null)
        return false;

      List<WebChannel> tvChannels = _tvServer.GetChannels(group.ChannelGroupId);
      foreach (WebChannel webChannel in tvChannels)
      {
        channels.Add(new Channel { ChannelId = webChannel.IdChannel, Name = webChannel.Name });
      }
      return true;
    }

    public bool GetChannel(int channelId, out IChannel channel)
    {
      channel = null;
      WebChannel tvChannel = _tvServer.GetChannelById(channelId);
      if (tvChannel != null)
      {
        channel = new Channel {ChannelId = tvChannel.IdChannel, Name = tvChannel.Name};
        return true;
      }
      return false;
    }

    #endregion

    public MediaItem CreateMediaItem(string streamUrl)
    {
      if (!String.IsNullOrEmpty(streamUrl))
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();
        MediaItemAspect providerResourceAspect;
        MediaItemAspect mediaAspect;
        MediaItemAspect movieAspect;

        SlimTvResourceAccessor resourceAccessor = new SlimTvResourceAccessor(streamUrl);
        aspects[ProviderResourceAspect.ASPECT_ID] =
          providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
        aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
        // videoaspect needs to be included to associate player later!
        aspects[VideoAspect.ASPECT_ID] = movieAspect = new MediaItemAspect(VideoAspect.Metadata);

        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH,
                                            resourceAccessor.LocalResourcePath.Serialize());

        mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, "Live TV");
        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/livetv"); //Custom mimetype for LiveTv

        MediaItem tvStream = new MediaItem(new Guid(), aspects);
        return tvStream;
      }
      return null;
    }

    #region IProgramInfo Member

    public bool GetCurrentProgram(IChannel channel, out IProgram program)
    {
      program = null;
      if (channel == null)
        return false;

      WebProgram tvProgram = _tvServer.GetCurrentProgramOnChannel(channel.ChannelId);
      if (tvProgram != null)
      {
        program = new Program(tvProgram);
        return true;
      }
      return false;
    }

    public bool GetNextProgram(IChannel channel, out IProgram program)
    {
      program = null;
      if (channel == null)
        return false;

      IProgram currentProgram;
      if (GetCurrentProgram(channel, out currentProgram))
      {
        List<WebProgram> nextPrograms = _tvServer.GetProgramsForChannel(channel.ChannelId,
                                                                       currentProgram.EndTime.AddMinutes(1),
                                                                       currentProgram.EndTime.AddMinutes(1));
        if (nextPrograms != null && nextPrograms.Count > 0)
        {
          program = new Program(nextPrograms[0]);
          return true;
        }
      }
      return false;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = null;
      if (channel == null)
        return false;

      programs = new List<IProgram>();

      List<WebProgram> tvPrograms = _tvServer.GetProgramsForChannel(channel.ChannelId, from, to);
      foreach (WebProgram webProgram in tvPrograms)
        programs.Add(new Program(webProgram));

      return true;
    }

    #endregion

    #region IProgramInfo Member


    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}
