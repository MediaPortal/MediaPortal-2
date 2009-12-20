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

using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;
using System;

namespace MediaPortal.UI.Views
{
  /// <summary>
  /// View specification which defining a view which only contains a configurable list of subviews and no media items.
  /// </summary>
  public class ViewCollectionViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected IList<ViewSpecification> _subViews = new List<ViewSpecification>();

    #endregion

    #region Ctor

    public ViewCollectionViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    public void AddSubView(ViewSpecification subView)
    {
      _subViews.Add(subView);
    }

    public void RemoveSubView(ViewSpecification subView)
    {
      _subViews.Remove(subView);
    }

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    protected internal override IEnumerable<MediaItem> ReLoadItems()
    {
      yield break;
    }

    protected internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      return _subViews;
    }

    #endregion
  }
}
