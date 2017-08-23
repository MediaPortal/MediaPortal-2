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
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Utilities.Process
{
  /// <summary>
  /// InpterProcessCommunication client
  /// </summary>
  public class IpcClient : IDisposable
  {
    #region public static methods

    /// <summary>
    /// Gets the names of all MP2 running applications
    /// </summary>
    /// <returns>Returns an array with the names of all running MP2 apps.</returns>
    public static string[] GetRunningApps()
    {
      var pipes = Directory.GetFiles(@"\\.\pipe\");
      var appNames = new List<string>();
      foreach (var pipe in pipes)
      {
        var name = pipe.Substring(pipe.LastIndexOf('\\') + 1);
        if(name.StartsWith(Ipc.PIPE_PREFIX, StringComparison.OrdinalIgnoreCase))
          appNames.Add(name.Substring(Ipc.PIPE_PREFIX.Length));
      }
      return appNames.ToArray();
    }

    /// <summary>
    /// Shuts down all MP2 applications
    /// </summary>
    /// <param name="exitTimeout">Timeout im milliseconds to wait for each individual application to shut down.
    /// <c>0</c> to not wait for the shutdown.
    /// <see cref="Timeout.Infinite"/> or <c>-1</c> to wait infinite.</param>
    /// <param name="killAfterTimeout"><c>true</c> if the process of the applications should be killed when a timeout occurs.
    /// <c>false</c> if the pocess should be left running when a timeout occurs.
    /// If <paramref name="exitTimeout"/> is <c>0</c> or <see cref="Timeout.Infinite"/> this parameter has no effect.</param>
    /// <returns>Returns <c>true</c> if all processs were successfully shut down or killed. <c>false</c> if any process is still running.
    /// If <paramref name="exitTimeout"/> is <c>0</c> <c>true</c> is returned.</returns>
    /// <remarks>The shut down for all applications is requested in parallel.</remarks>
    public static bool ShutdownAllApplications(int exitTimeout, bool killAfterTimeout)
    {
      var appNames = GetRunningApps();
      int succeededCnt = 0;
      Parallel.ForEach(appNames,
         appName =>
         {
           try
           {
             var client = new IpcClient(appName);
             client.Connect();
             if (client.ShudownApplication(exitTimeout, killAfterTimeout))
               ++succeededCnt;
           }
           catch (Exception ex)
           {

           }
         });
      return succeededCnt == appNames.Length;
    }

    #endregion

    #region private fields

    private NamedPipeClientStream _pipe;
    private ushort _nextMessageId = 1;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new instance of the IPC client for a specific application
    /// </summary>
    /// <param name="appName"></param>
    public IpcClient(string appName)
    {
      AppName = appName;
      PipeName = Ipc.PIPE_PREFIX + appName;
      ResponseTimeout = 2000;
    }

    #endregion

    #region public properties

    /// <summary>
    /// Gets the name of the application.
    /// </summary>
    public string AppName { get; private set; }

    /// <summary>
    /// Gets the used pipe name.
    /// </summary>
    public string PipeName { get; private set; }

    /// <summary>
    /// Gets if the client is currently connected to the application.
    /// </summary>
    public bool IsConnected
    {
      get { return _pipe != null; }
    }

    /// <summary>
    /// Gets or sets the message response timeout in milliseconds.
    /// </summary>
    /// <remarks>The default value is <c>2000</c>.</remarks>
    public int ResponseTimeout { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Connects to the application
    /// </summary>
    public void Connect()
    {
      if (IsConnected)
        return;

      try
      {
        _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
        _pipe.Connect();
        _nextMessageId = 1;
      }
      catch
      {
        Disconnect();
      }
    }

    /// <summary>
    /// Disconnects from the application
    /// </summary>
    public void Disconnect()
    {
      if (_pipe != null)
      {
        try
        {
          _pipe.Flush();
        }
        catch
        {
          // ignored
        }
        try
        {
          _pipe.Close();
        }
        catch
        {
          // ignored
        }
        _pipe = null;
      }
    }

    /// <summary>
    /// Requests the process id from the application.
    /// </summary>
    /// <returns>Returns the process id from the application</returns>
    /// <exception cref="InvalidOperationException">Throws an <see cref="InvalidOperationException"/> when the client is not connected to the application.</exception>
    public int GetProcessId()
    {
      CheckConnected();
      int offset;
      var data = GetCommandBuffer(0, out offset);
      byte[] response;
      var responseCode = SendReceive(Ipc.Command.GetProcessId, data, offset, out response);
      if (responseCode != Ipc.ResponseCode.Ok)
        throw new IpcException(String.Format("GetProcessId: Invalid response code: {0}", responseCode));
      if (response == null || response.Length < 4)
        throw new IpcException("GetProcessId: Invalid response");
      return BitConverter.ToInt32(response, 0);
    }

    /// <summary>
    /// Request the application to shut down.
    /// </summary>
    /// <param name="exitTimeout">Timeout im milliseconds to wait for the application to shut down.
    /// <c>0</c> to not wait for the shutdown.
    /// <see cref="Timeout.Infinite"/> or <c>-1</c> to wait infinite.</param>
    /// <param name="killAfterTimeout"><c>true</c> if the process of the application should be killed when a timeout occurs.
    /// <c>false</c> if the pocess should be left running when a timeout occurs.
    /// If <paramref name="exitTimeout"/> is <c>0</c> or <see cref="Timeout.Infinite"/> this parameter has no effect.</param>
    /// <returns>Returns <c>true</c> if the process was successfully shut down or killed. <c>false</c> if the process is still running.
    /// If <paramref name="exitTimeout"/> is <c>0</c> <c>true</c> is returned.</returns>
    public bool ShudownApplication(int exitTimeout, bool killAfterTimeout)
    {
      CheckConnected();
      int processId = -1;
      if (exitTimeout != 0)
      {
        processId = GetProcessId();
      }

      int offset;
      var data = GetCommandBuffer(0, out offset);
      byte[] response;
      var responseCode = SendReceive(Ipc.Command.Shutdown, data, offset, out response);
      if (responseCode == Ipc.ResponseCode.False)
        return false;
      if (responseCode != Ipc.ResponseCode.Ok)
        throw new IpcException(String.Format("GetProcessId: Invalid response code: {0}", responseCode));

      if (exitTimeout == 0)
        return true;

      var process = System.Diagnostics.Process.GetProcessById(processId);
      if (process.WaitForExit(exitTimeout))
        return true;

      if (killAfterTimeout)
      {
        process.Kill();
        return true;
      }
      return false;
    }

    #endregion

    #region Protected methods

    protected void CheckConnected()
    {
      if (!IsConnected)
        throw new InvalidOperationException(String.Format("IPC client is not connected to {0}", AppName));
    }

    protected byte[] GetCommandBuffer(int length, out int offset)
    {
      offset = 6;
      return new byte[6 + length];
    }

    protected Ipc.ResponseCode SendReceive(Ipc.Command commandId, byte[] data, int dataLength, out byte[] response)
    {
      CheckConnected();
      ushort messageId = _nextMessageId;
      _nextMessageId = (ushort)(_nextMessageId == UInt16.MaxValue ? 1 : _nextMessageId + 1);

      BitConverter.GetBytes((ushort)commandId).CopyTo(data, 0);
      BitConverter.GetBytes(messageId).CopyTo(data, 2);
      BitConverter.GetBytes((ushort)(dataLength - 6)).CopyTo(data, 4);

      _pipe.Write(data, 0, dataLength);
      _pipe.Flush();
      if (((ushort)commandId & Ipc.NO_RESPONSE_COMMAND) != 0)
      {
        response = null;
        return Ipc.ResponseCode.Ok;
      }
      var header = new byte[6];
      var tEnd = DateTime.Now.AddMilliseconds(ResponseTimeout);
      while (DateTime.Now <= tEnd)
      {
        // read 6 byte header
        int cnt = 0;
        while (cnt < header.Length)
        {
          var readDoneEvent = new ManualResetEvent(false);
          int newCnt = 0;
          Exception endReadEx = null;
          _pipe.BeginRead(header, cnt, header.Length - cnt, ar =>
          {
            try
            {
              newCnt = _pipe.EndRead(ar);
            }
            catch (Exception ex)
            {
              endReadEx = ex;
            }
            readDoneEvent.Set();
          }, null);
          if (!readDoneEvent.WaitOne(ResponseTimeout))
          {
            Disconnect();
            Connect();
            throw new TimeoutException(cnt == 0
               ? String.Format("No response for IPC message from {0} for command {1}", AppName, commandId)
               : String.Format("Timeout for IPC message from {0} for command {1}", AppName, commandId));
          }
          if (endReadEx != null)
          {
            throw new IpcException(endReadEx.Message, endReadEx);
          }
          if (newCnt == 0) // end of stream
            throw new IpcException("Connection closed unexpected");
          cnt += newCnt;
        }
        Ipc.ResponseCode responseCode = (Ipc.ResponseCode)BitConverter.ToUInt16(header, 0);
        ushort responseMessageId = BitConverter.ToUInt16(header, 2); // for future use to allow parallel messages from a single client
        ushort responseLength = BitConverter.ToUInt16(header, 4);

        response = new byte[responseLength];
        if (responseLength > 0)
        {
          cnt = 0;
          while (cnt < response.Length)
          {
            var readDoneEvent = new ManualResetEvent(false);
            int newCnt = 0;
            Exception endReadEx = null;
            _pipe.BeginRead(response, cnt, response.Length - cnt, ar =>
            {
              try
              {
                newCnt = _pipe.EndRead(ar);
              }
              catch (Exception ex)
              {
                endReadEx = ex;
              }
              readDoneEvent.Set();
            }, null);
            if (!readDoneEvent.WaitOne(ResponseTimeout))
            {
              Disconnect();
              Connect();
              throw new TimeoutException(String.Format("Timeout for IPC message from {0} for command {1}", AppName, commandId));
            }
            if (endReadEx != null)
            {
              throw new IpcException(endReadEx.Message, endReadEx);
            }
            if (newCnt == 0) // end of stream
              throw new IpcException("Connection closed unexpected");
            cnt += newCnt;
          }
        }
        if (responseMessageId == messageId)
        {
          if (responseCode == Ipc.ResponseCode.ServerException)
          {
            int offset = 0;
            var message = Ipc.BytesToString(response, ref offset);
            var typeName = Ipc.BytesToString(response, ref offset);
            throw new IpcException(String.Format("A IPC server exception of type {0} was thrown:\n{1}", typeName, message));
          }
          return responseCode;
        }
      }
      throw new TimeoutException(String.Format("No response for IPC message from {0} for command {1}", AppName, commandId));
    }

    #endregion

    #region IDisposable pattern

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> if called from <see cref="Dispose()"/>, <c>false</c> if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      Disconnect();
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
    ~IpcClient()
    {
      Dispose(false);
    }

    #endregion
  }

  public class IpcException : Exception
  {
    public IpcException(string message) : base(message)
    { }

    public IpcException(string message, Exception innerException) : base(message, innerException)
    { }
  }
}