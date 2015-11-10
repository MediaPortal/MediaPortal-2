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

using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MediaServer.Tree
{
  public class TreeNode<T>
  {
    public string Key { get; protected set; }
    public T Value { get; protected set; }
    public TreeNode<T> Parent { get; protected set; }

    private List<TreeNode<T>> _children;

    public List<TreeNode<T>> Children
    {
      get { return _children ?? (_children = new List<TreeNode<T>>()); }
    }

    public TreeNode(string key, T value)
    {
      Key = key;
      Value = value;
      Parent = null;
    }

    public void Add(TreeNode<T> node)
    {
      node.Parent = this;
      if (!Children.Contains(node))
      {
        Children.Add(node);
      }
    }

    //       public TreeNode<T> FindNode(string key)
    //       {
    //           return Key == key ? this : Children.FindNode(key);
    //       }

    public virtual TreeNode<T> FindNode(string key)
    {
      return Key == key ? this : Children.Select(node => node.FindNode(key)).FirstOrDefault(n => n != null);
    }

	public void Sort()
    {
        if (_children != null)
        {
            _children.Sort();
            foreach (var node in _children)
            {
                node.Sort();
            }
        }
    }
  }
}
