#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.MP2Web.WebAppConfiguration
{
  public class MP2WebMenuDefinition
  {
    /// <summary>
    /// Id of the Menu Item
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Defines the order of the menu items
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Identifier for the Route: [routerLink]="['Name']"
    /// It must start with an upper case!
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Label which is shown in the Navigationbar
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Route Path: "/Path"
    /// If the Component defines own Routes the Route must end with "/..."
    /// e.g. "movies/..."
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Is the entry visble in the Navigation
    /// </summary>
    public bool Visible { get; set; }
  }
}