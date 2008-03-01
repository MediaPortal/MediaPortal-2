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
using MediaPortal.Core.PluginManager;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;

namespace LocalHdProvider
{
  public class LocalHdProvider : IPlugin, IProvider
  {
    public void Initialize(string id)
    {
      // ServiceScope.Get<IMediaManager>().Register(this);
    }

    public void Dispose() { }

    #region IProvider Members

    /// <summary>
    /// get the root containers for this provider
    /// </summary>
    /// <value></value>
    public List<IRootContainer> RootContainers
    {
      get
      {
        string[] drives = Environment.GetLogicalDrives();
        List<IRootContainer> containers = new List<IRootContainer>();
        for (int i = 0; i < drives.Length; ++i)
        {
          DiskRootContainer root = new DiskRootContainer(drives[i]);
          containers.Add(root);
        }
        return containers;
      }
    }

    /// <summary>
    /// get the title for this provider
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return "Local Harddisk Provider"; }
      set { }
    }

    /// <summary>
    /// Gets the view.
    /// </summary>
    /// <param name="query">The query for the view.</param>
    /// <returns>list of containers&items for the query</returns>
    public List<IAbstractMediaItem> GetView(IView query, IRootContainer root, IRootContainer parent)
    {
      //local hd provider does not support views...
      return new List<IAbstractMediaItem>();
    }
    #endregion
  }
}
