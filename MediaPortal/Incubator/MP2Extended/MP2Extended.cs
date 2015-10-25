using System.Linq.Expressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Common.PathManager;
using System.IO;
using System.Xml;
using System;

namespace MediaPortal.Plugins.MP2Extended
{
  public class MP2Extended : IPluginStateTracker
  {
    private const string SETTINGS_FILE = "MediaPortal.Plugins.MP2Extended.Settings.xml";

    public static bool TranscodingAllowed { get; private set; }
    public static bool HardcodedSubtitlesAllowed { get; private set; }

    public MP2Extended()
    {
      TranscodingAllowed = true;
      HardcodedSubtitlesAllowed = true;
    }
    
    private void StartUp()
    {
      ProfileManager.LoadProfiles(false);
      ProfileManager.LoadProfiles(true);

      Logger.Debug("MP2Extended: Registering HTTP resource access module");
      ServiceRegistration.Get<IResourceServer>().AddHttpModule(new MainRequestHandler());
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

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadSettings();
      StartUp();
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
      MainRequestHandler.Shutdown();
    }

    #endregion IPluginStateTracker

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
