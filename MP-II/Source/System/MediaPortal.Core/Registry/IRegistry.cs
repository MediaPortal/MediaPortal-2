#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.Registry
{
  /// <summary>
  /// Interface implemented by the application object registry. The registry is a tree structure
  /// which can store items. The registry is a central data store to manage globally accessible
  /// application data.
  /// </summary>
  /// <remarks>
  /// The registry can be seen as a hierarchical system (tree). Each tree node can contain
  /// either child nodes, or items, or both. This interface provides methods to create, access and test the
  /// availability of registry nodes.
  /// <para>
  /// <b>Registry path expressions</b>
  /// Registry paths are specified in a syntax similar to UNIX path expression syntax. Absolute paths
  /// start with the '/' character, which denotes the root node when used as the first character.
  /// Path element separator is the '/' character.
  /// </para>
  /// <para>
  /// <b>Thread-Safety:</b><br/>
  /// This class can be called from multiple threads. It synchronizes thread access to its fields via its
  /// <see cref="SyncObj"/> instance. Also accesses to all of its contained <see cref="IRegistryNode"/> instances
  /// are synchronized via the same <see cref="SyncObj"/> instance. To widen the synchronization scope for a critical
  /// code section, the <see cref="SyncObj"/> can be explicitly used to lock.
  /// </para>
  /// </remarks>
  public interface IRegistry
  {
    /// <summary>
    /// Returns the multithreading synchronization object the registry uses for locking multithreaded access.
    /// </summary>
    object SyncObj { get; }

    /// <summary>
    /// Returns the root node of the registry tree.
    /// </summary>
    IRegistryNode RootNode { get; }

    /// <summary>
    /// Checks if the specified registry path already exists, and returns the specified registry
    /// node. If the specified path doesn't exist and <paramref name="createOnNotExist"/> is set to
    /// <c>true</c>, the node will be created. The specified path has to be an absolute path.
    /// </summary>
    /// <param name="path">Absolute path expression specifying the node location in the tree.</param>
    /// <param name="createOnNotExist">If set to <c>true</c> and the node with the specified path
    /// doesn't exist, it will be created.</param>
    /// <returns>Registry tree node specified by <paramref name="path"/>, if it exists or if
    /// it was created, else <c>false</c>.</returns>
    IRegistryNode GetRegistryNode(string path, bool createOnNotExist);

    /// <summary>
    /// Returns the specified registry node, if the <paramref name="path"/> exists. The specified path has to
    /// be an absolute path.
    /// </summary>
    /// <param name="path">Absolute path expression specifying the node location in the tree.</param>
    /// <returns>Registry tree node specified by <paramref name="path"/>, if it exists, else
    /// <c>false</c>.</returns>
    IRegistryNode GetRegistryNode(string path);

    /// <summary>
    /// Returns the information, if the registry node in the specified <paramref name="path"/> exists.
    /// </summary>
    /// <param name="path">Absolute path expression.</param>
    /// <returns><c>true</c>, if the specified registry path exists, else <c>false</c>.</returns>
    bool RegistryNodeExists(string path);
  }
}
