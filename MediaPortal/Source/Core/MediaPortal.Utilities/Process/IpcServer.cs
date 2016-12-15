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

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MediaPortal.Utilities.Process
{
  /// <summary>
  /// InpterProcessCommunication server
  /// </summary>
  /// <remarks>Opens a named pipe to allow other processes to comunicate with this application.</remarks>
  public class IpcServer : IDisposable
  {
    #region Private fields

    private readonly List<NamedPipeServerStream> _serverPipes = new List<NamedPipeServerStream>();

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new instance of the server
    /// </summary>
    /// <param name="appName">Name of this application/service</param>
    /// <remarks>Only one instance of the server should be created per application.</remarks>
    public IpcServer(string appName)
    {
      AppName = appName;
      PipeName = Ipc.PIPE_PREFIX + appName;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string AppName { get; private set; }

    /// <summary>
    /// Gets the used pipe name.
    /// </summary>
    public string PipeName { get; private set; }

    /// <summary>
    /// Gets if the server is opened
    /// </summary>
    public bool IsOpen
    {
      get { return _serverPipes.Count > 0; }
    }

    /// <summary>
    /// Gets or sets a custom shutdown callback
    /// </summary>
    public Func<bool> CustomShutdownCallback { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Opens the server
    /// </summary>
    public void Open()
    {
      if (IsOpen)
        return;

      OpenNewPipe();
    }

    /// <summary>
    /// Closes all open connections and pending named pipes
    /// </summary>
    /// <remarks>Any data eventually not sent out yet will be sent before the pipe is closed.</remarks>
    public void Close()
    {
      lock (_serverPipes)
      {
        foreach (var pipe in _serverPipes)
        {
          try
          {
            pipe.Flush();
          }
          catch
          {
            // ignored
          }
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

    #endregion

    #region private methods

    private void OpenNewPipe()
    {
      // start a new server pipe with read and write access for any authenticated user of this system.
      // it is important to have PipeAccessRights.CreateNewInstance allowed, or else it would not be possible to create anew pipe for the next client.
      var pipeSecurity = new PipeSecurity();
      pipeSecurity.SetAccessRule(
        new PipeAccessRule(
          new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
          PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));

      var pipe = new NamedPipeServerStream(PipeName,
        PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024, pipeSecurity);

      lock (_serverPipes)
      {
        _serverPipes.Add(pipe);
      }
      pipe.BeginWaitForConnection(WaitForConnectionCallback, pipe);
    }

    private void WaitForConnectionCallback(IAsyncResult ar)
    {
      try
      {
        // finish async wait
        var pipe = (NamedPipeServerStream)ar.AsyncState;
        pipe.EndWaitForConnection(ar);

        // start another pipe for the next client
        OpenNewPipe();

        try
        {
          var header = new byte[6];
          while (true)
          {
            // read 6 byte header
            int cnt = 0;
            while (cnt < header.Length)
            {
              int newCnt = pipe.Read(header, cnt, header.Length - cnt);
              if (newCnt == 0) // end of stream
                return;
              cnt += newCnt;
            }
            Ipc.Command commandId = (Ipc.Command)BitConverter.ToUInt16(header, 0);
            ushort messageId = BitConverter.ToUInt16(header, 2); // for future use to allow parallel messages from a single client
            ushort dataLength = BitConverter.ToUInt16(header, 4);

            var data = new byte[dataLength];
            if (dataLength > 0)
            {
              cnt = 0;
              while (cnt < data.Length)
              {
                int newCnt = pipe.Read(data, cnt, data.Length - cnt);
                if (newCnt == 0) // end of stream
                  return;
                cnt += newCnt;
              }
            }

            var response = new byte[1024];
            int responseOffset = 6;
            Ipc.ResponseCode responseCode;
            try
            {
              responseCode = ProcessCommand(commandId, data, 0, dataLength, ref response, ref responseOffset);
            }
            catch (Exception ex)
            {
              responseCode = Ipc.ResponseCode.ServerException;
              responseOffset = 6;
              Ipc.StringToBytes(ex.Message, response, ref responseOffset);
              Ipc.StringToBytes(ex.GetType().FullName, response, ref responseOffset);
            }
            if (((ushort)commandId & Ipc.NO_RESPONSE_COMMAND) == 0)
            {
              // send response
              BitConverter.GetBytes((ushort)responseCode).CopyTo(response, 0);
              BitConverter.GetBytes(messageId).CopyTo(response, 2);
              BitConverter.GetBytes((ushort)(responseOffset - 6)).CopyTo(response, 4);
              pipe.Write(response, 0, responseOffset);
            }
          }
        }
        finally
        {
          lock (_serverPipes)
          {
            _serverPipes.Remove(pipe);
            pipe.Close();
          }
        }
      }
      catch (Exception ex)
      {

      }
    }

    /// <summary>
    /// Processes an incomming comand and prepares the response data
    /// </summary>
    /// <param name="commandId">Id of the command</param>
    /// <param name="data">Custom data of the command. The array might be larger then the actual data.
    /// The 1st valid byte is at <paramref name="dataOffset"/>. The number of valid bytes is provided by <paramref name="dataLength"/>.</param>
    /// <param name="dataOffset">First valid data byte in <paramref name="data"/>.</param>
    /// <param name="dataLength">Number of valid bytes in <paramref name="data"/>.</param>
    /// <param name="response">Byte array to write the custom response data into. The 1st written byte must be at <paramref name="responseOffset"/>.
    /// At the end <paramref name="responseOffset"/> must be set to the 1st byte after the response.
    /// The byte array can be replaced by a larger arrqay if needed. All data up to the current <paramref name="responseOffset"/> must be copied into this new array.</param>
    /// <param name="responseOffset">Holds the 1st byte offset to write response data to <paramref name="response"/> when mthod is called.
    /// The value must be updatet to the 1st byte offset after the valid response date before the method returns.</param>
    /// <returns>Returns the command response code.</returns>
    private Ipc.ResponseCode ProcessCommand(Ipc.Command commandId, byte[] data, int dataOffset, int dataLength, ref byte[] response, ref int responseOffset)
    {
      switch (commandId)
      {
        case Ipc.Command.GetProcessId:
          using (var process = System.Diagnostics.Process.GetCurrentProcess())
          {
            BitConverter.GetBytes(process.Id).CopyTo(response, responseOffset);
          }
          responseOffset += 4;
          return Ipc.ResponseCode.Ok;

        case Ipc.Command.Shutdown:
          return ShutdownApplication() ? Ipc.ResponseCode.Ok : Ipc.ResponseCode.False;
      }

      return Ipc.ResponseCode.UnknownCommand;
    }

    private bool ShutdownApplication()
    {
      if (CustomShutdownCallback != null)
      {
        return CustomShutdownCallback();
      }
      using (var process = System.Diagnostics.Process.GetCurrentProcess())
      {
        return process.CloseMainWindow();
      }
    }

    #endregion

    #region IDisposable pattern

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> if called from <see cref="Dispose()"/>, <c>false</c> if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      Close();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
    /// </summary>
    ~IpcServer()
    {
      Dispose(false);
    }

    #endregion
  }
}