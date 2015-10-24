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

using MediaPortal.Backend.BackendServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using System.IO;
using MediaPortal.Common.PathManager;
using System.Xml;
using System;
using System.Threading;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MediaPortal.Plugins.MediaServer
{
  public class MediaServerPlugin : IPluginStateTracker
  {
    private readonly UPnPMediaServerDevice _device;
    /// <summary>
    /// Tracks all UPnP Rootdevices available in the Network
    /// </summary>
    public static UPnPDeviceTracker Tracker;

    public const string DEVICE_UUID = "45F2C54D-8C0A-4736-AA04-E6F91CD45457";

    private const string SETTINGS_FILE = "MediaPortal.Plugins.MediaServer.Settings.xml";

    public static bool TranscodingAllowed { get; private set; }
    public static bool HardcodedSubtitlesAllowed { get; private set; }

    public MediaServerPlugin()
    {
      _device = new UPnPMediaServerDevice(DEVICE_UUID.ToLower());
      Tracker = new UPnPDeviceTracker();
      Tracker.Start();

      TranscodingAllowed = true;
      HardcodedSubtitlesAllowed = true;
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      Logger.Debug("MediaServerPlugin: Adding UPNP device as a root device");
      ServiceRegistration.Get<IBackendServer>().UPnPBackendServer.AddRootDevice(_device);

      Logger.Debug("MediaServerPlugin: Registering DLNA HTTP resource access module");
      ServiceRegistration.Get<IResourceServer>().AddHttpModule(new DlnaResourceAccessModule());

      LoadSettings();

      ProfileManager.LoadProfiles(false);
      ProfileManager.LoadProfiles(true);
      ProfileManager.LoadProfileLinks();
      ProfileManager.LoadPreferredLanguages();
    }

    private void LoadSettings()
    {
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string dataPath = pathManager.GetPath("<CONFIG>");
      string settingsFile = Path.Combine(dataPath, SETTINGS_FILE);
      if (File.Exists(settingsFile) == true)
      {
        XmlDocument document = new XmlDocument();
        document.Load(settingsFile);
        XmlNode configNode = document.SelectSingleNode("Configuration");
        XmlNode node = null;
        if (configNode != null)
        {
          node = configNode.SelectSingleNode("Transcoding");
        }
        if (node != null)
        {
          foreach (XmlNode childNode in node.ChildNodes)
          {
            if (childNode.Name == "TranscodingAllowed")
            {
              TranscodingAllowed = Convert.ToInt32(childNode.InnerText) > 0;
            }
            else if (childNode.Name == "HardcodedSubtitlesAllowed")
            {
              HardcodedSubtitlesAllowed = Convert.ToInt32(childNode.InnerText) > 0;
            }
          }
        }
      }
    }

    private void SaveSettings()
    {
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string dataPath = pathManager.GetPath("<CONFIG>");
      string settingsFile = Path.Combine(dataPath, SETTINGS_FILE);
      XmlDocument document = new XmlDocument();
      if (File.Exists(settingsFile) == true)
      {
        document.Load(settingsFile);
      }
      XmlNode configNode = document.SelectSingleNode("Configuration");
      XmlNode node = null;
      if (configNode != null)
      {
        node = configNode.SelectSingleNode("Transcoding");
        if (node == null)
        {
          node = document.CreateElement("Transcoding");
          configNode.AppendChild(node);
        }
      }
      else
      {
        configNode = document.CreateElement("Configuration");
        document.AppendChild(configNode);
        node = document.CreateElement("Transcoding");
        configNode.AppendChild(node);
      }
      if (node != null)
      {
        node.RemoveAll();

        XmlElement elem = document.CreateElement("TranscodingAllowed");
        elem.InnerText = Convert.ToString(TranscodingAllowed ? 1 : 0);
        node.AppendChild(elem);
        elem = document.CreateElement("HardcodedSubtitlesAllowed");
        elem.InnerText = Convert.ToString(HardcodedSubtitlesAllowed ? 1 : 0);
        node.AppendChild(elem);
      }

      XmlWriterSettings settings = new XmlWriterSettings();
      settings.Indent = true;
      settings.IndentChars = "\t";
      settings.NewLineChars = Environment.NewLine;
      settings.NewLineHandling = NewLineHandling.Replace;
      using (XmlWriter writer = XmlWriter.Create(settingsFile, settings))
      {
        document.Save(writer);
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
      LoadSettings();
    }

    public void Shutdown()
    {
      SaveSettings();
      ProfileManager.SavePreferredLanguages();
      DlnaResourceAccessModule.Shutdown();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
