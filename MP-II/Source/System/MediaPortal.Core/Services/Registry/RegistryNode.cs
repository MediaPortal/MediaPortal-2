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
using MediaPortal.Core.Registry;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.Registry
{
  public class RegistryNode: IRegistryNode, IStatus
  {
    #region Protected fields

    protected RegistryNode _parent;
    protected string _name;
    protected IDictionary<string, IRegistryNode> _subNodes = null; // lazy initialized
    protected IDictionary<string, object> _items = null; // lazy initialized

    #endregion

    #region Ctor

    public RegistryNode(RegistryNode parent, string name)
    {
      _parent = parent;
      _name = name;
    }

    #endregion

    #region Protected methods

    protected void CheckSubNodeCollectionPresent()
    {
      if (_subNodes == null)
        _subNodes = new Dictionary<string, IRegistryNode>(StringComparer.InvariantCultureIgnoreCase);
    }

    protected void CheckItemCollectionPresent()
    {
      if (_items == null)
        _items = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
    }

    #endregion

    #region IRegistryNode implementation

    public IDictionary<string, IRegistryNode> SubNodes
    {
      get { return _subNodes; }
    }

    public IDictionary<string, object> Items
    {
      get { return _items; }
    }

    public IRegistryNode GetSubNodeByPath(string path, bool createOnNotExist)
    {
      if (_subNodes == null && !createOnNotExist)
        return null;
      if (path.StartsWith("/"))
        throw new ArgumentException("Registry path expression has to be a relative path (no '/' character at the beginning)");
      path = StringUtils.RemoveSuffixIfPresent(path, "/");
      int i = path.IndexOf('/');
      string nodeName = i == -1 ? path : path.Substring(0, i);
      CheckSubNodeCollectionPresent();
      IRegistryNode node = _subNodes.ContainsKey(nodeName) ? _subNodes[nodeName] : null;
      if (node == null)
        if (createOnNotExist)
        {
          node = new RegistryNode(this, nodeName);
          _subNodes.Add(nodeName, node);
        }
        else
          return null;
      return i == -1 ? node : node.GetSubNodeByPath(RegistryHelper.RemoveRootFromAbsolutePath(path.Substring(i)), createOnNotExist);
    }

    public IRegistryNode GetSubNodeByPath(string path)
    {
      return GetSubNodeByPath(path, false);
    }

    public bool SubNodeExists(string path)
    {
      return GetSubNodeByPath(path) != null;
    }

    public void AddItem(string name, object item)
    {
      CheckItemCollectionPresent();
      _items.Add(name, item);
    }

    public object RemoveItem(string name)
    {
      object result = _items.ContainsKey(name) ? _items[name] : null;
      _items.Remove(name);
      return result;
    }

    public IList<T> GetItems<T>()
    {
      IList<T> result = new List<T>();
      foreach (object item in _items.Values)
        if (item is T)
          result.Add((T) item);
      return result;
    }

    #endregion

    #region IStatus implementation

    public IList<string> GetStatus()
    {
      List<string> result = new List<string>();
      result.Add("[" + _name + "]");
      foreach (RegistryNode child in _subNodes.Values)
        foreach (string line in child.GetStatus())
          result.Add("  " + line);
      return result;
    }

    #endregion
  }
}
