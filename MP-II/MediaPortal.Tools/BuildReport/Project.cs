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
