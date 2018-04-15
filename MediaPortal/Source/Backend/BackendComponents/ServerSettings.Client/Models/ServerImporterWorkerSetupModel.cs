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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Services.MediaManagement;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.ServerSettings.Models
{
  public class ServerImporterWorkerSetupModel : IWorkflowModel
  {
    public const string IMPORTER_SETUP_MODEL_ID_STR = "B3109220-78E4-4ED1-90E1-D3180E02B401";

    protected AbstractProperty _enableAutoRefreshProperty;
    protected AbstractProperty _runAtHourProperty;

    public AbstractProperty EnableAutoRefreshProperty
    {
      get { return _enableAutoRefreshProperty; }
    }

    public bool EnableAutoRefresh
    {
      get { return (bool) _enableAutoRefreshProperty.GetValue(); }
      set { _enableAutoRefreshProperty.SetValue(value); }
    }

    public AbstractProperty RunAtHourProperty
    {
      get { return _runAtHourProperty; }
    }

    public double RunAtHour
    {
      get { return (double) _runAtHourProperty.GetValue(); }
      set { _runAtHourProperty.SetValue(value); }
    }

    public ServerImporterWorkerSetupModel()
    {
      _enableAutoRefreshProperty = new SProperty(typeof(bool), false);
      _runAtHourProperty = new SProperty(typeof(double), 0d);
    }

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      IServerSettingsClient settingsManager = ServiceRegistration.Get<IServerSettingsClient>();
      ImporterWorkerSettings settings = settingsManager.Load<ImporterWorkerSettings>();
      settings.EnableAutoRefresh = EnableAutoRefresh;
      settings.ImporterStartTime = RunAtHour;
      settingsManager.Save(settings);
    }

    private void InitModel()
    {
      IServerSettingsClient settingsManager = ServiceRegistration.Get<IServerSettingsClient>();
      ImporterWorkerSettings settings = settingsManager.Load<ImporterWorkerSettings>();
      EnableAutoRefresh = settings.EnableAutoRefresh;
      RunAtHour = settings.ImporterStartTime;
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(IMPORTER_SETUP_MODEL_ID_STR); }
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
