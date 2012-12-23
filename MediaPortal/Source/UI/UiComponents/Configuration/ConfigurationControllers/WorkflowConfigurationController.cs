#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller which shows a configuration with an own workflow.
  /// </summary>
  public class WorkflowConfigurationController : ConfigurationController
  {
    public override void ExecuteConfiguration()
    {
      ConfigSettingMetadata metadata = (ConfigSettingMetadata) _setting.Metadata;
      if (metadata.AdditionalData.ContainsKey("WorkflowState"))
      { // Custom configuration workflow
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePush(new Guid(metadata.AdditionalData["WorkflowState"]));
        // New configuration workflow has to take over the configuration "life cycle" for the
        // current config setting object (which can be accessed in this model via CurrentConfigSetting):
        // - Configure data (providing workflow states and screens for doing that, change the data, ...)
        // - Calling Save and Apply, or discard the setting by not saving it
        // The the sub workflow should step out again to give the control back to this model again.
      }
    }

    public override bool IsSettingSupported(ConfigSetting setting)
    {
      if (!(setting is CustomConfigSetting))
        return false;
      ConfigSettingMetadata metadata = (ConfigSettingMetadata) setting.Metadata;
      return metadata.AdditionalData.ContainsKey("WorkflowState");
    }

    public override Type ConfigSettingType
    {
      get { return typeof(CustomConfigSetting); }
    }
  }
}
