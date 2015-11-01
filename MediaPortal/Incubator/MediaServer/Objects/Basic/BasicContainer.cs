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
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MediaServer.Parser;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.Tree;

namespace MediaPortal.Extensions.MediaServer.Objects.Basic
{
  public class BasicContainer : BasicItem, IDirectoryContainer
  {
    private bool _initialised = false;
    protected readonly Dictionary<string, BasicItem> _children = new Dictionary<string, BasicItem>();

    public BasicContainer(string id, EndPointSettings client) 
      : base(id, client)
    {
      Restricted = true;
      Searchable = false;
      SearchClass = new List<IDirectorySearchClass>();
      CreateClass = new List<IDirectoryCreateClass>();
      WriteStatus = "NOT_WRITABLE";
      Class = "object.container";
    }

    public int ChildCount
    {
      get
      {
        if (!_initialised) Initialise();
        return _children.Count;
      }
      set { } //Meaningless in this implementation
    }

    public override void Initialise()
    {
      _initialised = true;
    }

    public override TreeNode<object> FindNode(string key)
    {
      if (!key.StartsWith(Key)) return null;
      if (key == Key) return this;

      if (!_initialised) Initialise();
      BasicItem container;
      _children.TryGetValue(key, out container);
      return container;
    }

    public override List<IDirectoryObject> Search(string filter, string sortCriteria)
    {
      // TODO: Use the parameters for something
      // IFilter searchFilter = SearchParser.Convert(SearchParser.Parse(filter), _necessaryMiaTypeIds);

      if (!_initialised) Initialise();

      return _children.Values.ToList().Cast<IDirectoryObject>().ToList();
    }

    public void ContainerUpdated()
    {
      UpdateId++;
      LastUpdate = DateTime.Now;
    }

    public virtual IList<IDirectorySearchClass> SearchClass { get; set; }

    public virtual bool Searchable { get; set; }

    public virtual IList<IDirectoryCreateClass> CreateClass { get; set; }

    public int UpdateId { get; set; }

    public DateTime LastUpdate { get; set; }
  }
}
