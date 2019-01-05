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
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlayFile
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string fileType = (string)message["FileType"];
      string filePath = (string)message["Filepath"];
      string id = (string)message["FileId"];
      int startPos = (message["StartPosition"] != null) ? (int)message["StartPosition"] : 0;

      ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play File: FileType: {0}, FilePath: {1}, FileId: {2}, StartPos: {3}", fileType, filePath, id, startPos);

      Guid mediaItemGuid;
      if (Guid.TryParse(filePath, out mediaItemGuid))
        id = mediaItemGuid.ToString();

      if (!Guid.TryParse(id, out mediaItemGuid))
      {
        ServiceRegistration.Get<ILogger>().Info("WifiRemote Play File: Couldn't convert FileId '{0} to Guid", id);
        return false;
      }

      await Helper.PlayMediaItemAsync(mediaItemGuid, startPos);

      return true;
    }
  }
}
