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

using System.Windows.Forms;

using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Configuration;

namespace MediaPortal.Manager
{
  public partial class MainWindow : Form
  {
    #region Enums

    private enum AreaType
    {
      SettingsArea,
      LogArea
    }

    #endregion

    #region Variables

    private SettingsControl _settingsArea;
    private AreaType _areaType;

    #endregion

    #region Constructors

    public MainWindow()
    {
      InitializeComponent();

      // Load plugins
      ServiceScope.Get<IConfigurationManager>().Load();
      // Localise window
      IResourceString settings = LocalizationHelper.CreateLabelProperty("[configuration.areas.settings]");
      areaSettings.Tag = settings;
      areaSettings.Text = settings.ToString();
      IResourceString logs = LocalizationHelper.CreateLabelProperty("[configuration.areas.logs]");
      areaLogs.Tag = logs;
      areaLogs.Text = logs.ToString();
      areaLogs.Enabled = false;
      ServiceScope.Get<ILocalisation>().LanguageChange += LangageChange;
      CheckRightToLeft();
      // Initialize SettingsControl
      _settingsArea = new SettingsControl();
      _settingsArea.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      _areaType = AreaType.SettingsArea;
      areaControls.Controls.Add(_settingsArea);
    }

    #endregion

    #region Private Events

    private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
    {
      e.Cancel = !_settingsArea.Closing();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.F)
      {
        switch (_areaType)
        {
          case AreaType.SettingsArea:
            _settingsArea.FocusSearch();
            break;
          case AreaType.LogArea:
            break;
        }
      }
    }

    #endregion

    #region Private Methods

    private void LangageChange(object o)
    {
      if (areaSettings.Tag is IResourceString)
        areaSettings.Text = ((IResourceString) areaSettings.Tag).Evaluate();

      if (areaLogs.Tag is IResourceString)
        areaLogs.Text = ((IResourceString) areaLogs.Tag).Evaluate();

      CheckRightToLeft();
    }

    private void CheckRightToLeft()
    {
      RightToLeftLayout = ServiceScope.Get<ILocalisation>().CurrentCulture.TextInfo.IsRightToLeft;
      RightToLeft = RightToLeftLayout ? System.Windows.Forms.RightToLeft.Yes : System.Windows.Forms.RightToLeft.No;
    }

    #endregion
  }
}