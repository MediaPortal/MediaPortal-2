#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using MediaPortal.Core.Logging;
using log4net;

namespace MediaPortal.Core.Services.Logging
{
  public class Log4NetLogger : ILogger
  {
    /// <summary>
    /// Creates a new <see cref="Log4NetLogger"/> instance and initializes it with the given parameters.
    /// </summary>
    /// <param name="logPath">Path where the logfiles should be written to.</param>
    public Log4NetLogger(string logPath)
    {
      XmlDocument xmlDoc = new XmlDocument();
      using (Stream stream = new FileStream(Application.ExecutablePath + ".config", FileMode.Open, FileAccess.Read))
        xmlDoc.Load(stream);
      XmlNodeList nodeList = xmlDoc.SelectNodes("configuration/log4net/appender/file");
      foreach (XmlNode node in nodeList)
        if (node.Attributes != null)
          foreach (XmlAttribute attribute in node.Attributes)
            if (attribute.Name.Equals("value"))
            {
              attribute.Value = Path.Combine(logPath, Path.GetFileName(attribute.Value));
              break;
            }

      using (MemoryStream stream = new MemoryStream())
      {
        xmlDoc.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        log4net.Config.XmlConfigurator.Configure(stream);
      }
    }

    protected ILog GetLogger
    {
      get { return LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType); }
    }

    #region ILogger implementation

    public void Debug(string format, params object[] args)
    {
      GetLogger.DebugFormat(format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      GetLogger.Debug(string.Format(format, args), ex);
    }

    public void Info(string format, params object[] args)
    {
      GetLogger.InfoFormat(format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      GetLogger.Info(string.Format(format, args), ex);
    }

    public void Warn(string format, params object[] args)
    {
      GetLogger.WarnFormat(format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      GetLogger.Warn(string.Format(format, args), ex);
    }

    public void Error(string format, params object[] args)
    {
      GetLogger.ErrorFormat(format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      GetLogger.Error(string.Format(format, args), ex);
    }

    public void Error(Exception ex)
    {
      GetLogger.Error("", ex);
    }

    public void Critical(string format, params object[] args)
    {
      GetLogger.FatalFormat(format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      GetLogger.Fatal(string.Format(format, args), ex);
    }

    public void Critical(Exception ex)
    {
      GetLogger.Fatal("", ex);
    }

    #endregion
  }
}
