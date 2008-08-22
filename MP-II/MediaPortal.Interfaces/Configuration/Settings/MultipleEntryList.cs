using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Configuration.Settings
{
  public class MultipleEntryList : ConfigBase
  {

    #region Variables

    /// <summary>
    /// The content of the MultipleEntryList.
    /// </summary>
    protected IList<string> _lines = new List<string>();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the lines.
    /// </summary>
    public IList<string> Lines
    {
      get { return this._lines; }
      set { this._lines = value; }
    }

    #endregion

  }
}
