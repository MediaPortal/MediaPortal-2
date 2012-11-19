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
  public class BasicContainer : BasicItem, IDirectoryContainer
  {
    public BasicContainer(string id) : base(id)
    {
      Restricted = true;
      Searchable = false;
      SearchClass = new List<IDirectorySearchClass>();
      CreateClass = new List<IDirectoryCreateClass>();
    }

    public override string Class
    {
      get { return "object.container"; }
    }

    public override void Initialise()
    {
    }

    public void InitialiseAll()
    {
      Initialise();
      foreach (var treeNode in Children.OfType<BasicContainer>())
      {
        (treeNode).InitialiseAll();
      }
    }

    public virtual IList<IDirectorySearchClass> SearchClass { get; set; }

    public virtual bool Searchable { get; set; }

    public virtual int ChildCount
    {
      get { return Children.Count; }
      set { throw new IllegalCallException("Meaningless in this implementation"); }
    }

    public virtual IList<IDirectoryCreateClass> CreateClass { get; set; }
  }
}