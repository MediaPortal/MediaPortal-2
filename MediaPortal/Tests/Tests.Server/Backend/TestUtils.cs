#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Mock;

namespace Tests.Server.Backend
{
  public class TestBackendUtils
  {
    public static SingleTestMIA CreateSingleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      SingleTestMIA mia = TestCommonUtils.CreateSingleMIA(table, cardinality, createStringAttribute, createIntegerAttribute);

      MockCore.AddMediaItemAspectStorage(mia.Metadata);

      return mia;
    }

    public static MultipleTestMIA CreateMultipleMIA(string table, Cardinality cardinality, bool createStringAttribute, bool createIntegerAttribute)
    {
      MultipleTestMIA mia = TestCommonUtils.CreateMultipleMIA(table, cardinality, createStringAttribute, createIntegerAttribute);

      MockCore.AddMediaItemAspectStorage(mia.Metadata);

      return mia;
    }
  }
}
