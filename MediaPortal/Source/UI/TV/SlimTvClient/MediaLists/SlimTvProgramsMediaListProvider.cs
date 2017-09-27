#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Commands;
using MediaPortal.Plugins.SlimTv.Client.Models;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvProgramsMediaListProvider : IMediaListProvider
  {
    protected ITvHandler _tvHandler;
    protected IEnumerable<Tuple<int, string>> _channelList;

    public SlimTvProgramsMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    private ListItem CreateProgramItem(IChannel channel, IProgram program)
    {
      ProgramProperties programProperties = new ProgramProperties();
      programProperties.SetProgram(program, channel);

      ListItem item = new ProgramListItem(programProperties)
      {
        Command = new MethodDelegateCommand(() => SlimTvModelBase.TuneChannel(channel)),
      };
      item.SetLabel("ChannelName", channel.Name);
      item.AdditionalProperties["PROGRAM"] = program;
      return item;
    }

    public bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      if (_tvHandler == null)
      {
        ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
        tvHandler.Initialize();
        if (tvHandler.ChannelAndGroupInfo == null)
          return false;
        _tvHandler = tvHandler;
      }

      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PeriodicMinute) == UpdateReason.PeriodicMinute ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete)
      {

        if(_channelList == null ||(updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PlaybackComplete) == UpdateReason.PlaybackComplete)
          {
          Guid? userProfile = null;
          IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
          if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
          {
            userProfile = userProfileDataManagement.CurrentUser.ProfileId;
          }

          IEnumerable<Tuple<int, string>> channelList;
          if (userProfile.HasValue && userProfileDataManagement.UserProfileDataManagement.GetUserAdditionalDataList(userProfile.Value, UserDataKeysKnown.KEY_CHANNEL_PLAY_COUNT,
            out channelList, UserDataKeysKnown.KEY_CHANNEL_PLAY_COUNT, null, Convert.ToUInt32(maxItems)))
          {
            _channelList = channelList;
          }
        }

        if (_tvHandler.ProgramInfo == null)
          return false;

        IDictionary<IChannel, IProgram> currentPrograms = new Dictionary<IChannel, IProgram>();
        foreach (var channelId in _channelList.Select(c => c.Item1))
        {
          IProgram currentProgram;
          IProgram nextProgram;
          IChannel channel = ChannelContext.Instance.Channels.FirstOrDefault(c => c.ChannelId == channelId && c.MediaType == MediaType.TV);
          if (channel != null)
          {
            if (_tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
            {
              currentPrograms.Add(channel, currentProgram);
            }
          }
        }

        List<ListItem> programtList = new List<ListItem>();
        foreach (var program in currentPrograms)
        {
          var item = CreateProgramItem(program.Key, program.Value);
          programtList.Add(item);
        }

        if (!AllItems.Select(s => ((IProgram)s.AdditionalProperties["PROGRAM"]).ProgramId).SequenceEqual(programtList.Select(si => ((IProgram)si.AdditionalProperties["PROGRAM"]).ProgramId)))
        {
          AllItems.Clear();
          foreach (var program in programtList)
          {
            AllItems.Add(program);
          }
          AllItems.FireChange();
        }
      }
      return true;
    }
  }
}
