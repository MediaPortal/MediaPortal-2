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

using MediaPortal.Common.Commands;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvProgramsMediaListProvider : SlimTvMediaListProviderBase
  {
    protected ICollection<IChannel> _currentChannels;
    protected ICollection<Tuple<IProgram, IChannel>> _currentPrograms = new List<Tuple<IProgram, IChannel>>();

    private ListItem CreateProgramItem(IProgram program, IChannel channel)
    {
      ProgramProperties programProperties = new ProgramProperties();
      programProperties.SetProgram(program, channel);

      ProgramListItem item = new ProgramListItem(programProperties)
      {
        Command = new AsyncMethodDelegateCommand(() => SlimTvModelBase.TuneChannel(channel)),
      };
      item.SetLabel("ChannelName", channel.Name);
      item.AdditionalProperties["PROGRAM"] = program;
      return item;
    }

    public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
    {
      if (!TryInitTvHandler() || _tvHandler.ProgramInfo == null)
        return false;

      if (_tvHandler.ProgramInfo == null)
        return false;

      if (!updateReason.HasFlag(UpdateReason.Forced) && !updateReason.HasFlag(UpdateReason.PlaybackComplete) && !updateReason.HasFlag(UpdateReason.PeriodicMinute))
        return true;

      ICollection<IChannel> channels;
      if (_currentChannels == null || updateReason.HasFlag(UpdateReason.Forced) || updateReason.HasFlag(UpdateReason.PlaybackComplete))
        channels = _currentChannels = await GetUserChannelList(maxItems, UserDataKeysKnown.KEY_CHANNEL_PLAY_COUNT, true);
      else
        channels = _currentChannels;

      IList<Tuple<IProgram, IChannel>> programs = new List<Tuple<IProgram, IChannel>>();
      foreach (IChannel channel in channels)
      {
        var result = await _tvHandler.ProgramInfo.GetNowNextProgramAsync(channel);
        if (!result.Success)
          continue;

        IProgram currentProgram = result.Result[0];

        if (currentProgram != null)
          programs.Add(new Tuple<IProgram, IChannel>(currentProgram, channel));
      }

      lock (_allItems.SyncRoot)
      {
        if (_currentPrograms.Select(p => p.Item1.ProgramId).SequenceEqual(programs.Select(p => p.Item1.ProgramId)))
          return true;
        _currentPrograms = programs;
        _allItems.Clear();
        foreach (var program in programs)
          _allItems.Add(CreateProgramItem(program.Item1, program.Item2));
      }
      _allItems.FireChange();
      return true;
    }
  }
}
