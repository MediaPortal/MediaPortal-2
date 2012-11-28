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

using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace MediaPortal.Database.MySQL
{
  public class MySQLSettings
  {
    private const int DEFAULT_MAX_PACKETSIZE = 10 * 1024 * 1024;
    public static bool Read(ref string server, ref string username, ref string password, ref string database, ref int maxPacketSize)
    {
      try
      {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MySQLSettings.xml");
        using (XmlReader reader = XmlReader.Create(path))
        {
          reader.ReadToFollowing("MySQLSettings");
          server = reader.GetAttribute("Server");
          username = reader.GetAttribute("UserName");
          password = reader.GetAttribute("Password");
          database = reader.GetAttribute("Database");
          maxPacketSize = int.Parse(reader.GetAttribute("maxPacketSize") ?? DEFAULT_MAX_PACKETSIZE.ToString());
          reader.Close();
          return true;
        }
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
