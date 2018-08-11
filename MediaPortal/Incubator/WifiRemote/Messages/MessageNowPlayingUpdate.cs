#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  /// <summary>
  /// Message that is sent to the client in regular updates as when Media is
  /// being played on the htpc
  /// </summary>
  internal class MessageNowPlayingUpdate : MessageNowPlayingBase, IMessage
  {
    public String Type
    {
      get { return "nowplayingupdate"; }
    }


    /// <summary>
    /// Current speed of the player
    /// </summary>
    public int Speed
    {
      get
      {
        try
        {
          return Convert.ToInt32(ServiceRegistration.Get<IMediaPlaybackControl>().PlaybackRate);
        }
        catch (Exception)
        {
          return 1;
        }
      }
    }
  }
}
