#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Helpers
{
  public class EscapedLogger : ILogger
  {
    private ILogger _logger;

    public EscapedLogger(ILogger logger)
    {
      _logger = logger;
    }

    private string Escape(string val)
    {
      return val?.Replace("{", "{{").Replace("}", "}}");
    }

    public void Debug(string format, params object[] args)
    {
      _logger.Debug(Escape(format), args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      _logger.Debug(Escape(format), ex, args);
    }

    public void Info(string format, params object[] args)
    {
      _logger.Info(Escape(format), args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      _logger.Debug(Escape(format), ex, args);
    }

    public void Warn(string format, params object[] args)
    {
      _logger.Warn(Escape(format), args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      _logger.Warn(Escape(format), ex, args);
    }

    public void Error(string format, params object[] args)
    {
      _logger.Error(Escape(format), args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      _logger.Error(Escape(format), ex, args);
    }

    public void Error(Exception ex)
    {
      _logger.Error(ex);
    }

    public void Critical(string format, params object[] args)
    {
      _logger.Critical(Escape(format), args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      _logger.Critical(Escape(format), ex, args);
    }

    public void Critical(Exception ex)
    {
      _logger.Critical(ex);
    }
  }
}
