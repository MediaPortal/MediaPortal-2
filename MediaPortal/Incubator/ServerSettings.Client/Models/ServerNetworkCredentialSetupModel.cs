#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.ServerSettings.Models
{
  public class ServerNetworkCredentialSetupModel : IWorkflowModel
  {
    public const string NETWORK_CREDENTIAL_SETUP_MODEL_ID_STR = "62BFBA02-88F8-41A9-BD5A-FFD98799089B";

    protected AbstractProperty _impersonateInteractiveProperty;
    protected AbstractProperty _useCredentialsProperty;
    protected AbstractProperty _networkUserNameProperty;
    protected AbstractProperty _networkPasswordProperty;

    public AbstractProperty ImpersonateInteractiveProperty
    {
      get { return _impersonateInteractiveProperty; }
    }

    public bool ImpersonateInteractive
    {
      get { return (bool) _impersonateInteractiveProperty.GetValue(); }
      set { _impersonateInteractiveProperty.SetValue(value); }
    }

    public AbstractProperty UseCredentialsProperty
    {
      get { return _useCredentialsProperty; }
    }

    public bool UseCredentials
    {
      get { return (bool) _useCredentialsProperty.GetValue(); }
      set { _useCredentialsProperty.SetValue(value); }
    }

    public AbstractProperty NetworkUserNameProperty
    {
      get { return _networkUserNameProperty; }
    }

    public string NetworkUserName
    {
      get { return (string) _networkUserNameProperty.GetValue(); }
      set { _networkUserNameProperty.SetValue(value); }
    }

    public AbstractProperty NetworkPasswordProperty
    {
      get { return _networkPasswordProperty; }
    }

    public string NetworkPassword
    {
      get { return (string) _networkPasswordProperty.GetValue(); }
      set { _networkPasswordProperty.SetValue(value); }
    }

    public ServerNetworkCredentialSetupModel ()
    {
      _networkPasswordProperty = new SProperty(typeof (string), string.Empty);
      _networkUserNameProperty = new SProperty(typeof (string), string.Empty);
      _impersonateInteractiveProperty = new SProperty(typeof (bool), false);
      _useCredentialsProperty = new SProperty(typeof (bool), false);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings ()
    {
      IServerSettingsClient settingsManager = ServiceRegistration.Get<IServerSettingsClient>();
      NetworkNeighborhoodResourceProviderSettings settings = settingsManager.Load<NetworkNeighborhoodResourceProviderSettings>();
      settings.ImpersonateInteractive = ImpersonateInteractive;
      settings.UseCredentials = UseCredentials;
      settings.NetworkUserName = NetworkUserName;
      settings.NetworkPassword = NetworkPassword;
      settingsManager.Save(settings);
    }

    private void InitModel ()
    {
      IServerSettingsClient settingsManager = ServiceRegistration.Get<IServerSettingsClient>();
      NetworkNeighborhoodResourceProviderSettings settings = settingsManager.Load<NetworkNeighborhoodResourceProviderSettings>();
      ImpersonateInteractive = settings.ImpersonateInteractive;
      UseCredentials = settings.UseCredentials;
      NetworkUserName = settings.NetworkUserName;
      NetworkPassword = settings.NetworkPassword;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(NETWORK_CREDENTIAL_SETUP_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
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
