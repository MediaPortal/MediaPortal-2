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

using System.Collections.Generic;

namespace MediaPortal.Core.Registry
{
  /// <summary>
  /// Interface to a node in the application registry tree. This interface provides methods to access
  /// sub nodes as well as items.
  /// </summary>
  /// <remarks>
  /// Every registry node can be seen as a sub tree. Every node contains sub nodes, which build up the node tree
  /// and items, which contain the registration data in the node.
  /// </remarks>
  public interface IRegistryNode
  {
    /// <summary>
    /// Returns the names of all sub nodes of this node mapped to the node instances.
    /// </summary>
    IDictionary<string, IRegistryNode> SubNodes { get; }

    /// <summary>
    /// Returns the names of all items registered in this node mapped to the item objects.
    /// </summary>
    IDictionary<string, object> Items { get; }

    /// <summary>
    /// Checks if the specified relative registry path already exists, and returns the specified
    /// registry node. If the specified path doesn't exist, it will be created if
    /// <paramref name="createOnNotExist"/> is set to <c>true</c>.
    /// The specified path has to be a relative path, where the first path entry denotes the sub node
    /// with the name of the path entry.
    /// </summary>
    /// <param name="path">Relative path expression specifying the node location in the tree, starting
    /// at this node.</param>
    /// <param name="createOnNotExist">If set to <c>true</c> and the node with the specified path doesn't
    /// exist, this method will create it.</param>
    /// <returns>Registry tree node specified by <paramref name="path"/>, if it exists or if
    /// it was created, else <c>false</c>.</returns>
    IRegistryNode GetSubNodeByPath(string path, bool createOnNotExist);

    /// <summary>
    /// Returns the node of the specified relative registry node, if it exists. The specified path has to
    /// be a relative path.
    /// </summary>
    /// <param name="path">Relative path expression specifying the node location in the tree, starting
    /// at this node.</param>
    /// <returns>Registry tree node specified by <paramref name="path"/>, if it exists, else
    /// <c>false</c>.</returns>
    IRegistryNode GetSubNodeByPath(string path);

    /// <summary>
    /// Returns the information, if the registry node in the specified <paramref name="path"/> exists.
    /// </summary>
    /// <param name="path">Relative path expression.</param>
    /// <returns><c>true</c>, if the specified registry path exists, else <c>false</c>.</returns>
    bool SubNodeExists(string path);

    /// <summary>
    /// Adds an item to this registry node with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name to use for the item registration. The item name has to be unique within
    /// the items of this registry node.</param>
    /// <param name="item">The item to register.</param>
    void AddItem(string name, object item);

    /// <summary>
    /// Removes the item with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name of the item to remove.</param>
    /// <returns>Removed item.</returns>
    object RemoveItem(string name);

    /// <summary>
    /// Returns all items of type <see cref="T"/> in this registry node.
    /// </summary>
    /// <typeparam name="T">Type of the items to return.</typeparam>
    /// <returns>All items of this registry node, filtered by class <see cref="T"/>.</returns>
    IList<T> GetItems<T>();
  }
}
