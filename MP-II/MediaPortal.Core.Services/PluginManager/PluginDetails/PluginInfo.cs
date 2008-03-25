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
using MediaPortal.Core.PluginManager;
using MediaPortal.Services.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  public sealed class PluginInfo : IPluginInfo
  {
    #region Variables
    PluginProperties _properties;
    List<PluginRuntime> _runtimes;
    string _fileName;
    PluginManifest _manifest;
    Dictionary<string, ExtensionPath> _extensionPaths;
    Dictionary<string, object> _instances;
    bool _enabled;
  	bool _loaded;

  	//AddInAction _action = AddInAction.Disable;
    //List<string> bitmapResources = new List<string>();
    //List<string> stringResources = new List<string>();
    //string customErrorMessage;
    //static bool hasShownErrorMessage = false;
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
    	_loaded = false;
    }
    #endregion

    ///// <summary>
    ///// Gets the message of a custom load error. Used only when AddInAction is set to CustomError.
    ///// Settings this property to a non-null value causes Enabled to be set to false and
    ///// Action to be set to AddInAction.CustomError.
    ///// </summary>
    //public string CustomErrorMessage {
    //  get {
    //    return customErrorMessage;
    //  }
    //  internal set {
    //    if (value != null) {
    //      Enabled = false;
    //      //Action = AddInAction.CustomError;
    //    }
    //    customErrorMessage = value;
    //  }
    //}

    ///// <summary>
    ///// Action to execute when the application is restarted.
    ///// </summary>
    //public AddInAction Action {
    //  get {
    //    return action;
    //  }
    //  set {
    //    action = value;
    //  }
    //}

    #region Properties
    // Public Properties
    public List<PluginRuntime> Runtimes
    {
      get { return _runtimes; }
    }

    public Version Version
    {
      get { return _manifest.Version; }
    }

    public string FileName
    {
      get { return _fileName; }
    }

    public string PluginPath
    {
      get { return Path.GetDirectoryName(_fileName); }
    }

    public string Name
    {
      get { return _properties["name"]; }
    }

    public string Id
    {
      get { return _properties["id"]; }
    }

    public PluginManifest Manifest
    {
      get { return _manifest; }
    }

    public Dictionary<string, ExtensionPath> ExtensionPaths
    {
      get { return _extensionPaths; }
    }

    public PluginProperties Properties
    {
      get { return _properties; }
    }

    //public List<string> BitmapResources {
    //  get {
    //    return bitmapResources;
    //  }
    //  set {
    //    bitmapResources = value;
    //  }
    //}

    //public List<string> StringResources {
    //  get {
    //    return stringResources;
    //  }
    //  set {
    //    stringResources = value;
    //  }
    //}

    public bool Enabled
    {
      get
      {
        return _enabled;
      }
      internal set
      {
        _enabled = value;
        //this.Action = value ? AddInAction.Enable : AddInAction.Disable;
      }
    }

		public bool Loaded
		{
			get
			{
				return _loaded;
			}
			set
			{
				_loaded = value;
			}
		}

    #endregion

    #region Public Methods
    public object CreateObject(string className)
    {
      if (!_instances.ContainsKey(_manifest.Identity))
      {
        ServiceScope.Get<ILogger>().Info("Creating plugin instance: " + _manifest.Identity);
        IPlugin pluginInstance = CreateInstance(_manifest.Identity) as IPlugin;
        if (pluginInstance != null)
        {
          _instances.Add(_manifest.Identity, (object)pluginInstance);
          ServiceScope.Get<ILogger>().Debug("Initialising plugin: {0}", _properties["name"]);
          pluginInstance.Initialize(_properties["name"]);  
        }
      }

      if (!_instances.ContainsKey(className))
      {
        ServiceScope.Get<ILogger>().Info("Creating plugin class instance: " + className);
        object instance = CreateInstance(className);
        if (instance != null)
          _instances.Add(className, instance);
      }

      return _instances[className];
    }

    public ExtensionPath GetExtensionPath(string pathName)
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
      //if (hasShownErrorMessage) {
      //  ServiceScope.Get<ILogger>().Error("Cannot create object: " + className);
      //} else {
      //  hasShownErrorMessage = true;
      //  //MessageService.ShowError("Cannot create object: " + className + "\nFuture missing objects will not cause an error message.");
      //}
      return null;
    }
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
            //case "StringResources":
            //case "BitmapResources":
            //  if (reader.AttributeCount != 1)
            //  {
            //    throw new PluginLoadException("BitmapResources requires ONE attribute.");
            //  }

            //  string filename = reader.GetAttribute("file"); // StringParser.Parse(reader.GetAttribute("file"));

            //  //if(reader.LocalName == "BitmapResources")
            //  //{
            //  //  addIn.BitmapResources.Add(filename);
            //  //}
            //  //else
            //  //{
            //  //  addIn.StringResources.Add(filename);
            //  //}
            //  break;
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
                throw new PluginLoadException("Unknown add-in file.");
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

    #region <Base class> Overloads
    public override string ToString()
    {
      if (!String.IsNullOrEmpty(Id))
        return "[Plugin: " + Name + "." + Id + "]";
      else
        return "[Plugin: " + Name + "]";
    }
    #endregion
  }
}
