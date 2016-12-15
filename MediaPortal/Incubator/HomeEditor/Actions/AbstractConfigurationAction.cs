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

using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Localization;
using MediaPortal.Common;

namespace HomeEditor.Actions
{
  public abstract class AbstractConfigurationAction : IWorkflowContributor
  {
    public const string CONFIG_LOCATION_KEY = "ConfigurationModel: CONFIG_LOCATION";
    public static readonly Guid CONFIGURATION_STATE_ID = new Guid("E7422BB8-2779-49ab-BC99-E3F56138061B");

    public abstract IResourceString DisplayTitle
    {
      get;
    }

    protected abstract string ConfigLocation
    {
      get;
    }

    public event ContributorStateChangeDelegate StateChanged;

    public virtual void Execute()
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      wf.NavigatePush(CONFIGURATION_STATE_ID, new NavigationContextConfig()
      {
        NavigationContextDisplayLabel = DisplayTitle.Evaluate(),
        AdditionalContextVariables = new Dictionary<string, object> { { CONFIG_LOCATION_KEY, ConfigLocation } }
      });
    }

    public virtual void Initialize()
    {

    }

    public virtual bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public virtual bool IsActionVisible(NavigationContext context)
    {
      return true;
    }

    public virtual void Uninitialize()
    {

    }
  }
}