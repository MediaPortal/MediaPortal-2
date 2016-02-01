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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Web.Configuration;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaPortal.Plugins.MP2Web.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for Trailers
  /// </summary>
  [Route("/api/[Controller]")]
  public class ConfigurationController : Controller
  {
    #region Private fields

    private readonly ILogger _logger;

    #endregion

    #region Constructor

    public ConfigurationController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<ConfigurationController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/Configuration/
    /// </summary>
    /// <returns>The Configuration for the MP2Web App</returns>
    [HttpGet()]
    public MP2WebAppConfiguration Get()
    {
      return new MP2WebAppConfiguration
      {
        WebApiUrl = "http://localhost:5555",
        Routes = new List<MP2WebAppRouterConfiguration>
        {
          new MP2WebAppRouterConfiguration
          {
            Name = "MediaLibrary",
            Label = "Media Library",
            Path = "/MediaLibrary",
            Visible = true,
            Pages = new List<MP2WebAppRouterConfiguration>
            {
              new MP2WebAppRouterConfiguration
              {
                Name = "Home",
                Label = "Home",
                Category = "test",
                Path = "/",
                ComponentPath = "./app/modules/home/lib/home.component",
                Component = "HomeComponent",
                Visible = true
              },
              new MP2WebAppRouterConfiguration
              {
                Name = "Heroes",
                Label = "Heroes",
                Category = "test",
                Path = "/heroes",
                ComponentPath = "./app/hero-list.component",
                Component = "HeroListComponent",
                Visible = true
              },
              new MP2WebAppRouterConfiguration
              {
                Name = "Movies",
                Label = "Movies",
                Category = "test",
                Path = "/movies/...",
                ComponentPath = "./app/modules/movies/lib/movies.component",
                Component = "MoviesComponent",
                Visible = true
              }
            }
          },
          new MP2WebAppRouterConfiguration
          {
            Name = "test",
            Label = "test",
            Visible = true,
            Pages = new List<MP2WebAppRouterConfiguration>
            {
              new MP2WebAppRouterConfiguration
              {
                Name = "CrisisCenter",
                Label = "CrisisCenter",
                Path = "/crisis-center",
                ComponentPath = "./app/crisis-list.component",
                Component = "CrisisListComponent",
                Visible = true
              }
            }
          },
          new MP2WebAppRouterConfiguration
          {
            Name = "Heroes",
            Label = "Heroes",
            Path = "/heroes",
            ComponentPath = "./app/hero-list.component",
            Component = "HeroListComponent",
            Visible = true
          }
        },
        MoviesPerRow = 5,
        MoviesPerQuery = 6
      };
    }

    #endregion
  }
}