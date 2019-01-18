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
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPosition : BaseParser
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      int seekType = GetMessageValue<int>(message, "SeekType");
      int position = GetMessageValue<int>(message, "Position");

      Logger.Debug("WifiRemote Position: SeekType: {0}", seekType);

      if (seekType == 0)
      {
        Helper.SetPositionPercent(position, true);
      }
      if (seekType == 1)
      {
        Helper.SetPositionPercent(position, false);
      }
      if (seekType == 2)
      {
        
        Helper.SetPosition(position, true);
      }
      else if (seekType == 3)
      {
        Helper.SetPosition(position, false);
      }

      return Task.FromResult(true);
    }
  }
}
