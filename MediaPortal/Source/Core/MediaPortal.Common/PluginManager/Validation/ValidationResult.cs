#region Copyright (C) 2007-2014 Team MediaPortal
/*
    Copyright (C) 2007-2014 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager.Validation
{
  /// <summary>
  /// This class holds information on validation errors collected when validating
  /// a single plugin.
  /// </summary>
  internal class ValidationResult
  {
    #region Fields
    public HashSet<Guid> MissingDependencies { get; internal set; }
    public HashSet<Guid> ConflictsWith { get; internal set; }
    public HashSet<Guid> IncompatibleWith { get; internal set; } 
    #endregion

    #region Properies (IsComplete, CanEnable)
    /// <summary>
    /// Returns true if the plugin has all of its dependencies available.
    /// </summary>
    public bool IsComplete
    {
      get { return MissingDependencies.Count == 0; }
    }

    /// <summary>
    /// Returns true if the plugin can be enabled. This requires that there were no 
    /// validation errors.
    /// </summary>
    public bool CanEnable
    {
      get { return IsComplete && ConflictsWith.Count == 0 && IncompatibleWith.Count == 0; }
    } 
    #endregion
  }
}
