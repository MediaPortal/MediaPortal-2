#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Collections;
using MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data
{
  public class ScriptableScraper
  {
    private static ILogger Logger => ServiceRegistration.Get<ILogger>();
    
    #region Properties

    // Friendly name for the script.
    public string Name { get; protected set; }
    // Description of the script. For display purposes.
    public string Description { get; protected set; }
    // Description of the script. For display purposes.
    public string Author { get; protected set; }
    // Friendly readable version number.
    public string Version
    {
      get { return VersionMajor + "." + VersionMinor + "." + VersionPoint; }
    }
    // Major version number of script.
    public int VersionMajor { get; protected set; }
    // Minor version number of script.
    public int VersionMinor { get; protected set; }
    // Point version number of script.
    public int VersionPoint { get; protected set; }
    // Friendly readable version number.
    public DateTime Published
    {
      get { return new DateTime(PublishedYear, PublishedMonth, PublishedDay); }
    }
    // Major version number of script.
    public int PublishedDay { get; protected set; }
    // Minor version number of script.
    public int PublishedMonth { get; protected set; }
    // Point version number of script.
    public int PublishedYear { get; protected set; }
    // Unique ID number for the script.
    public int ID { get; protected set; }
    // The type(s) of script. Used for categorization purposes. This basically defines
    // which predefined actions are implemented.
    public StringList ScriptTypes { get; protected set; }
    // The type of category for the metadata returned by the script. Only specified if another category than the default is needed
    public string Category { get; protected set; }
    // The language supported by the script. Used for categorization and informational purposes.
    public string Language { get; protected set; }
    // Returns true if the script loaded successfully.
    public bool LoadSuccessful { get; protected set; }
    public string Script
    {
      get { return xml.OuterXml; }
    }
    // Gets the cache dictionary.
    public DiskCachedDictionary<string, string> Cache
    {
      get
      {
        return _cache;
      }
    }

    #endregion

    private XmlDocument xml;
    private Dictionary<string, ScraperNode> actionNodes = null;
    private DiskCachedDictionary<string, string> _cache = new DiskCachedDictionary<string, string>();

    public ScriptableScraper(string xmlScriptFile)
    {
      LoadSuccessful = true;
      string fileName = Path.GetFileName(xmlScriptFile);

      try
      {
        Logger.Debug("ScriptableScraperProvider: Loading scriptable scraper XML file {0}", fileName);

        xml = new XmlDocument();
        xml.Load(xmlScriptFile);

        if (!xml.DocumentElement.Name.Equals("ScriptableScraper", StringComparison.InvariantCultureIgnoreCase))
        {
          Logger.Error("ScriptableScraperProvider: Invalid root node (expecting <ScriptableScraper>) in scriptable scraper XML file {0}", fileName);
          return;
        }
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing scriptable scraper XML file {0}", fileName, e);
        LoadSuccessful = false;
        return;
      }

      // try to grab info from the details node
      LoadDetails();
      if (!LoadSuccessful)
        return;

      Logger.Debug("ScriptableScraperProvider: Successfully parsed scriptable scraper: {0} ({1}) Version {2}", Name, ID, Version);
    }

    private bool LoadDetails()
    {
      try
      {
        XmlNode detailsNode = xml.DocumentElement.SelectNodes("child::details")[0];
        foreach (XmlNode currNode in detailsNode.ChildNodes)
        {
          if (currNode.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase))
          {
            Name = currNode.InnerText;
          }
          else if (currNode.Name.Equals("author", StringComparison.InvariantCultureIgnoreCase))
          {
            Author = currNode.InnerText;
          }
          else if (currNode.Name.Equals("description", StringComparison.InvariantCultureIgnoreCase))
          {
            Description = currNode.InnerText;
          }
          else if (currNode.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
          {
            ID = int.Parse(currNode.InnerText);
          }
          else if (currNode.Name.Equals("version", StringComparison.InvariantCultureIgnoreCase))
          {
            VersionMajor = int.Parse(currNode.Attributes["major"].Value);
            VersionMinor = int.Parse(currNode.Attributes["minor"].Value);
            VersionPoint = int.Parse(currNode.Attributes["point"].Value);
          }
          else if (currNode.Name.Equals("type", StringComparison.InvariantCultureIgnoreCase))
          {
            ScriptTypes = new StringList(currNode.InnerText);
          }
          else if (currNode.Name.Equals("category", StringComparison.InvariantCultureIgnoreCase))
          {
            Category = currNode.InnerText;
          }
          else if (currNode.Name.Equals("language", StringComparison.InvariantCultureIgnoreCase))
          {
            Language = currNode.InnerText;
          }
          else if (currNode.Name.Equals("published", StringComparison.InvariantCultureIgnoreCase))
          {
            PublishedDay = int.Parse(currNode.Attributes["day"].Value);
            PublishedMonth = int.Parse(currNode.Attributes["month"].Value);
            PublishedYear = int.Parse(currNode.Attributes["year"].Value);
          }
        }
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing <details> node for scriptable scraper", e);
        LoadSuccessful = false;
      }

      return true;
    }

    private void LoadActionNodes()
    {
      Logger.Debug("ScriptableScraperProvider: Parsing action nodes on scriptable scraper: {0}", Name);
      actionNodes = new Dictionary<string, ScraperNode>();
      foreach (XmlNode currAction in xml.DocumentElement.SelectNodes("child::action"))
      {
        ActionNode newNode = (ActionNode)ScraperNode.Load(currAction, this);
        if (newNode != null && newNode.LoadSuccess)
          actionNodes[newNode.Name] = newNode;
        else
        {
          Logger.Error("ScriptableScraperProvider: Error loading action node: {0}", currAction.OuterXml);
          LoadSuccessful = false;
        }
      }
    }

    public Dictionary<string, string> Execute(string action, Dictionary<string, string> input)
    {
      if (!LoadSuccessful)
        return null;

      if (actionNodes == null)
      {
        LoadActionNodes();
        if (!LoadSuccessful)
          return null;
      }

      if (actionNodes.ContainsKey(action))
      {
        // transcribe the dictionary because we don't want to modify the input
        Dictionary<string, string> workingVariables = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> currPair in input)
          workingVariables[currPair.Key] = currPair.Value;

        actionNodes[action].Execute(workingVariables);
        return workingVariables;
      }
      return null;
    }

  }
}
