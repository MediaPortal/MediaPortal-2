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

namespace MediaPortal.Attributes
{
  [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
  public sealed class CoreAPIAttribute : Attribute
  {
    readonly int currentAPI;

    public CoreAPIAttribute(int currentAPI)
    {
      this.currentAPI = currentAPI;
      this.MinCompatibleAPI = currentAPI; // in case this is not set by the assembly, assume the same as current API
    }

    /// <summary>
    /// Returns the current API level of this core component.
    /// </summary>
    public int CurrentAPI 
    { 
      get { return currentAPI; } 
    }

    /// <summary>
    /// Specifies the minimum API level of this core component that is compatible with the current API level of this core component's version.
    /// </summary>
    public int MinCompatibleAPI { get; set; }
  }
}
