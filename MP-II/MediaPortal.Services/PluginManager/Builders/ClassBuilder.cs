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

﻿#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 * 
 */

#endregion

using System;
using System.Collections;
using MediaPortal.Core.PluginManager;

namespace MediaPortal.Services.PluginManager.Builders
{
  /// <summary>
  /// Creates menu items from a location in the addin tree.
  /// </summary>
  /// <attribute name="class" use="optional">
  /// Command class that is run when item is clicked.
  /// </attribute>
  /// <attribute name="shortcut" use="optional">
  /// Shortcut that activates the command (e.g. "Control|S").
  /// </attribute>
  /// <conditions>Conditions are handled by the item, "Exclude" maps to "Visible = false", "Disable" to "Enabled = false"</conditions>
  public class ClassBuilder : IPluginBuilder
  {
    ///// <summary>
    ///// Gets if the doozer handles codon conditions on its own.
    ///// If this property return false, the item is excluded when the condition is not met.
    ///// </summary>
    //public bool HandleConditions {
    //  get {
    //    return true;
    //  }
    //}

    public object BuildItem(object caller, INodeItem item, ArrayList subItems)
    {
      return item.CreateObject(item["class"]);
    }
  }
}
