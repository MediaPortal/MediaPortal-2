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

using System;
using System.Data;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Mock
{
  class MockTransaction : ITransaction
  {
    private MockDatabase _database;

    public MockTransaction(MockDatabase database)
    {
      this._database = database;
    }

    public ISQLDatabase Database
    {
      get { return _database; }
    }

    public IDbConnection Connection
    {
      get { throw new NotImplementedException(); }
    }

    public void Begin(IsolationLevel level)
    {
      ServiceRegistration.Get<ILogger>().Info("Beginning");
    }

    public void Commit()
    {
      ServiceRegistration.Get<ILogger>().Info("Committing");
    }

    public void Rollback()
    {
      ServiceRegistration.Get<ILogger>().Info("Rolling back");
    }

    public IDbCommand CreateCommand()
    {
      return _database.CreateCommand();
    }

    public void Dispose()
    {
    }
  }
}
