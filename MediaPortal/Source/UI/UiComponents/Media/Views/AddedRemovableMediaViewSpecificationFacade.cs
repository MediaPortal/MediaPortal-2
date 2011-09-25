#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// View implementation which lists all items of another (delegate) view specification but additionally adds removable media drives.
  /// </summary>
  public class AddedRemovableMediaViewSpecificationFacade : ViewSpecification
  {
    #region Protected fields

    protected ICollection<RemovableDriveViewSpecification> _removableDriveVS = new List<RemovableDriveViewSpecification>();
    protected ViewSpecification _delegate;

    #endregion

    #region Ctor

    public AddedRemovableMediaViewSpecificationFacade(ViewSpecification dlgt) :
        this(dlgt, dlgt.ViewDisplayName, dlgt.NecessaryMIATypeIds, dlgt.OptionalMIATypeIds) { }

    public AddedRemovableMediaViewSpecificationFacade(ViewSpecification dlgt, string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(viewDisplayName, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _delegate = dlgt;
      _removableDriveVS = RemovableDriveViewSpecification.CreateViewSpecificationsForRemovableDrives(_necessaryMIATypeIds, _optionalMIATypeIds);
    }

    #endregion

    #region Base overrides

    public override bool CanBeBuilt
    {
      get { return _delegate.CanBeBuilt; }
    }

    public override IViewChangeNotificator GetChangeNotificator()
    {
      IViewChangeNotificator delegateChangeNotificator = _delegate.GetChangeNotificator();
      IViewChangeNotificator removableDriveChangeNotificator = CombinedViewChangeNotificator.CombineViewChangeNotificators(_removableDriveVS);
      IList<IViewChangeNotificator> subChangeNotificators = new List<IViewChangeNotificator>(2);
      if (delegateChangeNotificator != null)
        subChangeNotificators.Add(delegateChangeNotificator);
      if (removableDriveChangeNotificator != null)
        subChangeNotificators.Add(removableDriveChangeNotificator);
      if (subChangeNotificators.Count == 0)
        return null;
      if (subChangeNotificators.Count == 1)
        return subChangeNotificators[0];
      return new CombinedViewChangeNotificator(subChangeNotificators);
    }

    protected internal override void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications)
    {
      _delegate.ReLoadItemsAndSubViewSpecifications(out mediaItems, out subViewSpecifications);
      foreach (RemovableDriveViewSpecification rdvs in _removableDriveVS)
      {
        IList<MediaItem> rdvsItems;
        IList<ViewSpecification> rdvsViewSpecs;
        rdvs.ReLoadItemsAndSubViewSpecifications(out rdvsItems, out rdvsViewSpecs);
        CollectionUtils.AddAll(mediaItems, rdvsItems);
        CollectionUtils.AddAll(subViewSpecifications, rdvsViewSpecs);
      }
    }

    #endregion
  }
}
