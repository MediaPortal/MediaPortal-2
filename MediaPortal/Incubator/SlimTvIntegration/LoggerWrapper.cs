#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using Mediaportal.TV.Server.TVLibrary.IntegrationProvider.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Integration
{
  class LoggerWrapper: ILogger
  {
    private readonly Common.Logging.ILogger _logger;

    public LoggerWrapper ()
    {
      _logger = ServiceRegistration.Get<Common.Logging.ILogger>();
    }

    public void Debug (string format, params object[] args)
    {
      _logger.Debug(format, args);
    }

    public void Debug (string format, Exception ex, params object[] args)
    {
      _logger.Debug(format, ex, args);
    }

    public void Debug (Type caller, string format, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Debug(format, args);
    }

    public void Debug (Type caller, string format, Exception ex, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Debug(format, ex, args);
    }

    public void Info (string format, params object[] args)
    {
      _logger.Info(format, args);
    }

    public void Info (string format, Exception ex, params object[] args)
    {
      _logger.Info(format, ex, args);
    }

    public void Info(Type caller, string format, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Info(format, args);
    }

    public void Info(Type caller, string format, Exception ex, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Info(format, ex, args);
    }

    public void Warn (string format, params object[] args)
    {
      _logger.Warn(format, args);
    }

    public void Warn (string format, Exception ex, params object[] args)
    {
      _logger.Warn(format, ex, args);
    }

    public void Warn(Type caller, string format, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Warn(format, args);
    }

    public void Warn(Type caller, string format, Exception ex, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Warn(format, ex, args);
    }

    public void Error (string format, params object[] args)
    {
      _logger.Error(format, args);
    }

    public void Error (string format, Exception ex, params object[] args)
    {
      _logger.Error(format, ex, args);
    }

    public void Error (Exception ex)
    {
      _logger.Error(ex);
    }
    
    public void Error(Type caller, Exception ex)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Error(ex);
    }

    public void Error(Type caller, string format, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Error(format, args);
    }

    public void Error(Type caller, string format, Exception ex, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Error(format, ex, args);
    }

    public void Critical (string format, params object[] args)
    {
      _logger.Critical(format, args);
    }

    public void Critical (string format, Exception ex, params object[] args)
    {
      _logger.Critical(format, ex, args);
    }

    public void Critical (Exception ex)
    {
      _logger.Critical(ex);
    }

    public void Critical(Type caller, Exception ex)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Critical(ex);
    }

    public void Critical(Type caller, string format, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Critical(format, args);
    }

    public void Critical(Type caller, string format, Exception ex, params object[] args)
    {
      // TODO: handle caller type (format, use own logger,...)
      _logger.Critical(format, ex, args);
    }
  }
}
