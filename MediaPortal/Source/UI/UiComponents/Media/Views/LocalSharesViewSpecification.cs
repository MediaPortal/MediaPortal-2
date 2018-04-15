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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which presents a list of of local shares, one sub view for each share.
  /// </summary>
  public class LocalSharesViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected ICollection<Share> _shares;

    #endregion

    #region Ctor

    public LocalSharesViewSpecification(ICollection<Share> shares, string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _shares = shares;
    }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return true; }
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      mediaItems = new List<MediaItem>();
      subViewSpecifications = new List<ViewSpecification>();
      foreach (Share share in _shares)
        subViewSpecifications.Add(new LocalDirectoryViewSpecification(share.Name, share.BaseResourcePath, _necessaryMIATypeIds, _optionalMIATypeIds));
    }

    #endregion
  }
}
