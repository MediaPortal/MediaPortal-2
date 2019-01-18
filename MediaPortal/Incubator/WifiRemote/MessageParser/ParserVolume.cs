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

using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.Players;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserVolume : BaseParser
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      int volume = GetMessageValue<int>(message, "Volume");
      bool relative = GetMessageValue<bool>(message, "Relative");
      if (relative)
      {
        volume += ServiceRegistration.Get<IPlayerManager>().Volume;
      }

      if (volume >= 0 && volume <= 100)
      {
        ServiceRegistration.Get<IPlayerManager>().Volume = volume;
      }
      else
      {
        Logger.Warn("WifiRemote Volume: Trying to set wrong Volume level: {0}", volume);
      }
      return Task.FromResult(true);
    }
  }
}
