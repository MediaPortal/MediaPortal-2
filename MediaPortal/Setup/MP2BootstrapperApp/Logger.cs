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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.Models;

namespace MP2BootstrapperApp
{
  public class Logger
  {
    private readonly IBootstrapperApplicationModel _model;

    public Logger(IBootstrapperApplicationModel model)
    {
      _model = model;
    }

    public void LogStandard(string message)
    {
      _model.LogMessage(LogLevel.Standard, message);
    }

    public void LogDebug(string message)
    {
      _model.LogMessage(LogLevel.Debug, message);
    }

    public void LogError(string message)
    {
      _model.LogMessage(LogLevel.Error, message);
    }
  }
}
