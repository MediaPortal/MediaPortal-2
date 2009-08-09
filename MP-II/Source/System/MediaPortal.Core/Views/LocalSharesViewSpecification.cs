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
using System.Xml.Serialization;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.Views
{
  /// <summary>
  /// View implementation which presents a list of of all local shares, one sub view for each share.
  /// </summary>
  public class LocalSharesViewSpecification : ViewSpecification
  {
    #region Ctor

    public LocalSharesViewSpecification(string viewDisplayName, IEnumerable<Guid> mediaItemAspectIds) :
        base(viewDisplayName, mediaItemAspectIds) { }

    #endregion

    #region Base overrides

    [XmlIgnore]
    public override bool CanBeBuilt
    {
      get { return true; }
    }

    internal override IEnumerable<MediaItem> ReLoadItems()
    {
      yield break;
    }

    internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      ILocalSharesManagement sharesManagement = ServiceScope.Get<ILocalSharesManagement>();
      foreach (ShareDescriptor share in sharesManagement.Shares.Values)
        yield return new LocalShareViewSpecification(share.ShareId, share.Name, string.Empty, _mediaItemAspectIds);
    }

    #endregion

    #region Additional members for the XML serialization

    internal LocalSharesViewSpecification() { }

    #endregion
  }
}
