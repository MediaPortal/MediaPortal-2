#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Settings;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.AppLauncher.Models
{
  public class BaseAppLauncherMenuAction : IWorkflowContributor
  {
    protected static Guid WF_HOME_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");

    private string _actionId;
    private int _appNumber;
    private string _appId;
    private string _appName;
    private SettingsChangeWatcher<Apps> _appChanges;

    public BaseAppLauncherMenuAction(string actionId, int appNumber)
    {
      _actionId = actionId;
      _appNumber = appNumber;

      _appChanges = new SettingsChangeWatcher<Apps>(true);
      _appChanges.SettingsChanged += SettingsChanged;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      Update();
      FireStateChanged();
    }

    private void Update()
    {
      var apps = Helper.LoadApps(false);
      var app = apps?.AppsList.FirstOrDefault(a => a.MenuNumber == _appNumber);
      _appId = app?.Id.ToString();
      _appName = app?.ShortName;
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateStaticString($"{LocalizationHelper.Translate(Consts.RES_MENU_ENTRY)} {_appNumber.ToString()}{(_appName == null ? "" : " (" + _appName + ")")}"); }
    }

    public void Initialize()
    {
      Update();
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return context?.WorkflowState?.StateId == WF_HOME_ID && _appId != null;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return _appId != null;
    }

    protected void FireStateChanged()
    {
      ContributorStateChangeDelegate d = StateChanged;
      if (d != null) d();
    }

    public void Execute()
    {
      var workflowManager = ServiceRegistration.Get<IWorkflowManager>(false);
      AppLauncherHomeModel model = workflowManager?.GetModel(AppLauncherHomeModel.APP_HOME_ID) as AppLauncherHomeModel;
      if (model != null)
        model.StartApp(_appId);
    }

    #endregion
  }
}
