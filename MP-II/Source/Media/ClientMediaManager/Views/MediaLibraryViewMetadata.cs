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
using MediaPortal.Core.MediaManagement.MLQueries;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Holds the metadata of a view which is based on a local provider path.
  /// </summary>
  public class MediaLibraryViewMetadata : ViewMetadata
  {
    #region Protected fields

    protected IQuery _query;

    #endregion

    internal MediaLibraryViewMetadata(Guid viewId, string displayName, IQuery query,
        Guid? parentViewId, ICollection<Guid> mediaItemAspectIds) :
      base(viewId, displayName, parentViewId, mediaItemAspectIds)
    {
      _query = query;
    }

    /// <summary>
    /// Returns the media library query this view is based on.
    /// </summary>
    public IQuery Query
    {
      get { return _query; }
    }
  }
}
