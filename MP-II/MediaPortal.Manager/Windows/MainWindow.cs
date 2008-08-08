#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MediaPortal.Core;
using MediaPortal.Core.Localisation;
using MediaPortal.Interfaces.Core.PluginManager;
using MediaPortal.Configuration;

namespace MediaPortal.Manager
{
  public partial class MainWindow : Form
  {
    SettingsControl _settingsArea;

    public MainWindow()
    {
      InitializeComponent();

      // Load plugins
      ServiceScope.Get<IPluginManager>().Startup();

      // localise buttons
      StringId settings = new StringId("configuration", "areas.settings");
      this.areaSettings.Tag = settings;
      this.areaSettings.Text = settings.ToString();

      StringId logs = new StringId("configuration", "areas.logs");
      this.areaLogs.Tag = logs;
      this.areaLogs.Text = logs.ToString();
      this.areaLogs.Enabled = false;

      ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);

      CheckRightToLeft();

      _settingsArea = new SettingsControl();

      _settingsArea.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top 
        | System.Windows.Forms.AnchorStyles.Bottom)
        | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));

      areaControls.Controls.Add(_settingsArea);
    }

    private void LangageChange(object o)
    {
      if (areaSettings.Tag is StringId)
        areaSettings.Text = ((StringId)areaSettings.Tag).ToString();

      if (areaLogs.Tag is StringId)
        areaLogs.Text = ((StringId)areaLogs.Tag).ToString();

      CheckRightToLeft();
    }

    private void CheckRightToLeft()
    {
      if (ServiceScope.Get<ILocalisation>().CurrentCulture.TextInfo.IsRightToLeft)
      {
        this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
        this.RightToLeftLayout = true;
      }
      else
      {
        this.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this.RightToLeftLayout = false;
      }
    }

    private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
      e.Cancel = !_settingsArea.Closing();
    }
  }
}