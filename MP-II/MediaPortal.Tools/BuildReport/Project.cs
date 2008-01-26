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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Tools.BuildReport
{
  class Project : IComparable<Project>
  {
    public enum CssClass
    {
      success,
      warning,
      error,
      question
    }

    public string name = string.Empty;
    public string filename = string.Empty;
    public Solution.Compile build = Solution.Compile.Skipped;
    public int errors = 0;
    public int warnings = 0;

    public CssClass Type
    {
      get
      {
        if (errors > 0)
          return CssClass.error;

        if (warnings > 0)
          return CssClass.warning;

        if (build == Solution.Compile.Skipped)
          return CssClass.question;

        return CssClass.success;
      }
    }

    #region IComparer<ListItem> Members

    public int CompareTo(Project x)
    {
      return string.Compare(this.name, x.name);
    }

    #endregion
  }
}
