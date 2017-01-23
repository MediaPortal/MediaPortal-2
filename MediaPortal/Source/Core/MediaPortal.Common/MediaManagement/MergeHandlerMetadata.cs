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
using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Holds all metadata for a media merge handler specified by the <see cref="IMediaMergeHandler"/>.
  /// </summary>
  public class MergeHandlerMetadata
  {
    #region Protected fields

    protected Guid _mergeHandlerId;
    protected string _name;

    #endregion

    public MergeHandlerMetadata(Guid mergeHandlerId, string name)
    {
      _mergeHandlerId = mergeHandlerId;
      _name = name;
    }

    /// <summary>
    /// GUID which uniquely identifies the media merge handler.
    /// </summary>
    public Guid MergeHandlerId
    {
      get { return _mergeHandlerId; }
    }

    /// <summary>
    /// Returns a name for the media merge handler.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }
  }
}
