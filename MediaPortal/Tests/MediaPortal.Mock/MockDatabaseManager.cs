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
using System.Collections.Generic;
using MediaPortal.Backend.Database;

namespace MediaPortal.Mock
{
  public class MockDatabaseManager : IDatabaseManager
  {
    public const string DUMMY_TABLE_NAME = "DUMMY";

    public string DummyTableName
    {
      get { return DUMMY_TABLE_NAME; }
    }

    public void Startup()
    {
      throw new NotImplementedException();
    }

    public ICollection<string> GetDatabaseSubSchemas()
    {
      throw new NotImplementedException();
    }

    public bool GetSubSchemaVersion(string subSchemaName, out int versionMajor, out int versionMinor)
    {
      throw new NotImplementedException();
    }

    public bool UpdateSubSchema(string subSchemaName, int? currentVersionMajor, int? currentVersionMinor, string updateScriptFilePath, int newVersionMajor, int newVersionMinor)
    {
      throw new NotImplementedException();
    }

    public void DeleteSubSchema(string subSchemaName, int currentVersionMajor, int currentVersionMinor, string deleteScriptFilePath)
    {
      throw new NotImplementedException();
    }

    public void ExecuteBatch(ISQLDatabase database, Utilities.DB.InstructionList instructions)
    {
      throw new NotImplementedException();
    }
  }
}
