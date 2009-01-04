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
using MediaPortal.Core;
using MediaPortal.Core.Registry;
using MediaPortal.Core.Settings;

namespace MediaPortal.Configuration.ConfigurationManagement
{
  /// <summary>
  /// Configuration tree containing <see cref="ConfigurationNode"/> instances.
  /// </summary>
  public class ConfigurationTree : IDisposable
  {
    #region Protected fields

    protected ConfigurationNode _root;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ConfigurationTree class.
    /// </summary>
    public ConfigurationTree()
    {
      _root = new ConfigurationNode();
    }

    #endregion

    #region Protected methods

    protected static void DisposeNodeAction(IConfigurationNode node)
    {
      if (node is IDisposable)
        ((IDisposable) node).Dispose();
    }

    protected static void ApplyNodeAction(IConfigurationNode node)
    {
      if (node.ConfigObj is ConfigSetting)
        ((ConfigSetting) node.ConfigObj).Apply();
    }

    protected static void SaveNodeAction(IConfigurationNode node)
    {
      ConfigSetting cs = node.ConfigObj as ConfigSetting;
      if (cs != null)
        cs.Save();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of tree nodes that are assigned to the ConfigurationTree.
    /// </summary>
    public IConfigurationNode RootNode
    {
      get { return _root; }
    }

    #endregion

    #region Public methods

    public void Apply()
    {
      _root.ForEach(ApplyNodeAction, true);
    }

    public void SaveSettings()
    {
      _root.ForEach(SaveNodeAction, true);
    }

    /// <summary>
    /// Returns if the specified location can be found in the tree.
    /// If found, it <paramref name="node"/> will be returned.
    /// </summary>
    /// <param name="location">Location to search for. Has to be absolute (starting with "/" character).</param>
    /// <param name="node">Node to be returned. If this method returns <c>false</c>, this parameter
    /// is undefined.</param>
    /// <returns><c>true</c>, if the node at the specified <paramref name="location"/> exists,
    /// else <c>false</c>.</returns>
    public bool FindNode(string location, out IConfigurationNode node)
    {
      if (!RegistryHelper.IsAbsolutePath(location))
        throw new ArgumentException(string.Format("ConfigurationTree: Node location has to be absolute (argument was: '{0}')", location));
      return _root.FindNode(RegistryHelper.RemoveRootFromAbsolutePath(location), out node);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      _root.ForEach(DisposeNodeAction, true);
    }

    #endregion
  }
}
