#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  /// <summary>
  /// PluginInfo contain all the information about a plugin
  /// </summary>
  public sealed class PluginInfo : IPluginInfo
  {
    #region Variables
    PluginProperties _properties;
    List<PluginRuntime> _runtimes;
    string _fileName;
    PluginManifest _manifest;
    Dictionary<string, ExtensionPath> _extensionPaths;
    Dictionary<string, object> _instances;
    PluginState _state;
    #endregion

    #region Constructors/Destructors
    internal PluginInfo()
    {
      _properties = new PluginProperties();
      _runtimes = new List<PluginRuntime>();
      _fileName = null;
      _manifest = new PluginManifest();
      _extensionPaths = new Dictionary<string, ExtensionPath>();
      _instances = new Dictionary<string, object>();
      _state = PluginState.Disabled;
    }
    #endregion

    #region Properties
    // Public Properties
    internal List<PluginRuntime> Runtimes
    {
      get { return _runtimes; }
    }

    internal string FileName
    {
      get { return _fileName; }
    }

    internal PluginManifest Manifest
    {
      get { return _manifest; }
    }

    internal Dictionary<string, ExtensionPath> ExtensionPaths
    {
      get { return _extensionPaths; }
    }

    internal PluginProperties Properties
    {
      get { return _properties; }
    }
    #endregion

    #region Public Methods
    internal ExtensionPath GetExtensionPath(string pathName)
    {
      if (!_extensionPaths.ContainsKey(pathName))
      {
        return _extensionPaths[pathName] = new ExtensionPath(pathName, this);
      }
      return _extensionPaths[pathName];
    }
    #endregion

    #region Private Methods

    private object CreateInstance(string className)
    {
      foreach (PluginRuntime runtime in _runtimes)
      {
        object o = runtime.CreateInstance(className);
        if (o != null)
        {
          return o;
        }
      }
      ServiceScope.Get<ILogger>().Error("Cannot create object: " + className);

      return null;
    }

    private void SendMessage(PluginMessaging.NotificationType type)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PluginMessaging.Queue);
      QueueMessage msg = new QueueMessage();
      msg.MessageData[PluginMessaging.PluginName] = Name;
      msg.MessageData[PluginMessaging.Notification] = type;

      queue.Send(msg);
    }

    #endregion

    #region IPluginInfo Members
    #region Properties
    public string Name
    {
      get { return _properties["name"]; }
    }

    public Version Version
    {
      get { return _manifest.Version; }
    }

    public DirectoryInfo PluginPath
    {
      get { return new DirectoryInfo(Path.GetDirectoryName(_fileName)); }
    }

    public PluginState State
    {
      get { return _state; }
      internal set { _state = value; }
    }

    #endregion

    #region Public Methods


    /// <remarks>
    /// All instances are only created once and stored in a cache (_instances dictionary).
    /// </remarks>
    public object CreateObject(string className)
    {

      // Check to see if the IPlugin class has been instanciated.
      // If not an instance of the IPlugin class is created and added to the instance cache.
      // The initialise method of the IPlugin class is executed and plugin state is set to Initialised
      if (!_instances.ContainsKey(_manifest.Identity))
      {
        ServiceScope.Get<ILogger>().Debug("Creating plugin instance: " + _manifest.Identity);
        IPlugin pluginInstance = CreateInstance(_manifest.Identity) as IPlugin;
        if (pluginInstance != null)
        {
          _instances.Add(_manifest.Identity, (object)pluginInstance);
          ServiceScope.Get<ILogger>().Info("Initialising plugin: {0}", _properties["name"]);
          pluginInstance.Initialise();
          _state = PluginState.Initialised;
          SendMessage(PluginMessaging.NotificationType.OnPluginInitialise);
        }
      }

      // Check to see if this item has already been instanciated
      // if not an instance of the item is created and added to the instance cache
      if (!_instances.ContainsKey(className))
      {
        ServiceScope.Get<ILogger>().Debug("Creating plugin item instance: " + className);
        object instance = CreateInstance(className);
        if (instance != null)
          _instances.Add(className, instance);
      }

      // Retrun the item instance from the instance cache
      return _instances[className];
    }
    #endregion
    #endregion

    #region Public static Methods

    static void SetupPlugin(XmlReader reader, PluginInfo plugin, string hintPath)
    {
      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.Element && reader.IsStartElement())
        {
          switch (reader.LocalName)
          {
            case "Runtime":
              if (!reader.IsEmptyElement)
              {
                PluginRuntime.ReadSection(reader, plugin, hintPath);
              }
              break;
            case "Include":
              if (reader.AttributeCount != 1)
              {
                throw new PluginLoadException("Include requires ONE attribute.");
              }
              if (!reader.IsEmptyElement)
              {
                throw new PluginLoadException("Include nodes must be empty!");
              }
              if (hintPath == null)
              {
                throw new PluginLoadException("Cannot use include nodes when hintPath was not specified (e.g. when AddInManager reads a .addin file)!");
              }
              string fileName = Path.Combine(hintPath, reader.GetAttribute(0));
              XmlReaderSettings xrs = new XmlReaderSettings();
              xrs.ConformanceLevel = ConformanceLevel.Fragment;
              using (XmlReader includeReader = XmlTextReader.Create(fileName, xrs))
              {
                SetupPlugin(includeReader, plugin, Path.GetDirectoryName(fileName));
              }
              break;
            case "Register":
              if (reader.AttributeCount != 1)
              {
                throw new PluginLoadException("Import node requires ONE attribute.");
              }
              string location = reader.GetAttribute(0);
              ExtensionPath extensionPath = plugin.GetExtensionPath(location);
              if (!reader.IsEmptyElement)
              {
                ExtensionPath.SetUp(extensionPath, reader, "Register");
              }
              break;
            case "Manifest":
              plugin.Manifest.ReadManifestSection(reader, hintPath);
              break;
            default:
              throw new PluginLoadException("Unknown root path node:" + reader.LocalName);
          }
        }
      }
    }

    public static PluginInfo Load(TextReader textReader)
    {
      return Load(textReader, null);
    }

    public static PluginInfo Load(TextReader textReader, string hintPath)
    {
      PluginInfo plugin = new PluginInfo();
      using (XmlTextReader reader = new XmlTextReader(textReader))
      {
        while (reader.Read())
        {
          if (reader.IsStartElement())
          {
            switch (reader.LocalName)
            {
              case "Plugin":
                plugin._properties = PluginProperties.ReadFromAttributes(reader);
                SetupPlugin(reader, plugin, hintPath);
                break;
              default:
                throw new PluginLoadException("Unknown plugin file.");
            }
          }
        }
      }
      return plugin;
    }

    public static PluginInfo Load(string fileName)
    {
      try
      {
        using (TextReader textReader = File.OpenText(fileName))
        {
          PluginInfo plugin = Load(textReader, Path.GetDirectoryName(fileName));
          plugin._fileName = fileName;
          return plugin;
        }
      }
      catch (Exception e)
      {
        throw new PluginLoadException("Can't load " + fileName, e);
      }
    }

    #endregion

    #region Base Overrides

    public override string ToString()
    {
      return "[Plugin: " + Name + "]";
    }

    #endregion
  }
}
