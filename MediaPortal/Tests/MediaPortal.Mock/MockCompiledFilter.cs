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

using System.Collections.Generic;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Mock
{
  public class MockCompiledFilter : CompiledFilter
  {
    public MockCompiledFilter()
      : base(null, null, null, null, null, null)
    {
    }

    public void test(MIA_Management miaManagement, IFilter filter,
      ICollection<MediaItemAspectMetadata> requiredMIATypes, string outerMIIDJoinVariable, ICollection<TableJoin> tableJoins,
      IList<object> resultParts, IList<BindVar> resultBindVars)
    {
      CompileStatementParts(miaManagement, filter, null, new BindVarNamespace(),
        requiredMIATypes, outerMIIDJoinVariable, tableJoins,
        resultParts, resultBindVars);
    }
  }
}