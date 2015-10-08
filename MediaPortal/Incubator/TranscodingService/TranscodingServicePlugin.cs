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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using System.IO;
using MediaPortal.Common.PathManager;
using System.Xml;
using System;
using System.Threading;
using MediaPortal.Plugins.Transcoding.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class TranscodingServicePlugin : IPluginStateTracker
  {
    public static string TranscoderCachePath { get; private set; }
    public static long TranscoderMaximumCacheSizeInGB { get; private set; }
    public static long TranscodeMaximumCacheAgeInDays { get; private set; }
    public static int TranscoderMaximumThreads { get; private set; }
    public static int TranscoderTimeout { get; private set; }
    public static int HLSSegmentTimeInSeconds { get; private set; }
    public static string HLSSegmentFileTemplate { get; private set; }
    public static string SubtitleDefaultEncoding { get; private set; }
    public static string SubtitleDefaultLanguage { get; private set; }
    public static bool NvidiaHWAccelerationAllowed { get; private set; }
    public static bool IntelHWAccelerationAllowed { get; private set; }
    public static int NvidiaHWMaximumStreams { get; private set; }
    public static int IntelHWMaximumStreams { get; private set; }
    public static ReadOnlyCollection<VideoCodec> NvidiaHWSupportedCodecs
    {
      get
      {
        return _nvidiaCodecs.AsReadOnly();
      }
    }
    public static ReadOnlyCollection<VideoCodec> IntelHWSupportedCodecs 
    { 
      get 
      {
        return _intelCodecs.AsReadOnly(); 
      } 
    }

    private static List<VideoCodec> _intelCodecs = new List<VideoCodec>() {VideoCodec.Mpeg2, VideoCodec.H264, VideoCodec.H265};
    private static List<VideoCodec> _nvidiaCodecs = new List<VideoCodec>() {VideoCodec.H264, VideoCodec.H265};

    private const string SETTINGS_FILE = "MediaPortal.Plugins.Transcoding.Service.Settings.xml";
    private const string DEFAULT_TRANSCODE_CACHE_FOLDER = "MPTranscodes";

    public TranscodingServicePlugin()
    {
      TranscoderMaximumCacheSizeInGB = 0; //GB
      TranscodeMaximumCacheAgeInDays = 30; //Days
      TranscoderMaximumThreads = 0;
      TranscoderCachePath = Path.Combine(Path.GetTempPath(), DEFAULT_TRANSCODE_CACHE_FOLDER);
      TranscoderTimeout = 5000;
      HLSSegmentTimeInSeconds = 10;
      HLSSegmentFileTemplate = "segment%05d.ts";
      SubtitleDefaultEncoding = "";
      SubtitleDefaultLanguage = "";
      NvidiaHWAccelerationAllowed = false;
      NvidiaHWMaximumStreams = 2; //For Gforce GPU
      IntelHWAccelerationAllowed = false;
      IntelHWMaximumStreams = 0;
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadTranscodeSettings();
    }

    private void LoadTranscodeSettings()
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
            if (childNode.Name == "TranscoderCachePath")
            {
              TranscoderCachePath = childNode.InnerText;
              if (Directory.Exists(TranscoderCachePath) == false)
              {
                Directory.CreateDirectory(TranscoderCachePath);
              }
            }
            else if (childNode.Name == "TranscoderMaximumCacheSizeInGB")
            {
              TranscoderMaximumCacheSizeInGB = Convert.ToInt64(childNode.InnerText);
            }
            else if (childNode.Name == "TranscodeMaximumCacheAgeInDays")
            {
              TranscodeMaximumCacheAgeInDays = Convert.ToInt64(childNode.InnerText);
            }
            else if (childNode.Name == "TranscoderMaximumThreads")
            {
              TranscoderMaximumThreads = Convert.ToInt32(childNode.InnerText);
            }
            else if (childNode.Name == "TranscoderTimeout")
            {
              TranscoderTimeout = Convert.ToInt32(childNode.InnerText);
            }
            else if (childNode.Name == "HLSSegmentTimeInSeconds")
            {
              HLSSegmentTimeInSeconds = Convert.ToInt32(childNode.InnerText);
            }
            else if (childNode.Name == "HLSSegmentFileTemplate")
            {
              HLSSegmentFileTemplate = childNode.InnerText;
            }
            else if (childNode.Name == "SubtitleDefaultEncoding")
            {
              SubtitleDefaultEncoding = childNode.InnerText;
            }
            else if (childNode.Name == "SubtitleDefaultLanguage")
            {
              SubtitleDefaultLanguage = childNode.InnerText;
            }
            else if (childNode.Name == "IntelHWAccelerationAllowed")
            {
              IntelHWAccelerationAllowed = Convert.ToInt32(childNode.InnerText) > 0;
            }
            else if (childNode.Name == "IntelHWMaximumStreams")
            {
              IntelHWMaximumStreams = Convert.ToInt32(childNode.InnerText);
            }
            else if (childNode.Name == "IntelHWSupportedCodecs")
            {
              _intelCodecs.Clear();
              string[] codecs = childNode.InnerText.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
              foreach(string codec in codecs)
              {
                VideoCodec vCodec;
                if(Enum.TryParse<VideoCodec>(codec, out vCodec) == true)
                {
                  _intelCodecs.Add(vCodec);
                }
              }
            }
            else if (childNode.Name == "NvidiaHWAccelerationAllowed")
            {
              NvidiaHWAccelerationAllowed = Convert.ToInt32(childNode.InnerText) > 0;
            }
            else if (childNode.Name == "NvidiaHWMaximumStreams")
            {
              NvidiaHWMaximumStreams = Convert.ToInt32(childNode.InnerText);
            }
            else if (childNode.Name == "NvidiaHWSupportedCodecs")
            {
              _nvidiaCodecs.Clear();
              string[] codecs = childNode.InnerText.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
              foreach(string codec in codecs)
              {
                VideoCodec vCodec;
                if(Enum.TryParse<VideoCodec>(codec, out vCodec) == true)
                {
                  _nvidiaCodecs.Add(vCodec);
                }
              }
            }
          }
        }
      }
    }

    private void SaveTranscodeSettings()
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

        XmlElement elem = document.CreateElement("TranscoderCachePath");
        elem.InnerText = TranscoderCachePath;
        node.AppendChild(elem);
        elem = document.CreateElement("TranscoderMaximumCacheSizeInGB");
        elem.InnerText = Convert.ToString(TranscoderMaximumCacheSizeInGB);
        node.AppendChild(elem);
        elem = document.CreateElement("TranscodeMaximumCacheAgeInDays");
        elem.InnerText = Convert.ToString(TranscodeMaximumCacheAgeInDays);
        node.AppendChild(elem);
        elem = document.CreateElement("TranscoderMaximumThreads");
        elem.InnerText = Convert.ToString(TranscoderMaximumThreads);
        node.AppendChild(elem);
        elem = document.CreateElement("TranscoderTimeout");
        elem.InnerText = Convert.ToString(TranscoderTimeout);
        node.AppendChild(elem);
        elem = document.CreateElement("HLSSegmentTimeInSeconds");
        elem.InnerText = Convert.ToString(HLSSegmentTimeInSeconds);
        node.AppendChild(elem);
        elem = document.CreateElement("HLSSegmentFileTemplate");
        elem.InnerText = HLSSegmentFileTemplate;
        node.AppendChild(elem);
        elem = document.CreateElement("SubtitleDefaultEncoding");
        elem.InnerText = SubtitleDefaultEncoding;
        node.AppendChild(elem);
        elem = document.CreateElement("SubtitleDefaultLanguage");
        elem.InnerText = SubtitleDefaultLanguage;
        node.AppendChild(elem);
        elem = document.CreateElement("IntelHWAccelerationAllowed");
        elem.InnerText = Convert.ToString(IntelHWAccelerationAllowed ? 1 : 0);
        node.AppendChild(elem);
        elem = document.CreateElement("IntelHWMaximumStreams");
        elem.InnerText = Convert.ToString(IntelHWMaximumStreams);
        node.AppendChild(elem);
        elem = document.CreateElement("IntelHWSupportedCodecs");
        elem.InnerText = string.Join(",", IntelHWSupportedCodecs);
        node.AppendChild(elem);
        elem = document.CreateElement("NvidiaHWAccelerationAllowed");
        elem.InnerText = Convert.ToString(NvidiaHWAccelerationAllowed ? 1 : 0);
        node.AppendChild(elem);
        elem = document.CreateElement("NvidiaHWMaximumStreams");
        elem.InnerText = Convert.ToString(NvidiaHWMaximumStreams);
        node.AppendChild(elem);
        elem = document.CreateElement("NvidiaHWSupportedCodecs");
        elem.InnerText = string.Join(",", NvidiaHWSupportedCodecs);
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
      LoadTranscodeSettings();
    }

    public void Shutdown()
    {
      SaveTranscodeSettings();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
