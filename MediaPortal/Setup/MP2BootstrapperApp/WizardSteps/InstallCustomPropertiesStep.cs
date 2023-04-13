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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.Utils;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Custom install step that sets optional installation properties like install directory and whether to install shortcuts.
  /// </summary>
  public class InstallCustomPropertiesStep : AbstractInstallStep, IStep
  {
    const string INSTALLDIR_VARIABLE = "INSTALLDIR";
    const string CREATEDESKTOPSHORTCUTS_VARIABLE = "CREATEDESKTOPSHORTCUTS";
    const string CREATESTARTMENUSHORTCUTS_VARIABLE = "CREATESTARTMENUSHORTCUTS";

    protected IPlan _actionPlan;

    public InstallCustomPropertiesStep(IBootstrapperApplicationModel bootstrapperApplicationModel, IPlan actionPlan)
      : base(bootstrapperApplicationModel)
    {
      _actionPlan = actionPlan;
      InstallDirectory = GetVariable(INSTALLDIR_VARIABLE);

      // Default create shortcuts to true if variable is empty, else false if variable not equal to 1
      string createDesktopShortcuts = GetVariable(CREATEDESKTOPSHORTCUTS_VARIABLE);      
      CreateDesktopShortcuts = string.IsNullOrWhiteSpace(createDesktopShortcuts) || createDesktopShortcuts == "1";
      string createStartMenuShortcuts = GetVariable(CREATESTARTMENUSHORTCUTS_VARIABLE);
      CreateStartMenuShortcuts = string.IsNullOrWhiteSpace(createStartMenuShortcuts) || createStartMenuShortcuts == "1";
    }

    /// <summary>
    /// Gets or sets the installation directory.
    /// </summary>
    public string InstallDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to create desktop shortcuts.
    /// </summary>
    public bool CreateDesktopShortcuts { get; set; }

    /// <summary>
    /// Gets or sets whether to create start menu shortcuts.
    /// </summary>
    public bool CreateStartMenuShortcuts { get; set; }

    public bool CanGoBack()
    {
      return true;
    }

    public bool CanGoNext()
    {
      return true;
    }

    public IStep Next()
    {
      if (!IsValidInstallDirectory(InstallDirectory))
        return null;

      _actionPlan.SetVariable(INSTALLDIR_VARIABLE, InstallDirectory);
      _actionPlan.SetVariable(CREATEDESKTOPSHORTCUTS_VARIABLE, CreateDesktopShortcuts ? "1" : "0");
      _actionPlan.SetVariable(CREATESTARTMENUSHORTCUTS_VARIABLE, CreateStartMenuShortcuts ? "1" : "0");
      return new InstallOverviewStep(_bootstrapperApplicationModel, _actionPlan);
    }

    public bool IsValidInstallDirectory(string path)
    {
      return PathUtils.IsValidAbsoluteDirectoryPath(path);
    }

    protected string GetVariable(string variableName)
    {
      IBootstrapperApp bootstrapperApplication = _bootstrapperApplicationModel.BootstrapperApplication;
      if (!bootstrapperApplication.Engine.ContainsVariable(variableName))
        return null;
      string variable = bootstrapperApplication.Engine.GetVariableString(variableName);
      return bootstrapperApplication.FormatString(variable);
    }
  }
}
