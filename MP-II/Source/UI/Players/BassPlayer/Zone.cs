#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

// Todo: 
// Obsolete. However we may need this again when creating the config GUI

using System;
using System.Collections.Generic;
using System.Text;

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Represents Zones == Available Audio Devices
  /// </summary>
  class Zone
  {
    int _id;
    string _name;
    
    public Zone(int id, string name)
    {
      _id = id;
      _name = name;
    }

    /// <summary>
    /// Returns the ID of the Zone
    /// </summary>
    public int Id
    {
      get { return _id; }
    }

    /// <summary>
    /// Returns the Name of the Zone
    /// </summary>
    public string Name
    {
      get { return _name; }
    }
  }
}
