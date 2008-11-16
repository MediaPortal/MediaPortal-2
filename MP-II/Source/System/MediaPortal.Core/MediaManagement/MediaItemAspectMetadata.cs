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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Holds the metadata descriptor for a <see cref="MediaItemAspect"/>.
  /// </summary>
  public class MediaItemAspectMetadata
  {
    #region Protected fields

    protected string _aspectName;
    protected Guid _aspectId;
    protected bool _isSystemAspect;
    protected IDictionary<string, string> _columnDefinitions;

    #endregion

    // TODO: constructor

    /// <summary>
    /// Returns the globally unique ID of this aspect.
    /// </summary>
    public Guid AspectId
    {
      get { return _aspectId; }
    }

    /// <summary>
    /// Name of this aspect. Can be shown in the gui, for example.
    /// </summary>
    public string Name
    {
      get { return _aspectName; }
    }

    /// <summary>
    /// Returns the information if this aspect is a system aspect. System aspects must not be deleted.
    /// </summary>
    public bool IsSystemAspect
    {
      get { return _isSystemAspect; }
    }

    // TODO: Should we really use a col-name -> SQL col-type mapping here? Or maybe a higher-level
    // description for the collection of metadata items described by this aspect?
    public IDictionary<string, string> ColumnDefinitions
    {
      get { return _columnDefinitions; }
    }
  }
}
