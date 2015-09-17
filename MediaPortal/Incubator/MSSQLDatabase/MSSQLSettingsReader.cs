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
using System.IO;
using System.Reflection;
using System.Xml;

namespace MediaPortal.Database.MSSQL
{
  /// <summary>
  /// Settings reader class for the MSSQL settings.
  /// </summary>
  /// <remarks>
  /// We're using an own settings reader instead of using the MP2 settings API here by design. Reason is that those database
  /// settings are very elemental for the server start. The XML file makes sense because of two reasons:
  /// <list type="bullet">
  /// <item>With this approach, we can provide a settings file which is already present before the first server start.
  /// That setting file can be filled with the actual DB settings before the server is started the first time.</item>
  /// <item>The MSSQL settings don't need to be changed often. In fact, they will be edited once, at the first install.
  /// At this time, we have admin rights anyway, so it isn't a problem that the settings are located in our plugin directory.</item>
  /// </list>
  /// </remarks>
  public static class MSSQLSettingsReader
  {
    public static void Read(out string server, out string initUsername, out string initPassword, out string username, out string password, out string database)
    {
      try
      {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MSSQLSettings.xml");
        using (XmlReader reader = XmlReader.Create(path))
        {
          reader.ReadToFollowing("MSSQLSettings");
          server = reader.GetAttribute("Server");
          initUsername = reader.GetAttribute("InitUserName");
          initPassword = reader.GetAttribute("InitPassword");
          username = reader.GetAttribute("UserName");
          password = reader.GetAttribute("Password");
          database = reader.GetAttribute("Database");
          reader.Close();
        }
      }
      catch (Exception e)
      {
        throw new ApplicationException("Cannot read database connection settings from MSSQLSettings.xml!", e);
      }
    }
  }
}
