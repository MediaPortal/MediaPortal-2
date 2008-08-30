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
using MediaPortal.Core.ExtensionManager;

namespace MediaPortal.Plugins.ExtensionUpdater.ExtensionManager
{
  [Serializable]
  public class ExtensionQueue : IExtensionQueue//, ISerializable 
  {
    public ExtensionQueue()
    {
      _items = new List<ExtensionQueueObject>();
    }

    List<ExtensionQueueObject> _items;
    /// <summary>
    /// Gets or sets the queue item.
    /// </summary>
    /// <value>The item.</value>
    public List<ExtensionQueueObject> Items
    {
      get
      {
        return _items;
      }
      set
      {
        _items = value;
      }
    }

    /// <summary>
    /// Determines whether [contains] [the specified obj].
    /// </summary>
    /// <param name="obj">The obj.</param>
    /// <returns>
    /// 	<c>true</c> if [contains] [the specified obj]; otherwise, <c>false</c>.
    /// </returns>
    public bool Contains(IExtensionQueueObject obj)
    {
      bool x_ret = false;
      foreach (IExtensionQueueObject obj1 in Items)
      {
        if (obj.Action == obj1.Action && obj.PackageId==obj1.PackageId)
        {
          x_ret = true;
          break;
        }
      }
      return x_ret;
    }
  }
}