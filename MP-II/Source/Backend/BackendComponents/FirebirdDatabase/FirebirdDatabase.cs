#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Data;

namespace MediaPortal.Database.FirebirdDatabase
{
TODO: Rename to FirebirdSQLDatabase
  public class FirebirdDatabase : ISQLDatabase
  {
    protected FbConnection _connection;

    public FirebirdDSQLDatabase(FbConnection connection)
    {
      _connection = connection;
    }

    public void Dispose()
    {
      _connection.Rollback();
    }

    #region ISQLDatabase implementation

    public void DDLStatement(string statement)
    {
      asdf
    }

    public void UpdateStatement(string statement)
    {
      asdf
    }

    public IDataReader SelectStatement(string statement)
    {
      asdf
    }

    #endregion
  }
}
