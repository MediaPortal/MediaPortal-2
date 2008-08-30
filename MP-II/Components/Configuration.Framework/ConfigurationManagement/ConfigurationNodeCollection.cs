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

namespace MediaPortal.Configuration
{

  /// <summary>
  /// Represents a collection of <see cref="ConfigurationNode"/> objects.
  /// </summary>
  internal class ConfigurationNodeCollection : IList<IConfigurationNode>
  {

    #region Variables

    private IConfigurationNode _owner;
    private IList<IConfigurationNode> _nodes;
    private bool _isSet;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the owner of the ConfigurationNodeCollection.
    /// </summary>
    public IConfigurationNode Owner
    {
      get { return _owner; }
      internal set { _owner = value; }
    }

    /// <summary>
    /// [Obsolete?] Gets or sets if the ConfigurationNodeCollection has been set.
    /// </summary>
    internal bool IsSet
    {
      get { return _isSet; }
      set { _isSet = value; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of ConfigurationNodeCollection.
    /// </summary>
    internal ConfigurationNodeCollection()
    {
      _owner = null;
      _nodes = new List<IConfigurationNode>();
    }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of ConfigurationNodeCollection.
    /// </summary>
    /// <param name="owner">Owner of the collection.</param>
    internal ConfigurationNodeCollection(IConfigurationNode owner)
    {
      _owner = owner;
      _nodes = new List<IConfigurationNode>();
    }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of ConfigurationNodeCollection.
    /// </summary>
    /// <param name="owner">Owner of the collection.</param>
    /// <param name="capacity">The expected number of items that the collection will store.</param>
    internal ConfigurationNodeCollection(IConfigurationNode owner, int capacity)
    {
      _owner = owner;
      _nodes = new List<IConfigurationNode>(capacity);
    }

    /// <summary>
    /// [Internal Constructor] Initializes a new instance of ConfigurationNodeCollection.
    /// </summary>
    /// <param name="owner">Owner of the collection.</param>
    /// <param name="collection">The collection whose elements are copied to the collection.</param>
    internal ConfigurationNodeCollection(IConfigurationNode owner, IEnumerable<IConfigurationNode> collection)
    {
      _owner = owner;
      _nodes = new List<IConfigurationNode>(collection);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns the index of the element with the itemId in the ConfigurationNodeCollection.
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    public int IndexOf(string itemId)
    {
      for (int i = 0; i < _nodes.Count; i++)
      {
        if (_nodes[i].Setting.Id.ToString() == itemId)
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Returns a String that represents the current ConfigurationNodeCollection.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return _owner.ToString() + " - Count: " + _nodes.Count;
    }

    #endregion

    #region IList<IConfigurationNode> Members

    /// <summary>
    /// Determines the index of a specific item in the ConfigurationNodeCollection.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <param name="item"></param>
    /// <returns></returns>
    public int IndexOf(IConfigurationNode item)
    {
      return _nodes.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item to the ConfigurationNodeCollection.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <param name="index"></param>
    /// <param name="item"></param>
    public void Insert(int index, IConfigurationNode item)
    {
      _nodes.Insert(index, item);
    }

    /// <summary>
    /// Removes the ConfigurationNodeCollection item at the specified index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <param name="index"></param>
    public void RemoveAt(int index)
    {
      _nodes.RemoveAt(index);
    }

    /// <summary>
    /// Gets or sets an item of the ConfigurationNodeCollection.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <param name="index"></param>
    /// <returns></returns>
    public IConfigurationNode this[int index]
    {
      get
      {
        return _nodes[index];
      }
      set
      {
        _nodes[index] = value;
      }
    }

    #endregion

    #region ICollection<IConfigurationNode> Members

    /// <summary>
    /// Adds an item to the ConfigurationNodeCollection.
    /// </summary>
    /// <param name="item"></param>
    public void Add(IConfigurationNode item)
    {
      ((ConfigurationNode)item).Parent = _owner;
      if (_owner != null)
        ((ConfigurationNode)item).Tree = ((ConfigurationNode)_owner).Tree;
      _nodes.Add(item);
    }

    /// <summary>
    /// Removes all items from the ConfigurationNodeCollection.
    /// </summary>
    public void Clear()
    {
      _nodes.Clear();
    }

    /// <summary>
    /// Determines whether the ConfigurationNodeCollection contains a specific value.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(IConfigurationNode item)
    {
      return _nodes.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the ConfigurationNodeCollection to an Array.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    public void CopyTo(IConfigurationNode[] array, int arrayIndex)
    {
      _nodes.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Gets the number of elements contained in the ConfigurationNodeCollection.
    /// </summary>
    public int Count
    {
      get { return _nodes.Count; }
    }

    /// <summary>
    /// Gets a value indicating whether the ConfigurationNodeCollection is read-only.
    /// </summary>
    public bool IsReadOnly
    {
      get { return false; }
    }

    /// <summary>
    /// Removes the first occurence of a specific value from the ConfigurationNodeCollection.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(IConfigurationNode item)
    {
      return _nodes.Remove(item);
    }

    #endregion

    #region IEnumerable<IConfigurationNode> Members

    /// <summary>
    /// Returns an enumerator that iterates through the ConfigurationNodeCollection.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<IConfigurationNode> GetEnumerator()
    {
      return _nodes.GetEnumerator();
    }

    #endregion

    #region IEnumerable Members

    /// <summary>
    /// Returns an enumerator that iterates through the ConfigurationNodeCollection.
    /// </summary>
    /// <returns></returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return _nodes.GetEnumerator();
    }

    #endregion

  }
}
