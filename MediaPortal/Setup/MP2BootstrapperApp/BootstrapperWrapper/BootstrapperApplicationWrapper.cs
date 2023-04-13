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
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public abstract class BootstrapperApplicationWrapper : BootstrapperApplication, IBootstrapperApp
  {
    private const string BurnBundleVersionVariable = "WixBundleVersion";

    public BootstrapperApplicationWrapper(IEngine engine, IBootstrapperCommand command)
      : base(engine)
    {
      Engine.Log(LogLevel.Debug, $"BootstrapperApplication Startup: Action: {command.Action}, Display: {command.Display}, Command Line Args: {command.CommandLine}");
      Command = command;
      LaunchAction = command.Action;
      Display = command.Display;
      CommandLine = command.ParseCommandLine();
      BundleVersion = engine.GetVariableVersion(BurnBundleVersionVariable);
    }

    public IEngine Engine { get { return engine; } }

    public IBootstrapperCommand Command { get; }

    public string BundleVersion { get; }

    public LaunchAction LaunchAction { get; protected set; }

    public Display Display { get; protected set; }

    public IMbaCommand CommandLine { get; protected set; }

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
      return !string.IsNullOrWhiteSpace(format) ? Engine.FormatString(format) : format;
    }

    public bool EvaluateCondition(string condition)
    {
      return Engine.EvaluateCondition(condition);
    }
  }
}
