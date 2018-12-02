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

using System;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  /// <summary>
  /// MediaType of a channel.
  /// </summary>
  public enum MediaType
  {
    TV,
    Radio
  }

  /// <summary>
  /// IChannel represents a channel.
  /// </summary>
  public interface IChannel
  {
    /// <summary>
    /// Gets or Sets the Channel ID.
    /// </summary>
    int ChannelId { get; set; }

    /// <summary>
    /// Gets or Sets the Channel number. This can be either a logical channel number or 
    /// the index inside the current group (depending on user choice for zapping).
    /// </summary>
    int ChannelNumber { get; set; }

    /// <summary>
    /// Gets or Sets the Name.
    /// </summary>
    String Name { get; set; }

    /// <summary>
    /// Gets or Sets the MediaType.
    /// </summary>
    MediaType MediaType { get; set; }
  }

  public static class ChannelExtensions
  {
    /// <summary>
    /// Returns a matching fanart MediaType that will be used to get channel logos. If a <c>null</c> value is given, the result defaults to "TV".
    /// </summary>
    /// <param name="channel">Channel</param>
    /// <returns>Fanart MediaType</returns>
    public static string GetFanArtMediaType(this IChannel channel)
    {
      return channel != null && channel.MediaType == MediaType.Radio ? "ChannelRadio" : "ChannelTv";
    }
  }
}
