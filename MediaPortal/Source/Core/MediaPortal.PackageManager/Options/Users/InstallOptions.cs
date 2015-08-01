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
using System.Text;
using CommandLine;
using MediaPortal.PackageManager.Options.Shared;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class InstallOptions : BaseOptions
  {
    private InstallAction[] _actions;
    private Dictionary<string, string> _installPaths;

    [OptionArray('p', "Paths")]
    public string[] InstallPaths { get; set; }

    public IDictionary<string, string> GetInstallPaths()
    {
      if (_installPaths == null)
      {
        _installPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in InstallPaths)
        {
          int n = path.IndexOf(':');
          if (n > 0)
          {
            _installPaths.Add(path.Substring(0, n).Trim(), path.Substring(n + 1).Trim());
          }
        }
      }
      return _installPaths;
    }

    [OptionArray('a', "Actions")]
    public string[] Actions { get; set; }

    public InstallAction[] GetActions()
    {
      if (_actions == null)
      {
        _actions = new InstallAction[Actions.Length];

        for(int n = 0; n < Actions.Length; ++n)
        {
          _actions[n] = new InstallAction(Actions[n]);
        }
      }
      return _actions;
    }

    [Option('r', "Run", HelpText = "Program to run after install actions.")]
    public string RunOnExitProgram { get; set; }

    [OptionArray('s', "SuccessArgs", HelpText = "Arguments to pass to run program after successful installation.")]
    public string[] SuccessArguments { get; set; }

    [OptionArray('f', "FailArgs", HelpText = "Arguments to pass to run program after a failed installation.")]
    public string[] FailArguments { get; set; }

    public string GetSuccessArgsString()
    {
      return BuildArgumentsString(SuccessArguments);
    }

    public string GetFailArgsString()
    {
      return BuildArgumentsString(FailArguments);
    }

    private string BuildArgumentsString(string[] arguments)
    {
      var sb = new StringBuilder();
      foreach (var arg in arguments)
      {
        if (sb.Length > 0)
        {
          sb.Append(' ');
        }
        if (arg.IndexOfAny(new[] { ' ', '/', '\\' }) >= 0)
        {
          sb.Append('\"');
          sb.Append(arg);
          sb.Append('\"');
        }
        else
        {
          sb.Append(arg);
        }
      }
      return sb.ToString();
    }

    #region Overrides of SharedOptions

    public override bool RequiresElevation
    {
      get { return true; }
    }

    #endregion
  }

  internal class InstallAction
  {
    public InstallAction(string action)
    {
      var parts = action.Split('|');
      if (parts.Length < 2 || parts.Length > 4)
      {
        throw new ArgumentException("An action must be defined by 2, 3 or 4 parameters separated by '|'.");
      }
      if (parts[0].Equals("I", StringComparison.OrdinalIgnoreCase))
      {
        ActionType = InstallActionType.Install;
      }
      else if (parts[0].Equals("U", StringComparison.OrdinalIgnoreCase))
      {
        ActionType = InstallActionType.Update;
      }
      else if (parts[0].Equals("R", StringComparison.OrdinalIgnoreCase))
      {
        ActionType = InstallActionType.Remove;
      }
      else
      {
        throw new ArgumentException(String.Format("Unknown action type '{0}'.", parts[0]));
      }
      if (ActionType == InstallActionType.Remove)
      {
        PackageName = parts[1];
      }
      else if (parts.Length == 3)
      {
        LocalPath = parts[1];
        OptionName = parts[2];
      }
      else
      {
        PackageName = parts[1];
        PackageVersion = parts[2];
        OptionName = parts[3];
      }
    }

    public InstallActionType ActionType { get; private set; }

    public bool IsLocalSource
    {
      get { return !String.IsNullOrEmpty(LocalPath); }
    }

    public string LocalPath { get; private set; }

    public string PackageName { get; private set; }

    public string PackageVersion { get; private set; }

    public string OptionName { get; private set; }
  }

  internal enum InstallActionType
  {
    Install,
    Update,
    Remove
  }
}