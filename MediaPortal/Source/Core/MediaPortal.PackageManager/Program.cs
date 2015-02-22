#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using CommandLine;
using MediaPortal.Common.Logging;
using MediaPortal.PackageManager.Core;
using MediaPortal.PackageManager.Options;
using MediaPortal.PackageManager.Options.Shared;
using MediaPortal.PackageManager.Options.Users;

namespace MediaPortal.PackageManager
{
  public class Program
  {
    private static readonly ILogger LOG = new BasicConsoleLogger(LogLevel.All);
    private static string[] _args;

    public static void Main(string[] args)
    {
      // Exit codes:
      // 0 = OK
      // 1 = Parameter error
      // 2 = General exception
      // 3 = IO exception
      try
      {
        // remember args if we need to run elevated
        _args = args;

        var options = new CommandLineOptions();
        var parser = new Parser(with => with.HelpWriter = Console.Out);
        parser.ParseArgumentsStrict(args, options, Dispatch, () => Environment.ExitCode = 1);
      }
      catch (IOException ex)
      {
        LOG.Error(ex.Message);
        Environment.ExitCode = 3;
      }
      catch (Exception ex)
      {
        LOG.Error(ex.Message);
        Environment.ExitCode = 2;
      }
#if DEBUG
      Thread.Sleep(2000);
#endif
    }

    private static void Dispatch(string verb, object options)
    {
      if (string.IsNullOrEmpty(verb) || options == null)
      {
        Environment.ExitCode = 1;
        return; // invalid verb or no options
      }

      Operation operation;
      if (!Enum.TryParse(verb.Replace("-", ""), true, out operation))
      {
        Environment.ExitCode = 1;
        return; // unknown operation
      }

      switch (operation)
      {
        case Operation.CreateUser:
        case Operation.RevokeUser:
          PackageAdminCmd.Dispatch(LOG, operation, options);
          return;
        
        case Operation.Create:
          PackageBuilderCmd.Dispatch(LOG, operation, options);
          return;
        
        case Operation.Publish:
        case Operation.Recall:
          PackagePublisherCmd.Dispatch(LOG, operation, options);
          return;

        default:
          var sharedOptions = options as InstallOptions;
          if (operation != Operation.List && sharedOptions != null && !sharedOptions.IsElevated)
          {
            try
            {
              RunElevated();
              RunOnExit(sharedOptions, Environment.ExitCode == 0);
            }
            catch
            {
              RunOnExit(sharedOptions, false);
              throw;
            }
            return;
          }
          // when we are elevated, we do not run the "run on exit" program, since this is done by the outer instance of this tool!
          PackageInstallerCmd.Dispatch(LOG, operation, options);
          return;
      }
    }

    private static void RunOnExit(InstallOptions options, bool success)
    {
      if (!String.IsNullOrEmpty(options.RunOnExitProgram))
      {
        var process = new Process()
        {
          StartInfo =
          {
            FileName = options.RunOnExitProgram,
            Arguments = success ? options.GetSuccessArgsString() : options.GetFailArgsString()
          }
        };
        process.Start();
      }
    }

    private static void RunElevated()
    {
#if DEBUG
      // wait a bit so the server pipe is ready
      Thread.Sleep(500);
#endif
      // contact windows service via named pipe
      var pipe = new NamedPipeClientStream("MediaPortal.PackageService");
      pipe.Connect();

      // rebuild arguments string
      var argsString = new StringBuilder();
      foreach (var arg in _args)
      {
        if (arg.IndexOfAny(new[] { ' ', '\\' }) >= 0)
        {
          // quoted
          argsString.Append('\"');
          argsString.Append(arg);
          argsString.Append("\" ");
        }
        else
        {
          argsString.Append(arg);
          argsString.Append(' ');
        }
      }
      argsString.Append("--elevated");

      // send command
      // file name in 1st line, arguments in 2nd line
      var data = Encoding.UTF8.GetBytes(String.Concat(
        typeof(Program).Assembly.Location, "\n",
        argsString.ToString()));
      pipe.Write(data, 0, data.Length);

      // poll data
      // it's always 4 byte length + data
      // for exit code, the length is 0x7fff followed by 4 byte exit code
      var readBuffer = new byte[1024];
      do
      {
        int length = pipe.Read(readBuffer, 0, 4);
        while (length < 4)
        {
          length += pipe.Read(readBuffer, length, 4 - length);
        }
        int dataLength = BitConverter.ToInt32(readBuffer, 0);
        if (dataLength == 0x7fff)
        {
          length = pipe.Read(readBuffer, 0, 4);
          while (length < 4)
          {
            length += pipe.Read(readBuffer, length, 4 - length);
          }
          Environment.ExitCode = BitConverter.ToInt32(readBuffer, 0);
          break;
        }
        length = pipe.Read(readBuffer, 0, dataLength);
        while (length < dataLength)
        {
          length += pipe.Read(readBuffer, length, dataLength - length);
        }
        // forward output to log
        LOG.Info(Encoding.UTF8.GetString(readBuffer, 0, length));
      } 
      while (true);
    }
  }
}