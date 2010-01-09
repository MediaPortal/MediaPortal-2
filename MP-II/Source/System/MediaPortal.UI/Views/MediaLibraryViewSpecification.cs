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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
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
    protected bool _onlyOnline;
    protected IList<MediaLibraryViewSpecification> _subViews = new List<MediaLibraryViewSpecification>();

    #endregion

    #region Ctor

    public MediaLibraryViewSpecification(string viewDisplayName, MediaItemQuery query, bool onlyOnline) :
        base(viewDisplayName, query.NecessaryRequestedMIATypeIDs, query.OptionalRequestedMIATypeIDs)
    {
      _query = query;
      _onlyOnline = onlyOnline;
    }

    #endregion

    public bool OnlyOnline
    {
      get { return _onlyOnline; }
    }

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
        IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
        return scm.IsHomeServerConnected;
      }
    }

    protected internal override IEnumerable<MediaItem> ReLoadItems()
    {
      IContentDirectory cd = ServiceScope.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        yield break;
      foreach (MediaItem mediaItem in cd.Search(_query, _onlyOnline))
        yield return mediaItem;
    }

    protected internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      IList<ViewSpecification> result = new List<ViewSpecification>(_subViews.Count);
      CollectionUtils.AddAll(result, _subViews);
      return result;
    }
  }
}
