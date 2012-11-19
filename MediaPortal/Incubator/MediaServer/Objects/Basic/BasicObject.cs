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
using System.Text;
using MediaPortal.Extensions.MediaServer.DIDL;
using MediaPortal.Extensions.MediaServer.Tree;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Extensions.MediaServer.Objects.Basic
{
  public abstract class BasicObject : TreeNode<object>, IDirectoryObject
  {
    protected BasicObject(string key) : base(key, null)
    {
      Resources = new List<IDirectoryResource>();
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

    public virtual IList<IDirectoryResource> Resources { get; set; }

    public virtual bool Restricted { get; set; }

    public virtual string WriteStatus { get; set; }

    public abstract string Class { get; }

    public abstract void Initialise();

    public virtual List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      //var sieve = new ObjectPropertyFilter(filter);
      //return Children.Cast<IDirectoryObject>().Where(sieve.Matches).ToList();
      //

      return Children.Cast<IDirectoryObject>().ToList();

      //TODO: Need to sort based on sortCriteria.
    }

    public BasicObject FindObject(string objectId)
    {
      return (BasicObject) FindNode(objectId);
    }
  }
}