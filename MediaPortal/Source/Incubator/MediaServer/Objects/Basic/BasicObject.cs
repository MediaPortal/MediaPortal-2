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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Plugins.MediaServer.Objects.Basic
{
  public abstract class BasicObject : IDirectoryObject
  {
    public string Key { get; protected set; }
    public BasicObject Parent { get; protected set; }

    private List<BasicObject> _children;

    public List<BasicObject> Children
    {
      get { return _children ?? (_children = new List<BasicObject>()); }
    }

    public EndPointSettings Client { get; private set; }

    protected BasicObject(string key, EndPointSettings client)
    {
      Resources = new List<IDirectoryResource>();
	  
      Key = key;
      Parent = null;
      Client = client;
    }

    public virtual string Id
    {
      get { return Key; }
      set { Key = value; }
    }

    public virtual string ParentId
    {
      get { return Parent != null ? Parent.Key : "-1"; }
      set { throw new IllegalCallException("Meaningless in this implementation"); }
    }

    public virtual string Title { get; set; }

    public virtual string Creator { get; set; }

    public IList<IDirectoryResource> Resources { get; set; }

    public virtual bool Restricted { get; set; }

    public virtual string WriteStatus { get; set; }

    public abstract string Class { get; set; }
	
    public abstract void Initialise();

    public void Add(BasicObject node)
    {
      Console.WriteLine("TreeNode::Add entry, {0} to {1}", node.Key, Key);
      node.Parent = this;
      if (!Children.Contains(node))
      {
        Children.Add(node);
      }
      Console.WriteLine("TreeNode::Add exit, {0} children", Children.Count);
    }

    //       public TreeNode<T> FindNode(string key)
    //       {
    //           return Key == key ? this : Children.FindNode(key);
    //       }

    public virtual BasicObject FindNode(string key)
    {
      return Key == key ? this : Children.Select(node => node.FindNode(key)).FirstOrDefault(n => n != null);
    }

	  public void Sort()
    {
        if (_children != null)
        {
            Children.Sort();
            foreach (var node in Children)
            {
                node.Sort();
            }
        }
    }

    public List<IDirectoryObject> Browse(string filter, string sortCriteria)
    {
      // TODO: Need to sort based on sortCriteria.

      return Children.Cast<IDirectoryObject>().ToList();
    }

    public BasicObject FindObject(string objectId)
    {
      return Key == objectId ? this : Children.Select(node => node.FindNode(objectId)).FirstOrDefault(n => n != null);
    }
  }
}
