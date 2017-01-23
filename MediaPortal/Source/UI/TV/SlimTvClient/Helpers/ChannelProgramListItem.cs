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

using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Holds a GUI item for the MultiChannel program guide, that contains the channel name and an ItemList
  /// with the Programs.
  /// </summary>
  public class ChannelProgramListItem : ListItem
  {
    public ItemsList Programs { get; set; }
    public IChannel Channel { get; set; }
    public string ChannelLogoPath { get; set; }
    public ChannelProgramListItem(IChannel channel, ItemsList programs)
    {
      SetLabel(Consts.KEY_NAME, channel.Name);
      Programs = programs;
      Channel = channel;
      //ChannelLogoPath = string.Format("channellogos\\{0}.png", channel.Name);
      ChannelLogoPath = channel.Name;
    }
  }
}