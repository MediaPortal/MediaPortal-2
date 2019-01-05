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

using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPosition
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      int seekType = (int)message["SeekType"];

      Logger.Debug("WifiRemote Position: SeekType: {0}", seekType);

      if (seekType == 0)
      {
        int position = (int)message["Position"];
        Helper.SetPositionPercent(position, true);
      }
      if (seekType == 1)
      {
        int position = (int)message["Position"];
        Helper.SetPositionPercent(position, false);
      }
      if (seekType == 2)
      {
        int position = (int)message["Position"];
        Helper.SetPosition(position, true);
      }
      else if (seekType == 3)
      {
        int position = (int)message["Position"];
        Helper.SetPosition(position, false);
      }

      return Task.FromResult(true);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
