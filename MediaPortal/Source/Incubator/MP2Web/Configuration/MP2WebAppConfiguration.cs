#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Web.Configuration
{
  /// <summary>
  /// This class defines the Configuration Object which gets send to the Web App.
  /// ATTENTION:
  /// It must be in line with interface.ConfigurationService.ts
  /// </summary>
  public class MP2WebAppConfiguration
  {
    /// <summary>
    /// URL to the WebAPI, e.g. "http://localhost:5555"
    /// </summary>
    public string WebApiUrl { get; set; }

    /// <summary>
    /// All availble Routes inseide the MP2WebApp except the "/" Route which is hardcoded inside the MP2WebApp.
    /// </summary>
    public List<MP2WebAppRouterConfiguration> Routes { get; set; }

    /// <summary>
    /// How many Movies are shown in one row
    /// </summary>
    public int MoviesPerRow { get; set; }
    
    /// <summary>
    /// How many Movies are requested per Request
    /// </summary>
    public int MoviesPerQuery { get; set; }
  }
}