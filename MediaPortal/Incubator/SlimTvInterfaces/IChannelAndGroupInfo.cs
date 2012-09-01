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

using System.Collections.Generic;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  /// <summary>
  /// IChannelAndGroupInfo defines all actions and properties for TV channel and channel group infos.
  /// </summary>
  public interface IChannelAndGroupInfo
  {
    /// <summary>
    /// Gets the list of available channel groups.
    /// </summary>
    /// <param name="groups">Channel groups.</param>
    /// <returns>True if succeeded.</returns>
    bool GetChannelGroups(out IList<IChannelGroup> groups);

    /// <summary>
    /// Gets the list of channels in a channel group.
    /// </summary>
    /// <param name="group">Channel group.</param>
    /// <param name="channels">List of channels.</param>
    /// <returns>True if succeeded.</returns>
    bool GetChannels(IChannelGroup group, out IList<IChannel> channels);

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
