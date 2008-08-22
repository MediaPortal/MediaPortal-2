using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.Settings
{

  /// <summary>
  /// Class to save all systemsettings (registry, start menu, ...),
  /// so the user can migrate his configuration files to another system.
  /// </summary>
  class SystemSettings
  {

    #region Variables

    private bool _autostart;
    private bool _balloontips;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether MediaPortal should autostart on Windows startup.
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool Autostart
    {
      get { return _autostart; }
      set { _autostart = value; }
    }


    /// <summary>
    /// Gets or sets whether tray area's balloontips are enabled.
    /// (for all applications)
    /// </summary>
    [Setting(SettingScope.User, true)]
    public bool Balloontips
    {
      get { return _balloontips; }
      set { _balloontips = value; }
    }

    [Setting(SettingScope.User, new int[] { 2, 1 })]
    public int[] TestArray
    {
      get { return new int[] { 1, 2 }; }
      //get { return new int[0]; }
      set { }
    }

    #endregion

  }

}
