#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using MediaPortal.Core.MPIManager;

namespace MediaPortal.Services.MPIManager
{
  [Serializable]
  public class MPIQueue : IMPIQueue//, ISerializable 
  {
    public MPIQueue()
    {
      _items = new List<MPIQueueObject>();
    }

     List<MPIQueueObject> _items;
    /// <summary>
    /// Gets or sets the queue item.
    /// </summary>
    /// <value>The item.</value>
    public List<MPIQueueObject> Items
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
    public bool Contains(IMPIQueueObject obj)
    {
      bool x_ret = false;
      foreach (IMPIQueueObject obj1 in Items)
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
