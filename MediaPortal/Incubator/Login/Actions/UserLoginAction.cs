#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Services.UserManagement;
using System;

namespace MediaPortal.UiComponents.Login.Actions
{
  /// <summary>
  /// WorkflowContributorAction that hides the
  /// User Login menu entry if user login is disabled.
  /// </summary>
  public class UserLoginAction : IWorkflowContributor
  {
    #region Consts

    //Id of this model
    public const string USERS_CONTRIBUTOR_MODEL_ID_STR = "D5CA9E6F-8639-4D2F-B94A-FEE7A69A87C4";
    public static readonly Guid USERS_CONTRIBUTOR_MODEL_ID = new Guid(USERS_CONTRIBUTOR_MODEL_ID_STR);

    //Workflow state id of the user login screen
    protected const string USERS_WORKFLOW_STATE_ID_STR = "2529B0F0-8415-4A4E-971B-38D6CDD2406A";
    protected static readonly Guid USERS_WORKFLOW_STATE_ID = new Guid(USERS_WORKFLOW_STATE_ID_STR);

    #endregion
    
    protected readonly IResourceString _displayTitle;
    protected SettingsChangeWatcher<UserSettings> _settingsChangeWatcher;
    protected volatile bool _isActionVisible = false;

    public UserLoginAction()
    {
      _displayTitle = LocalizationHelper.CreateResourceString("[Login.Title]");
    }

    public IResourceString DisplayTitle
    {
      get { return _displayTitle; }
    }

    public event ContributorStateChangeDelegate StateChanged;

    public void Initialize()
    {
      _settingsChangeWatcher = new SettingsChangeWatcher<UserSettings>();
      _isActionVisible = _settingsChangeWatcher.Settings.EnableUserLogin;
      _settingsChangeWatcher.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      Update();
    }

    protected void Update()
    {
      bool enableUserLogin = _settingsChangeWatcher.Settings.EnableUserLogin;
      if (_isActionVisible != enableUserLogin)
      {
        _isActionVisible = enableUserLogin;
        FireStateChanged();
      }
    }

    public void Execute()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(USERS_WORKFLOW_STATE_ID);
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return _isActionVisible;
    }

    public void Uninitialize()
    {
      _settingsChangeWatcher.Dispose();
    }

    protected void FireStateChanged()
    {
      StateChanged?.Invoke();
    }
  }
}
