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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Plugins.AspNetWebApi;
using MediaPortal.Plugins.MP2Web.Configuration;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Web.Controllers
{
  /// <summary>
  /// AspNet MVC Controller for Trailers
  /// </summary>
  [Route("/api/[Controller]")]
  public class ConfigurationController : Controller
  {
    #region Private fields

    const string MODULES_PATH = "./wwwroot/app/modules";
    const string MODULE_DEFINITION_FILE = "module.json";
    const string MENU_DEFINITION_FILE = "menu.json";
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
    [HttpGet]
    public MP2WebAppConfiguration Get()
    {
      var webApiPort = ServiceRegistration.Get<IAspNetWebApiService>().Port;
      
      return new MP2WebAppConfiguration
      {
        WebApiUrl = "http://" + HttpContext.Request.Host.Value.Split(':')[0] + ":" + webApiPort,
        Routes = GenerateRoutes(),
        MoviesPerRow = 5,
        MoviesPerQuery = 6,
        DefaultEpgGroupId = 1
      };
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Iterates through all modules and looks for a module.json file
    /// If found it trys to parse it
    /// </summary>
    /// <returns></returns>
    private List<MP2WebAppRouterConfiguration> GenerateRoutes()
    {
      List<MP2WebAppRouterConfiguration> routes = new List<MP2WebAppRouterConfiguration>();
      Dictionary<Guid, List<MP2WebAppRouterConfiguration>> routeDictionary = new Dictionary<Guid, List<MP2WebAppRouterConfiguration>>();

      try
      {
        // Get all modules and go through each one
        string[] modules = Directory.GetDirectories(Path.Combine(MP2WebService.ASSEMBLY_PATH, MODULES_PATH));
        foreach (var module in modules)
        {
          try
          {
            // If the module has a definition file, process it
            string moduleDefinitionPath = Path.Combine(module, MODULE_DEFINITION_FILE);
            if (System.IO.File.Exists(moduleDefinitionPath))
            {
              List<MP2WebModuleDefinition> moduleDefinitions = JsonConvert.DeserializeObject<List<MP2WebModuleDefinition>>(System.IO.File.ReadAllText(moduleDefinitionPath));
              foreach (var moduleDefinition in moduleDefinitions)
              {
                // create Route
                MP2WebAppRouterConfiguration route = new MP2WebAppRouterConfiguration
                {
                  Name = moduleDefinition.Name,
                  Label = moduleDefinition.Label,
                  Path = moduleDefinition.Path,
                  Category = moduleDefinition.Category,
                  Priority = moduleDefinition.Priority,
                  Component = moduleDefinition.Component,
                  ComponentPath = moduleDefinition.ComponentPath,
                  Visible = moduleDefinition.Visible
                };

                // if "Menu" == null it is a direct link in the NavigationBar
                if (moduleDefinition.Menu == null)
                {
                  routes.Add(route);
                }
                else
                {
                  if (!routeDictionary.ContainsKey(moduleDefinition.Menu.Value))
                  {
                    routeDictionary.Add(moduleDefinition.Menu.Value, new List<MP2WebAppRouterConfiguration>());
                  }

                  routeDictionary[moduleDefinition.Menu.Value].Add(route);
                }
              }
            }
          }
          catch (Exception e)
          {
            _logger.LogError("Error accessing dir", e);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Error generating Routes", ex);
      }

      routes.AddRange(LoadMenuDefinitionAndAddSubpages(routeDictionary));
      // Sort
      routes = routes.OrderBy(x => x.Priority).ToList();

      return routes;
    }

    /// <summary>
    /// Gets the menu items from the definition file and adds the routes to each menu point
    /// </summary>
    /// <param name="routeDictionary"></param>
    /// <returns></returns>
    private List<MP2WebAppRouterConfiguration> LoadMenuDefinitionAndAddSubpages(Dictionary<Guid, List<MP2WebAppRouterConfiguration>> routeDictionary)
    {
      List<MP2WebAppRouterConfiguration> routes = new List<MP2WebAppRouterConfiguration>();

      try
      {
        string menuDefinitionPath = Path.Combine(MP2WebService.ASSEMBLY_PATH, MODULES_PATH, MENU_DEFINITION_FILE);
        if (System.IO.File.Exists(menuDefinitionPath))
        {
          List<MP2WebMenuDefinition> menuDefinitions = JsonConvert.DeserializeObject<List<MP2WebMenuDefinition>>(System.IO.File.ReadAllText(menuDefinitionPath));

          foreach (var menuDefinition in menuDefinitions)
          {
            List<MP2WebAppRouterConfiguration> pages = null;
            if (routeDictionary.ContainsKey(menuDefinition.Id))
            {
              pages = routeDictionary[menuDefinition.Id].OrderBy(r => r.Priority).ToList();
            }

            routes.Add(new MP2WebAppRouterConfiguration
            {
              Name = menuDefinition.Name,
              Label = menuDefinition.Label,
              Path = menuDefinition.Path,
              Priority = menuDefinition.Priority,
              Visible = menuDefinition.Visible,
              Pages = pages
            });
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Error generating Menu", ex);
      }

      return routes;
    }

    #endregion
  }
}