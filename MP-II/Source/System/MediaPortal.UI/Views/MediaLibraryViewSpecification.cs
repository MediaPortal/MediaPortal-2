#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Views
{
  /// <summary>
  /// View which is based on a media library query.
  /// </summary>
  public class MediaLibraryViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected MediaItemQuery _query;
    protected IList<MediaLibraryViewSpecification> _subViews;

    #endregion

    #region Ctor

    public MediaLibraryViewSpecification(string viewDisplayName, MediaItemQuery query,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _query = query;
    }

    #endregion

    /// <summary>
    /// Returns a list of all sub query view specifications of this view specification.
    /// </summary>
    public IList<MediaLibraryViewSpecification> SubViewSpecifications
    {
      get { return _subViews; }
    }

    public override bool CanBeBuilt
    {
      get
      {
        // TODO (Albert 2009-01-10): Return true if the media library is present
        return false;
      }
    }

    protected internal override IEnumerable<MediaItem> ReLoadItems()
    {
      // TODO (Albert, 2008-11-15): Load view contents from the media library, if connected
      yield break;
    }

    protected internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      IList<ViewSpecification> result = new List<ViewSpecification>(_subViews.Count);
      CollectionUtils.AddAll(result, _subViews);
      return result;
    }
  }
}
