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

using System;
using System.Collections.Generic;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Registry;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Services.Registry
{
  public class RegistryNode: IRegistryNode, IStatus
  {
    #region Protected fields

    protected RegistryNode _parent;
    protected string _name;
    protected object _syncObj;
    protected IDictionary<string, IRegistryNode> _subNodes = null; // lazy initialized
    protected IDictionary<string, object> _items = null; // lazy initialized

    #endregion

    #region Ctor

    public RegistryNode(RegistryNode parent, string name, object syncObj)
    {
      _parent = parent;
      _name = name;
      _syncObj = syncObj;
    }

    #endregion

    #region Protected methods

    protected void CheckSubNodeCollectionPresent()
    {
      lock (_syncObj)
        if (_subNodes == null)
          _subNodes = new Dictionary<string, IRegistryNode>(StringComparer.InvariantCultureIgnoreCase);
    }

    protected void CheckItemCollectionPresent()
    {
      lock (_syncObj)
        if (_items == null)
          _items = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
    }

    #endregion

    #region IRegistryNode implementation

    public string Name
    {
      get { return _name; }
    }

    public string Path
    {
      get
      {
        if (_parent == null)
          return "/" + _name;
        else
          lock (_syncObj)
            return _parent.Path + "/" + _name;
      }
    }

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
      lock (_syncObj)
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
            node = new RegistryNode(this, nodeName, _syncObj);
            _subNodes.Add(nodeName, node);
          }
          else
            return null;
        return i == -1 ? node : node.GetSubNodeByPath(RegistryHelper.RemoveRootFromAbsolutePath(path.Substring(i)), createOnNotExist);
      }
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
      lock (_syncObj)
      {
        CheckItemCollectionPresent();
        try
        {
          _items.Add(name, item);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error("Error adding plugin item '{0}' to plugin tree node '{1}'", e, name, Path);
        }
      }
    }

    public object RemoveItem(string name)
    {
      object result;
      lock (_syncObj)
      {
        if (!_items.TryGetValue(name, out result))
          return null;
        _items.Remove(name);
      }
      return result;
    }

    public IList<T> GetItems<T>()
    {
      IList<T> result = new List<T>();
      lock (_syncObj)
      {
        foreach (object item in _items.Values)
          if (item is T)
            result.Add((T) item);
      }
      return result;

    }

    #endregion

    #region IStatus implementation

    public IList<string> GetStatus()
    {
      lock (_syncObj)
      {
        List<string> result = new List<string> {"[" + _name + "]"};
        if (_subNodes != null)
          foreach (RegistryNode child in _subNodes.Values)
            foreach (string line in child.GetStatus())
              result.Add("  " + line);
        return result;
      }
    }

    #endregion
  }
}
