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
using System.Reflection;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AspNetWebApi.Utils;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers.ServerPlugins
{
  /// <summary>
  /// AspNet MVC Controller for Server Plugins
  /// </summary>
  [Route("v1/Server/[Controller]")]
  public class ServerPluginsController : Controller
  {
    #region Private fields

    const string GLOBAL_SETTINGS_LOCATION = "<CONFIG>";
    private readonly ILogger _logger;
    private readonly string[] _badWords = new[] { "settings" };

    #endregion

    #region Constructor

    public ServerPluginsController(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger<ServerControllerController>();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// GET /api/v1/Server/ServerPlugins/PluginSettings
    /// </summary>
    [HttpGet("PluginSettings")]
    public List<PluginSettingsDescription> GetPluginSettings()
    {
      string configPath = ServiceRegistration.Get<IPathManager>().GetPath(GLOBAL_SETTINGS_LOCATION);
      string[] configFiles = Directory.GetFiles(configPath, "*.xml");

      Dictionary<string, string> configDictionary = new Dictionary<string, string>();
      foreach (var config in configFiles)
      {
        string fileName = Path.GetFileNameWithoutExtension(config);
        if (fileName != null && !configDictionary.ContainsKey(fileName))
          configDictionary.Add(fileName, config);
      }

      List<PluginSettingsDescription> availableSettings = new List<PluginSettingsDescription>();

      foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (Type type in a.GetTypes())
        {
          if (configDictionary.ContainsKey(type.FullName))
          {
            availableSettings.Add(new PluginSettingsDescription {
              Id = type.FullName,
              Name = FormatSettingName(type.FullName.Split('.').Last())
            });
          }
        }
      }

      return availableSettings;
    }

    /// <summary>
    /// GET /api/v1/Server/ServerPlugins/SettingProperties/[id]
    /// </summary>
    [HttpGet("SettingProperties/{id}")]
    public List<SettingDescription> GetSettingProperties(string id)
    {
      List<SettingDescription> output = new List<SettingDescription>();

      Type type;
      if (GetType(id, out type))
      {
        dynamic settings = ServiceRegistration.Get<ISettingsManager>().Load(type);
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
          var name = property.Name;
          var value = property.GetValue(settings, null);
          var propertyType = property.PropertyType.Name;

          output.Add(new SettingDescription
          {
            Name = name,
            Value = value,
            Type = propertyType
          });
        }
      }

      return output;
    }

    /// <summary>
    /// PUT /api/v1/Server/ServerPlugins/SettingProperty/[id]/[setting]
    /// </summary>
    /// <remarks>
    /// From the documentation: "At most one parameter is allowed to read from the message body."
    /// http://www.asp.net/web-api/overview/formats-and-model-binding/parameter-binding-in-aspnet-web-api
    /// </remarks>
    /// <returns>true on success otherwise false</returns>
    [HttpPut("SettingProperty/{id}/{setting}")]
    public bool PutSettingProperty(string id, string setting, [FromBody]string value)
    {
      bool success = false;

      Type type;
      if (GetType(id, out type))
      {
        dynamic settings = ServiceRegistration.Get<ISettingsManager>().Load(type);

        try
        {
          var properties = type.GetProperties().ToList();
          int index = properties.FindIndex(x => x.Name == setting);
          if (index == -1)
          {
            _logger.LogWarning($"A setting with the name '{setting}' wasn't found!");
            return success;
          }

          var property = properties[index];
          object convertedValue;

          success = value.TryParse(property.PropertyType, out convertedValue);

          if (success)
          {
            property.SetValue(settings, convertedValue);
            ServiceRegistration.Get<ISettingsManager>().Save(settings);
          }
        }
        catch (Exception ex)
        {
          _logger.LogError("Error while saving Service setting", ex);
        }
      }

      return success;
    }

    #endregion

    #region Private methods

    private string FormatSettingName(string settingName)
    {
      string output = settingName;
      foreach (var badWord in _badWords)
      {
        var regex = new Regex(badWord, RegexOptions.IgnoreCase);
        output = regex.Replace(output, "");
      }

      // Add spaces before capital letters
      output = Regex.Replace(output, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");

      return output;
    }

    private bool GetType(string typeStr, out Type typeOut)
    {
      typeOut = null;
      foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (Type type in a.GetTypes())
        {
          if (type.FullName == typeStr)
          {
            typeOut = type;
            return true;
          }
        }
      }

      return false;
    }

    #endregion
  }
}
