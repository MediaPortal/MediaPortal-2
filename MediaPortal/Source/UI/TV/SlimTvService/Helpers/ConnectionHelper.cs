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

using System.Data;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;

namespace MediaPortal.Plugins.SlimTv.Service.Helpers
{
  static class ConnectionExtension
  {
    static readonly Regex REGEX_SQLITE_REPLACE = new Regex(@"(\/)([^\/]*)(.s3db)");

    public static DbConnection GetClone(this IDbConnection connection, string replaceDatabase = null)
    {
      DbConnection connClone = null;
      DbProviderFactory factory;
      string cloneConnectString;
      if (GetCloneFactory(connection, replaceDatabase, out factory, out cloneConnectString))
      {
        connClone = factory.CreateConnection();
        if (connClone == null)
          return null;

        connClone.ConnectionString = cloneConnectString;
      }
      return connClone;
    }

    public static bool GetCloneFactory(this IDbConnection connection, string replaceDatabase, out DbProviderFactory factory, out string cloneConnectString)
    {
      factory = connection.GetDbFactory();
      cloneConnectString = connection.ConnectionString;
      if (factory == null)
        return false;

      if (!string.IsNullOrEmpty(replaceDatabase))
      {
        var csb = factory.CreateConnectionStringBuilder();
        if (csb != null)
        {
          csb.ConnectionString = connection.ConnectionString;
          if (csb.ContainsKey("Data Source"))
          {
            // SQL-Server, keep Data Source, but change initial catalog
            if (csb.ContainsKey("Initial Catalog"))
              csb["Initial Catalog"] = replaceDatabase;
            else
            {
              // SQL CE
              string directory = Path.GetDirectoryName(csb["Data Source"].ToString());
              csb["Data Source"] = Path.Combine(directory, replaceDatabase + ".sdf");
            }
          }
          if (csb.ContainsKey("database"))
          {
            csb["database"] = replaceDatabase;
          }
          // SQLite
          if (csb.ContainsKey("fulluri"))
          {
            // Typical SQLite connection string using URI format:
            // fulluri="file:///C:/ProgramData/Team%20MediaPortal/MP2-Server/Database/Datastore.s3db?cache=shared";version=3;binaryguid=True;default timeout=30000;cache size=65536;journal mode=Wal;pooling=False;synchronous=Normal;foreign keys=True
            csb["fulluri"] = REGEX_SQLITE_REPLACE.Replace(csb["fulluri"].ToString(), "$1" + replaceDatabase + "$3");
          }
          cloneConnectString = csb.ConnectionString;
        }
      }
      return true;
    }

    /// <summary>
    /// Extension method to retrieve the <see cref="DbProviderFactory"/> from an existing <see cref="DbConnection"/>.
    /// </summary>
    /// <param name="existingConnection">The DbConnection to get the DBProviderFactory from.</param>
    /// <returns>DbProviderFactory Property.</returns>
    /// <remarks></remarks>
    public static DbProviderFactory GetDbFactory(this IDbConnection existingConnection)
    {
      DbConnection dbConnection = existingConnection as DbConnection;
      return dbConnection == null ? null : DbProviderFactories.GetFactory(dbConnection);
    }
  }
}
