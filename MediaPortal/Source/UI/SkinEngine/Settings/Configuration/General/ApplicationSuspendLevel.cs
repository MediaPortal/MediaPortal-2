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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.General
{
  public class ApplicationSuspendLevel : SingleSelectionList
  {
    protected const string RES_SUSPEND_LEVEL_PREFIX = "Settings.General.System.ApplicationSuspendLevel";

    protected IList<SuspendLevel> _suspendLevels = new List<SuspendLevel>();

    #region Base overrides

    public override void Load()
    {
      _suspendLevels = Enum.GetValues(typeof(SuspendLevel)).Cast<SuspendLevel>().ToList();
      SuspendLevel selectedSuspendLevel = SettingsManager.Load<AppSettings>().SuspendLevel;
      Selected = _suspendLevels.IndexOf(selectedSuspendLevel);

      // Fill items
      _items = _suspendLevels.Select(level => LocalizationHelper.CreateResourceString(
          '[' + RES_SUSPEND_LEVEL_PREFIX + '.' + level.ToString() + ']')).ToList();
    }

    public override void Save()
    {
      IScreenControl sc = ServiceRegistration.Get<IScreenControl>();
      int selected = Selected;
      sc.ApplicationSuspendLevel = selected > -1 && selected < _suspendLevels.Count ? _suspendLevels[selected] : SuspendLevel.None;
    }

    #endregion
  }
}
