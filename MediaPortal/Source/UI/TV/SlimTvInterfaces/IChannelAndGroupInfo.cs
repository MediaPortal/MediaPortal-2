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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  /// <summary>
  /// IChannelAndGroupInfoAsync defines all actions and properties for TV channel and channel group infos.
  /// </summary>
  public interface IChannelAndGroupInfoAsync
  {
    /// <summary>
    /// Gets the list of available channel groups.
    /// </summary>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> Channel groups.
    /// </returns>
    Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync();

    /// <summary>
    /// Gets the list of channels in a channel group.
    /// </summary>
    /// <param name="group">Channel group.</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> List of channels.
    /// </returns>
    Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group);

    /// <summary>
    /// Gets the channel by given <paramref name="channelId"/>.
    /// </summary>
    /// <param name="channelId">ID of channel</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if at least one program could be found.
    /// <see cref="AsyncResult{T}.Result"/> Channels.
    /// </returns>
    Task<AsyncResult<IChannel>> GetChannelAsync(int channelId);

    /// <summary>
    /// Gets or Sets the ID of the current selected channel.
    /// </summary>
    int SelectedChannelId { get; set; }

    /// <summary>
    /// Gets or Sets the ID of the current selected channel group.
    /// </summary>
    int SelectedChannelGroupId { get; set; }
  }
}
