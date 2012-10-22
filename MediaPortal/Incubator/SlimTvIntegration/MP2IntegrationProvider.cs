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

using Mediaportal.TV.Server.TVLibrary.Interfaces.Integration;

namespace MediaPortal.Plugins.SlimTv.Integration
{
  public class MP2IntegrationProvider: IIntegrationProvider
  {
    readonly PathManagerWrapper _pathManagerWrapper = new PathManagerWrapper();
    readonly LoggerWrapper _loggerWrapper = new LoggerWrapper();

    public IPathManager PathManager
    {
      get { return _pathManagerWrapper; }
    }

    public ILogger Logger
    {
      get { return _loggerWrapper; }
    }
  }
}
