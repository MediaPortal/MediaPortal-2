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

using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using System;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View specification defining a view which contains a configurable list of subviews and no media items.
  /// </summary>
  public class StaticViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected IList<MediaItem> _mediaItems = new List<MediaItem>();
    protected IList<ViewSpecification> _subViewSpecifications = new List<ViewSpecification>();

    #endregion

    #region Ctor

    public StaticViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds) { }

    #endregion

    public void AddMediaItem(MediaItem item)
    {
      _mediaItems.Add(item);
    }

    public void RemoveMediaItem(MediaItem item)
    {
      _mediaItems.Remove(item);
    }

    public void AddSubView(ViewSpecification subView)
    {
      _subViewSpecifications.Add(subView);
    }

    public void RemoveSubView(ViewSpecification subView)
    {
      _subViewSpecifications.Remove(subView);
    }

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = _mediaItems;
      subViewSpecifications = _subViewSpecifications;
    }

    #endregion
  }
}
