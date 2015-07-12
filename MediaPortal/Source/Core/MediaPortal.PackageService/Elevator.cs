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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace MediaPortal.PackageService
{
  /// <summary>
  /// Delegate for simple logging
  /// </summary>
  /// <param name="error"><c>true</c> if this is an error.</param>
  /// <param name="format">Format of the message.</param>
  /// <param name="args">Format arguments.</param>
  public delegate void LogDelegate(bool error, string format, params object[] args);

  /// <summary>
  /// Elevator is a simple static class that opens a named pipe (MediaPortal.PackageService) and waits for 
  /// PackageManager to send a request to restart it with elevated privileges.
  /// All console output of the elevated process is sent back through the pipe.
  /// </summary>
  public static class Elevator
  {
    private static readonly List<NamedPipeServerStream> _serverPipes = new List<NamedPipeServerStream>();
    private static LogDelegate _log;

    private static void DummyLog(bool error, string format, params object[] args)
    { }

    public static void Start(LogDelegate log)
    {
      if (_log == null)
      {
        _log = log ?? DummyLog;
      }

      // start a new server pipe with read and write access for any authenticated user of this system.
      var pipeSecurity = new PipeSecurity();
      pipeSecurity.SetAccessRule(
        new PipeAccessRule(
          new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
          PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));

      var pipe = new NamedPipeServerStream("MediaPortal.PackageService",
        PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, 
        PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024, pipeSecurity);

      lock (_serverPipes)
      {
        _serverPipes.Add(pipe);
      }
      pipe.BeginWaitForConnection(WaitForConnectionCallback, pipe);
    }

    private static void WaitForConnectionCallback(IAsyncResult ar)
    {
      try
      {
        // finish async wait
        var pipe = (NamedPipeServerStream)ar.AsyncState;
        pipe.EndWaitForConnection(ar);

        // start another pipe
        Start(null);

        // read input data
        var buffer = new byte[1024];
        int length = pipe.Read(buffer, 0, buffer.Length);

        // input data is UTF8 string with file name in the 1st and arguments in the 2nd line
        var fileName = Encoding.UTF8.GetString(buffer, 0, length);
        string arguments = String.Empty;
        int n = fileName.IndexOf('\n');
        if (n >= 0)
        {
          arguments = fileName.Substring(n + 1);
          fileName = fileName.Substring(0, n);
        }

        var redirector = new StdOutRedirector(pipe);

        // start process and redirect std out and std error to our pipe
        var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.OutputDataReceived += redirector.ProcessOnDataReceived;
        process.StartInfo.RedirectStandardError = true;
        process.ErrorDataReceived += redirector.ProcessOnDataReceived;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // wait until process finishes and send exit code back through the pipe
        // we prefix this with 0x7fff instead of data length, so the receiver can see that this is not an UTF8 encoded string, no matter what the exit code is
        process.WaitForExit();
        pipe.Write(BitConverter.GetBytes(0x7fff), 0, 4);
        pipe.Write(BitConverter.GetBytes(process.ExitCode), 0, 4);

        lock (_serverPipes)
        {
          _serverPipes.Remove(pipe);
          pipe.Close();
        }
      }
      catch (Exception ex)
      {
        _log(true, "Processing: " + ex.Message);
      }
    }

    public static void Stop()
    {
      // close all open pipes
      lock (_serverPipes)
      {
        foreach (var pipe in _serverPipes)
        {
          try
          {
            pipe.Close();
          }
          catch
          {
            // ignored
          }
        }
        _serverPipes.Clear();
      }
    }

    private class StdOutRedirector
    {
      private readonly NamedPipeServerStream _pipe;

      public StdOutRedirector(NamedPipeServerStream pipe)
      {
        _pipe = pipe;
      }

      public void ProcessOnDataReceived(object sender, DataReceivedEventArgs e)
      {
        if (e.Data != null)
        {
          try
          {
            var data = Encoding.UTF8.GetBytes(e.Data);
            _pipe.Write(BitConverter.GetBytes(data.Length), 0, 4);
            _pipe.Write(data, 0, data.Length);
          }
          catch
          {
            // ignored
          }
        }
      }
    }
  }
}
