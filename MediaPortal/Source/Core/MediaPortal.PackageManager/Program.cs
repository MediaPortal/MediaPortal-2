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
    private static ILogger _log = new BasicConsoleLogger(LogLevel.All);
    private static string[] _args;

    public static void Run(string[] args, ILogger log)
    {
      _log = log;
      Main(args);
    }

    public static void Main(string[] args)
    {
      _args = args;

      // Exit codes:
      // 0 = OK
      // 1 = Parameter error
      // 2 = General exception
      // 3 = IO exception
      // 4 = elevated client did not connect or service pipe is not available
      try
      {
        var options = new CommandLineOptions();
        var parser = new Parser(with => with.HelpWriter = Console.Out);
        parser.ParseArgumentsStrict(args, options, Dispatch, () => Environment.ExitCode = 1);
      }
      catch (IOException ex)
      {
        _log.Error(ex.Message);
        Environment.ExitCode = 3;
      }
      catch (Exception ex)
      {
        _log.Error(ex.Message);
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

      var baseOptions = options as BaseOptions;
      if (baseOptions != null)
      {
        if (!String.IsNullOrEmpty(baseOptions.RedirectOutput))
        {
          _log = new NamedPipeClientLogger(baseOptions.RedirectOutput, LogLevel.All);
          _log.Debug("Redirection output to {0}", baseOptions.RedirectOutput);
        }

        try
        {
          if (baseOptions.RequiresElevation && !baseOptions.IsElevated)
          {
            _log.Debug("Elevation required");
            try
            {
              if (!RunElevated())
              {
                Environment.ExitCode = 4;
              }
            }
            catch (Exception ex)
            {
              _log.Error("Run elevated", ex);
              Environment.ExitCode = 4;
            }
          }
          else
          {
            if (baseOptions.IsElevated)
            {
              _log.Debug("Running elevated");
            }

            switch (operation)
            {
              case Operation.CreateUser:
              case Operation.RevokeUser:
                PackageAdminCmd.Dispatch(_log, operation, options);
                break;

              case Operation.Create:
                PackageBuilderCmd.Dispatch(_log, operation, options);
                break;

              case Operation.Publish:
              case Operation.Recall:
                PackagePublisherCmd.Dispatch(_log, operation, options);
                break;

                case Operation.ListAssemblies:
                OtherCmd.Dispatch(_log, operation, options);
                break;

              default:
                //PackageInstallerCmd.Dispatch(_log, operation, options);
                for (int n = 0; n < 100; ++n)
                {
                  switch (n % 5)
                  {
                    case 0:
                      _log.Debug("Debug {0}", n);
                      break;

                    case 1:
                      if(n > 90)
                        _log.Info("Info {0} kdjfsdhfg djshf gjhksadfg lasdhfg lksdaj fhkjsdahf kjsdahfkljdasfh klsdajhf klajdfh kldash fkldsjhfa kljahsfd kjdashf", n);
                      _log.Info("Info {0}", n);
                      break;

                    case 2:
                      _log.Warn("Warn {0}", n);
                      break;

                    case 3:
                      _log.Error("Error {0}", n);
                      break;

                    case 4:
                      if (n > 90)
                        _log.Critical("Critical {0}\ndfjhfg djsfg sdjfg sdjhfgdsaf\ndsajhfg dsajf gdasjfgh sdjfg djh sf\nsdfjhgdsjh fgdsjh fgsdhj", n);
                      _log.Critical("Critical {0}", n);
                      break;
                  }
                  Thread.Sleep(20);
                }
                break;
            }
          }
        }
        finally
        {
          try
          {
            RunOnExit(baseOptions, Environment.ExitCode == 0);
          }
          catch
          {
            // ignored
          }
          var pipeLog = _log as NamedPipeClientLogger;
          if (pipeLog != null)
          {
            _log = new BasicConsoleLogger(LogLevel.All);
            pipeLog.Close();
          }
        }
      }
    }

    private static void RunOnExit(BaseOptions sharedOptions, bool success)
    {
      var options = sharedOptions as InstallOptions;
      if (options != null && !options.IsElevated && !String.IsNullOrEmpty(options.RunOnExitProgram))
      {
        _log.Debug("Run on exit: success:{0}: {1} {2}", success, options.RunOnExitProgram, success ? options.GetSuccessArgsString() : options.GetFailArgsString());
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

    internal static bool RunElevated()
    {
#if DEBUG
      // wait a bit so the service server pipe is ready
      Thread.Sleep(500);
#endif
      try
      {
        // open a server pipe that will receive all outputs from elevated instance
        using (var serverPipe = new NamedPipeServerStream("MediaPortal.PackageManager",
           PipeDirection.In, 1,
          PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024))
        {

          // now wait for an connection to our server pipe and process the incomming messages
          var connectData = new ElevatedClientConnectData
          {
            ServerPipe = serverPipe,
            ConnectedEvent = new ManualResetEvent(false)
          };
          serverPipe.BeginWaitForConnection(ClientConnected, connectData);


          // contact windows service via named pipe
          using (var pipe = new NamedPipeClientStream(".", "MediaPortal.PackageService", PipeDirection.Out))
          {
            pipe.Connect();

            // rebuild arguments string
            var argsString = new StringBuilder();
            foreach (var arg in _args)
            {
              if (arg.IndexOfAny(new[] { ' ', '\\', '\t' }) >= 0)
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

            argsString.Append(" --redirectoutput \"MediaPortal.PackageManager\"");

            // send command
            // file name in 1st line, arguments in 2nd line
            var data = Encoding.UTF8.GetBytes(String.Concat(
              typeof(Program).Assembly.Location, "\n",
              argsString.ToString()));
            pipe.Write(data, 0, data.Length);
            pipe.Flush();
            try
            {
              pipe.Close();
            }
            catch
            {
              // ignored
            }

            if (!connectData.ConnectedEvent.WaitOne(10000))
            {
              connectData.ConnectedEvent = null;
              // if the elevated client does not connect within 10 seconds, we cancel wit an error code
              _log.Error("Elevated client did not connect within 10 seconds");
              return false;
            }
            if (connectData.ConnectException != null)
            {
              _log.Error("Elevateed client connection", connectData.ConnectException);
              return false;
            }
            // poll all data from client
            // all messages have the following structure
            // 2 byte message id
            // 2 byte data length => n
            // n byte data
            var readBuffer = new byte[4096];
            do
            {
              int length = serverPipe.Read(readBuffer, 0, 4);
              while (length < 4)
              {
                length += serverPipe.Read(readBuffer, length, 4 - length);
              }
              int msgId = BitConverter.ToInt16(readBuffer, 0);
              int dataLength = BitConverter.ToInt16(readBuffer, 2);
              while (length < 4 + dataLength)
              {
                length += serverPipe.Read(readBuffer, length, (4 + dataLength) - length);
              }
              switch (msgId)
              {
                case 0: // exit
                  if (dataLength != 4)
                  {
                    _log.Error("Invalid data from elevated client: exit code must have 4 byte data length");
                    return false;
                  }
                  Environment.ExitCode = BitConverter.ToInt32(readBuffer, 4);
                  // return true even if exit code != 0, because actually everything went fine on this side
                  return true;

                case 1: // debug text
                  _log.Debug(Encoding.UTF8.GetString(readBuffer, 4, dataLength));
                  break;
                case 2: // info text
                  _log.Info(Encoding.UTF8.GetString(readBuffer, 4, dataLength));
                  break;
                case 3: // warning text
                  _log.Warn(Encoding.UTF8.GetString(readBuffer, 4, dataLength));
                  break;
                case 4: // error text
                  _log.Error(Encoding.UTF8.GetString(readBuffer, 4, dataLength));
                  break;
                case 5: // critical error text
                  _log.Critical(Encoding.UTF8.GetString(readBuffer, 4, dataLength));
                  break;

                default:
                  _log.Error("Invalid data from elevated client: message id is unknown: {0}", msgId);
                  return false;
              }
            }
            while (true);
          }
        }
      }
      catch (Exception ex)
      {
        _log.Error("Elevate", ex);
        return false;
      }
    }

    private class ElevatedClientConnectData
    {
      public NamedPipeServerStream ServerPipe;
      public ManualResetEvent ConnectedEvent;
      public Exception ConnectException;
    }

    private static void ClientConnected(IAsyncResult ar)
    {
      var data = ar.AsyncState as ElevatedClientConnectData;
      if (data != null)
      {
        try
        {
          data.ServerPipe.EndWaitForConnection(ar);
        }
        catch (Exception ex)
        {
          data.ConnectException = ex;
        }
        if (data.ConnectedEvent != null)
        {
          data.ConnectedEvent.Set();
        }
      }
    }
  }
}