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

using System;
using System.Security;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public abstract class BootstrapperApplicationWrapper : BootstrapperApplication, IBootstrapperApp
  {
    protected override void OnStartup(StartupEventArgs args)
    {
      Engine.Log(LogLevel.Debug, $"BootstrapperApplication Startup: Action: {Command.Action}, Display: {Command.Display}, Command Line Args: {string.Join(",", Command.GetCommandLineArgs())}");
      LaunchAction = Command.Action;
      Display = Command.Display;
      CommandLineArguments = Command.GetCommandLineArgs();

      SecureStringVariables = new Variables<SecureString>(Engine.SecureStringVariables);
      NumericVariables = new Variables<long>(Engine.NumericVariables);
      VersionVariables = new Variables<Version>(Engine.VersionVariables);
      StringVariables = new Variables<string>(Engine.StringVariables);

      base.OnStartup(args);
    }

    public LaunchAction LaunchAction { get; protected set; }

    public Display Display { get; protected set; }

    public string[] CommandLineArguments { get; protected set; }

    public IVariables<SecureString> SecureStringVariables { get; private set; }

    public IVariables<long> NumericVariables { get; private set; }

    public IVariables<Version> VersionVariables { get; private set; }

    public IVariables<string> StringVariables { get; private set; }

    public void Detect()
    {
      Engine.Detect();
    }

    public void Plan(LaunchAction action)
    {
      Engine.Plan(action);
    }

    public void Apply(IntPtr hwndParent)
    {
      Engine.Apply(hwndParent);
    }

    public void Log(LogLevel level, string message)
    {
      Engine.Log(level, message);
    }

    public string FormatString(string format)
    {
      return Engine.FormatString(format);
    }

    public bool EvaluateCondition(string condition)
    {
      return Engine.EvaluateCondition(condition);
    }
  }
}
