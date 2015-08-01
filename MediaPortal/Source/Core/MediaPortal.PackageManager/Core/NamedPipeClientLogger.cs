using System;
using System.IO.Pipes;
using System.Text;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageManager.Core
{
  public class NamedPipeClientLogger : ILogger, IDisposable
  {
    private readonly NamedPipeClientStream _pipe;
    private readonly int _logLevel;

    public NamedPipeClientLogger(string pipeName, LogLevel logLevel)
    {
      _logLevel = (int)logLevel;
      _pipe = new NamedPipeClientStream(".", "MediaPortal.PackageManager", PipeDirection.Out);
      _pipe.Connect();
    }

    public void Close()
    {
      if (_pipe != null)
      {
        // send exit code
        var data = JoinArrays(
          BitConverter.GetBytes((short)0),
          BitConverter.GetBytes((short)4),
          BitConverter.GetBytes(Environment.ExitCode));
        _pipe.Write(data, 0, data.Length);
        _pipe.Flush();
        _pipe.Close();
      }
    }

    private byte[] JoinArrays(params byte[][] arrays)
    {
      int length = 0;
      foreach (var array in arrays)
      {
        length += array.Length;
      }
      var result = new byte[length];
      int pos = 0;
      foreach (var array in arrays)
      {
        array.CopyTo(result, pos);
        pos += array.Length;
      }
      return result;
    }

    private void SendText(short msgId, string text)
    {
      if (_pipe != null)
      {
        var textData = Encoding.UTF8.GetBytes(text);
        var data = JoinArrays(
          BitConverter.GetBytes(msgId),
          BitConverter.GetBytes((short)textData.Length),
          textData);
        _pipe.Write(data, 0, data.Length);
      }
    }

    #region Implementation of ILogger

    public void Debug(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Debug)
      {
        System.Diagnostics.Debug.Print(format, args);
        SendText(1, String.Format(format, args));
      }
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Debug)
      {
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(1, String.Concat(String.Format(format, args), "\n", ex.ToString()));
      }
    }

    public void Info(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Information)
      {
        System.Diagnostics.Debug.Print(format, args);
        SendText(2, String.Format(format, args));
      }
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Information)
      {
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(2, String.Concat(String.Format(format, args), "\n", ex.ToString()));
      }
    }

    public void Warn(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Warning)
      {
        System.Diagnostics.Debug.Print(format, args);
        SendText(3, String.Format(format, args));
      }
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Warning)
      {
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(3, String.Concat(String.Format(format, args), "\n", ex.ToString()));
      }
    }

    public void Error(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        System.Diagnostics.Debug.Print(format, args);
        SendText(4, String.Format(format, args));
      }
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(4, String.Concat(String.Format(format, args), "\n", ex.ToString()));
      }
    }

    public void Error(Exception ex)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(4, ex.ToString());
      }
    }

    public void Critical(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        System.Diagnostics.Debug.Print(format, args);
        SendText(5, String.Format(format, args));
      }
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(5, String.Concat(String.Format(format, args), "\n", ex.ToString()));
      }
    }

    public void Critical(Exception ex)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        System.Diagnostics.Debug.Print(ex.ToString());
        SendText(5, ex.ToString());
      }
    }

    #endregion

    #region Implementation of IDisposable

    ~NamedPipeClientLogger()
    {
      Close();
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
      Close();
    }

    #endregion
  }
}