#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.ConfigurationManagement
{
  /// <summary>
  /// The default <see cref="IConfigurationManager"/> service implementation.
  /// </summary>
  public class ConfigurationManager : IConfigurationManager
  {
    #region Protected fields

    /// <summary>
    /// Tree which holds the configuration nodes with our config objects.
    /// </summary>
    protected ConfigurationTree _tree = null;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of ConfigurationManager. Before usage, <see cref="Initialize"/> has to be
    /// called.
    /// </summary>
    public ConfigurationManager() { }

    #endregion

    #region Protected methods

    /// <summary>
    /// Searches for a configuration node which best matches the specified <paramref name="searchMatcher"/>
    /// in the given <paramref name="startNode"/> and subnodes.
    /// </summary>
    /// <param name="startNode">Node to search through.</param>
    /// <param name="searchMatcher">Pattern which does the matching.</param>
    /// <param name="bestMatch">Location of the best matching configuration node for the
    /// search value.</param>
    /// <param name="bestScore">Score of the best match.</param>
    /// <returns>Enumeration of matching configuration nodes.</returns>
    protected static ICollection<IConfigurationNode> Search(IConfigurationNode startNode, ConfigObjectSearchMatcher searchMatcher,
        out IConfigurationNode bestMatch, out float bestScore)
    {
      bestMatch = null;
      List<IConfigurationNode> result = new List<IConfigurationNode>();
      // Check current node (the node may have no config object assigned if it is the root node or
      // if we have a section in use which was not defined)
      bestScore = startNode.ConfigObj == null ? 0 : searchMatcher.CalculateMatchQuality(startNode.ConfigObj);
      if (bestScore > 0)
      {
        bestMatch = startNode;
        result.Add(startNode);
      }
      // Check children
      foreach (ConfigurationNode childNode in startNode.ChildNodes)
      {
        IConfigurationNode match;
        float score;
        result.AddRange(Search(childNode, searchMatcher, out match, out score));
        if (score > bestScore)
        {
          bestMatch = match;
          bestScore = score;
        }
      }
      return result;
    }

    protected void CheckInitialized()
    {
      if (_tree == null)
        throw new IllegalCallException("The configuration manager is not initialized");
    }

    #endregion

    #region IConfigurationManager implementation

    public void Initialize()
    {
      Dispose();
      _tree = new ConfigurationTree();
    }

    public void Dispose()
    {
      if (_tree == null)
        return; // Already disposed
      _tree.Dispose();
      _tree = null;
    }

    public void RemoveAllConfigurationData(bool user, bool global)
    {
      ServiceScope.Get<ISettingsManager>().RemoveAllSettingsData(user, global);
      Dispose();
    }

    public void Load()
    {
      Initialize();
      // We will load our structures lazily
    }

    public void Save()
    {
      CheckInitialized();
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      settingsManager.StartBatchUpdate();
      _tree.SaveSettings();
      settingsManager.EndBatchUpdate();
    }

    public IConfigurationNode GetNode(string nodeLocation)
    {
      CheckInitialized();
      // Do some basic checks and split the location to an array
      if (string.IsNullOrEmpty(nodeLocation))
        throw new ArgumentNullException("nodeLocation");
      IConfigurationNode result;
      if (!_tree.FindNode(nodeLocation, out result))
        throw new NodeNotFoundException("Configuration node with location '" + nodeLocation + "' not found");
      return result;
    }

    public SearchResult Search(string searchText)
    {
      CheckInitialized();
      float score;
      ConfigObjectSearchMatcher searchMatcher = new ConfigObjectSearchMatcher(searchText);
      IConfigurationNode bestMatch;
      ICollection<IConfigurationNode> matches = Search(_tree.RootNode, searchMatcher, out bestMatch, out score);
      return new SearchResult(matches, bestMatch);
    }

    #endregion
  }
}
