using System;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using CommandLine;

namespace MediaPortal.PackageManager.Options.Users
{
  internal class InstallOptions : SharedOptions
  {
    private InstallAction[] _actions;

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
  }

  internal class InstallAction
  {
    public InstallAction(string action)
    {
      var parts = action.Split('|');
      if (parts.Length < 2 || parts.Length > 3)
      {
        throw new ArgumentException("An action must be defined by 3 parameters separated by '|'.");
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
      else if (parts.Length == 2)
      {
        LocalPath = parts[1];
      }
      else
      {
        PackageName = parts[1];
        PackageVersion = parts[2];
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
  }

  internal enum InstallActionType
  {
    Install,
    Update,
    Remove
  }
}