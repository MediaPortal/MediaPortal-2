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
using System.Collections.Generic;
using System.Data;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities.DB;

namespace MediaPortal.Mock
{
  public class MockDBUtils : DBUtils
  {
    private static readonly MockDatabase DATABASE = new MockDatabase();
    private static readonly IDictionary<string, MockReader> READERS = new Dictionary<string, MockReader>();
    private static IList<MockCommand> COMMANDS = new List<MockCommand>();
    private static MockReader ALIASES_READER = null;

    static MockDBUtils()
    {
      // Override mappings to make output log more readable
      SIMPLEDOTNETTYPE2DBTYPE[typeof(DateTime)] = DbType.String;
      SIMPLEDOTNETTYPE2DBTYPE[typeof(Guid)] = DbType.String;

      ServiceRegistration.Set<ISQLDatabase>(DATABASE);

      ALIASES_READER = AddReader("SELECT MIAM_ID, IDENTIFIER, DATABASE_OBJECT_NAME FROM MIA_NAME_ALIASES");
      /*
          reader.AddResult("", GetMIATableIdentifier(MediaAspect.Metadata), "M_MEDIAITEM");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(MediaAspect.ATTR_TITLE), "TITLE");
          reader.AddResult("", GetMIATableIdentifier(AudioAspect.Metadata), "M_AUDIOITEM");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(AudioAspect.ATTR_ALBUM), "ALBUM");
          reader.AddResult("", GetMIATableIdentifier(RelationshipAspect.Metadata), "M_RELATIONSHIP");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_ROLE), "ROLE");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_LINKED_ID), "LINKEDID");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_LINKED_ROLE), "LINKEDROLE");
          */

      AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION FROM MIA_TYPES");
      AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION, CREATION_DATE FROM MIA_TYPES");
      AddReader("SELECT MIAM_ID, CREATION_DATE FROM MIA_TYPES");
    }

    public static DbType GetType(Type type)
    {
      return SIMPLEDOTNETTYPE2DBTYPE[type];
    }

    public static MockReader GetAliasesReader()
    {
      return ALIASES_READER;
    }

    public static void Reset()
    {
      COMMANDS.Clear();
    }

    public static MockDatabase Database
    {
      get { return DATABASE; }
    }

    public static MockReader AddReader(string command, params string[] columns)
    {
      return AddReader(command, new MockReader(columns));
    }

    public static MockReader AddReader(string command, MockReader reader)
    {
      READERS[command] = reader;
      return reader;
    }

    public static IDataReader GetReader(string command)
    {
      MockReader reader = null;
      if (!READERS.TryGetValue(command, out reader))
      {
        foreach (string key in READERS.Keys)
        {
        }
        throw new NotImplementedException("No DB reader for " + command);
      }
      return reader;
    }

    public static string GetMIATableIdentifier(MediaItemAspectMetadata miam)
    {
      return "T_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString()).ToUpperInvariant();
    }

    public static string GetMIAAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return "A_" + SqlUtils.ToSQLIdentifier(spec.ParentMIAM.AspectId + "_" + spec.AttributeName).ToUpperInvariant();
    }

    public static void AddCommand(MockCommand command)
    {
      COMMANDS.Add(command);
    }

    public static MockCommand FindCommand(string match)
    {
      foreach (MockCommand command in COMMANDS)
      {
        if (command.CommandText.StartsWith(match))
        {
          return command;
        }
      }

      return null;
    }
  }
}