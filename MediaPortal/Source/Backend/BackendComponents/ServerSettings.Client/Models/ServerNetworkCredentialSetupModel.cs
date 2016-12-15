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
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Plugins.ServerSettings.Models
{
  public class ServerNetworkCredentialSetupModel : IWorkflowModel
  {
    public const string NETWORK_CREDENTIAL_SETUP_MODEL_ID_STR = "62BFBA02-88F8-41A9-BD5A-FFD98799089B";

    protected AbstractProperty _useCredentialsProperty;
    protected AbstractProperty _networkUserNameProperty;
    protected AbstractProperty _networkPasswordProperty;

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
      _useCredentialsProperty = new SProperty(typeof (bool), false);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings ()
    {
      var settings = ServiceRegistration.Get<IServerSettingsClient>().Load<NetworkNeighborhoodResourceProviderSettings>();
      settings.NetworkCredentials.Clear();
      if (UseCredentials)
      {
        // We currently only store one credential for the root path of the NetworkNeighborhoodResourceProvider.
        // ToDo: Modify the frontend to support multiple credentials for different ResourcePaths
        settings.NetworkCredentials.Add(NetworkNeighborhoodResourceProvider.RootPath.ToString(), new SerializableNetworkCredential
        {
          UserName = NetworkUserName,
          // It is not recommended to use a string for storing cleartext passwords. This is 
          // (besides InitModel below) the only place in the code base where we have to
          // access the Password property instead of the SecurePassword property.
          Password = NetworkPassword
        });
      }
      // We currently store the settings on the server and locally on the client.
      // ToDo: Make these settings SystemSettings, once they are implemented.
      ServiceRegistration.Get<IServerSettingsClient>().Save(settings);
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    private void InitModel ()
    {
      var settings = ServiceRegistration.Get<IServerSettingsClient>().Load<NetworkNeighborhoodResourceProviderSettings>();
      if (settings.NetworkCredentials.Any())
      {
        UseCredentials = true;
        NetworkUserName = settings.NetworkCredentials.First().Value.UserName;
        // It is not recommended to use a string for storing cleartext passwords. This is 
        // (besides SaveSettings above) the only place in the code base where we have to
        // access the Password property instead of the SecurePassword property.
        // ToDo: We need a TextBox that can handle SecureString
        NetworkPassword = settings.NetworkCredentials.First().Value.Password;
      }
      else
      {
        UseCredentials = false;
        NetworkUserName = String.Empty;
        NetworkPassword = String.Empty;
      }
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
