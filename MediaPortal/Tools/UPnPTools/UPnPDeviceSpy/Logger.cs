using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UPnP.Infrastructure;

namespace UPnPDeviceSpy
{
  class Logger : ILogger
  {
    private TextWriter _writer = null;
    protected const string LOG_FORMAT_STR = "{0}: {1}";

    public Logger()
    {
      _writer = File.CreateText("c:\\UPnP.log");
    }

    public void Close()
    {
      if (_writer != null)
      {
        _writer.Close();
        _writer.Dispose();
        _writer = null;
      }
    }

    protected void WriteException(Exception ex)
    {
      _writer.WriteLine("Exception: " + ex);
      _writer.WriteLine("  Message: " + ex.Message);
      _writer.WriteLine("  Site   : " + ex.TargetSite);
      _writer.WriteLine("  Source : " + ex.Source);
      _writer.WriteLine("Stack Trace:");
      _writer.WriteLine(ex.StackTrace);
      if (ex.InnerException != null)
      {
        _writer.WriteLine("Inner Exception(s):");
        WriteException(ex.InnerException);
      }
    }

    public void Debug(string format, params object[] args)
    {
      _writer.WriteLine("Debug: " + string.Format(format, args));
      _writer.Flush();
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      _writer.WriteLine("Debug: " + string.Format(format, args));
      WriteException(ex);
      _writer.Flush();
    }

    public void Info(string format, params object[] args)
    {
      _writer.WriteLine("Info: " + string.Format(format, args));
      _writer.Flush();
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      _writer.WriteLine("Info: " + string.Format(format, args));
      WriteException(ex);
      _writer.Flush();
    }

    public void Warn(string format, params object[] args)
    {
      _writer.WriteLine("Warn: " + string.Format(format, args));
      _writer.Flush();
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      _writer.WriteLine("Warn: " + string.Format(format, args));
      WriteException(ex);
      _writer.Flush();
    }

    public void Error(string format, params object[] args)
    {
      _writer.WriteLine("Error: " + string.Format(format, args));
      _writer.Flush();
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      _writer.WriteLine("Error: " + string.Format(format, args));
      WriteException(ex);
      _writer.Flush();
    }

    public void Error(Exception ex)
    {
      _writer.WriteLine("Ex...");
      WriteException(ex);
      _writer.Flush();
    }

    public void Critical(string format, params object[] args)
    {
      _writer.WriteLine("Crit: " + string.Format(format, args));
      _writer.Flush();
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      _writer.WriteLine("Crit: " + string.Format(format, args));
      WriteException(ex);
      _writer.Flush();
    }

    public void Critical(Exception ex)
    {
      _writer.WriteLine("Crit ex...");
      WriteException(ex);
      _writer.Flush();
    }
  }
}
