#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MP2BootstrapperApp.BundlePackages;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Implementation of <see cref="IPlan"/> that plans a specified action and does not override the default requested state for packages.
  /// </summary>
  public class SimplePlan : IPlan
  {
    protected LaunchAction _plannedAction;
    protected IDictionary<string, string> _variables;

    /// <summary>
    /// Creates a new instance of <see cref="SimplePlan"/>.
    /// </summary>
    /// <param name="plannedAction">The action to plan.</param>
    public SimplePlan(LaunchAction plannedAction)
    {
      _plannedAction = plannedAction;
      _variables = new Dictionary<string, string>();
    }

    public virtual LaunchAction PlannedAction
    {
      get { return _plannedAction; }
    }

    public virtual RequestState? GetRequestedInstallState(IBundlePackage package)
    {
      return null;
    }

    public virtual FeatureState? GetRequestedInstallState(IBundlePackageFeature feature)
    {
      return null;
    }

    public virtual void SetVariable(string name, string value)
    {
      _variables[name] = value;
    }

    public virtual IEnumerable<KeyValuePair<string, string>> GetVariables()
    {
      return _variables.ToArray();
    }
  }
}
