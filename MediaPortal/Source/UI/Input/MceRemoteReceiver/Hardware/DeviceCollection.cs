#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections;

namespace MediaPortal.Plugins.MceRemoteReceiver.Hardware
{
  public sealed class DeviceCollection : CollectionBase
  {
    #region Methods

    public void Add(Device device)
    {
      if (device == null)
      {
        throw new ArgumentNullException("device");
      }

      List.Add(device);
    }

    public bool Contains(Device device)
    {
      if (device == null)
      {
        throw new ArgumentNullException("device");
      }

      return List.Contains(device);
    }

    public void CopyTo(Device[] array, int arrayIndex)
    {
      if (array == null)
      {
        throw new ArgumentNullException("array");
      }

      List.CopyTo(array, arrayIndex);
    }

    public int IndexOf(Device device)
    {
      if (device == null)
      {
        throw new ArgumentNullException("device");
      }

      return List.IndexOf(device);
    }

    public void Insert(int index, Device device)
    {
      if (device == null)
      {
        throw new ArgumentNullException("device");
      }

      List.Insert(index, device);
    }

    public bool Remove(Device device)
    {
      if (device == null)
      {
        throw new ArgumentNullException("device");
      }

      if (List.Contains(device) == false)
      {
        return false;
      }

      List.Remove(device);

      return true;
    }

    #endregion Methods

    #region Properties

    public Device this[int index]
    {
      get { return (Device)List[index]; }
      set { List[index] = value; }
    }

    #endregion Properties
  }
}