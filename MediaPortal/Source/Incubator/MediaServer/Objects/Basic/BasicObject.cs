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
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Common.Logging;
using MediaPortal.Common;

namespace MediaPortal.Plugins.MediaServer.Objects.Basic
{
  public abstract class BasicObject : IEquatable<BasicObject>, IComparable<BasicObject>, IDirectoryObject
  {
    public string Key { get; private set; }
    public EndPointSettings Client { get; private set; }

    public BasicObject Parent { get; set; }

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

    public bool Equals(BasicObject other)
    {
      if ((object)other == null)
        return false;
      return string.Equals(Key, other.Key, StringComparison.InvariantCultureIgnoreCase);
    }

    public int CompareTo(BasicObject other)
    {
      if ((object)other == null) return 1;
      return Title.CompareTo(other.Title);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
