#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using TraktSettings = MediaPortal.UiComponents.Trakt.Settings.TraktSettings;

namespace MediaPortal.UiComponents.Trakt.Models
{
  public class TraktSetupModel : IWorkflowModel
  {
    #region Consts

    public const string DEFAULT_TEXT = "[Trakt.TestAccount]";
    public const string TRAKT_SETUP_MODEL_ID_STR = "65E4F7CA-3C9C-4538-966D-2A896BFEF4D3";
    public readonly static Guid TRAKT_SETUP_MODEL_ID = new Guid(TRAKT_SETUP_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected readonly AbstractProperty _isEnabledProperty = new WProperty(typeof(bool), false);
    protected readonly AbstractProperty _usermameProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _passwordProperty = new WProperty(typeof(string), null);
    protected readonly AbstractProperty _testStatusProperty = new WProperty(typeof(string), DEFAULT_TEXT);

    #endregion

    #region Public properties - Bindable Data

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool)_isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public AbstractProperty UsernameProperty
    {
      get { return _usermameProperty; }
    }

    public string Username
    {
      get { return (string)_usermameProperty.GetValue(); }
      set { _usermameProperty.SetValue(value); }
    }

    public AbstractProperty PasswordProperty
    {
      get { return _passwordProperty; }
    }

    public string Password
    {
      get { return (string)_passwordProperty.GetValue(); }
      set { _passwordProperty.SetValue(value); }
    }

    public AbstractProperty TestStatusProperty
    {
      get { return _testStatusProperty; }
    }

    public string TestStatus
    {
      get { return (string)_testStatusProperty.GetValue(); }
      set { _testStatusProperty.SetValue(value); }
    }

    #endregion

    #region Public methods - Commands

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      TraktSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();
      settings.EnableTrakt = IsEnabled;
      settings.Authentication = new TraktAuthentication { Username = Username, Password = Password };
      // Save
      settingsManager.Save(settings);
    }

    /// <summary>
    /// Uses the current accound information and tries to validate them at trakt.
    /// </summary>
    public void TestAccount()
    {
      try
      {
        TraktResponse result = TraktAPI.TestAccount(new TraktAccount { Username = Username, Password = Password });
        if (!string.IsNullOrWhiteSpace(result.Error))
          TestStatus = result.Error;
        else if (!string.IsNullOrWhiteSpace(result.Message))
          TestStatus = result.Message;
        else
          TestStatus = DEFAULT_TEXT;
      }
      catch (Exception ex)
      {
        TestStatus = "Error";
        ServiceRegistration.Get<ILogger>().Error("Trakt.tv: Exception while testing account.", ex);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return TRAKT_SETUP_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Load settings
      TraktSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<TraktSettings>();
      IsEnabled = settings.EnableTrakt;
      Username = settings.Authentication != null ? settings.Authentication.Username : null;
      Password = settings.Authentication != null ? settings.Authentication.Password : null;
      TestStatus = DEFAULT_TEXT;
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
