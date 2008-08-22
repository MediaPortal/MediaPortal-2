using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.Settings
{

  /// <summary>
  /// Class to save some sample settings, used for debugging.
  /// </summary>
  public class SampleSettings
  {

    #region Variables

    private string _entry;
    private string[] _multiEntry;
    private int[] _multiSelection;
    private string _path;
    private int[] _preferenceList;

    #endregion

    #region Properties

    [Setting(SettingScope.Global, "")]
    public string Entry
    {
      get { return _entry; }
      set { _entry = value; }
    }

    [Setting(SettingScope.Global, new string[] { })]
    public string[] MultiEntry
    {
      get { return _multiEntry; }
      set { _multiEntry = value; }
    }

    [Setting(SettingScope.Global, new int[] { })]
    public int[] MultiSelection
    {
      get { return _multiSelection; }
      set { _multiSelection = value; }
    }

    [Setting(SettingScope.Global, "")]
    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    [Setting(SettingScope.Global, new int[] { })] // irl: set a default order here
    public int[] PreferenceList
    {
      get { return _preferenceList; }
      set { _preferenceList = value; }
    }

    #endregion

  }

}
