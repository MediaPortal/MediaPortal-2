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
using System.Xml.Serialization;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Holds the metadata of a view which only contains subviews and no media items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ViewCollectionViewMetadata : ViewMetadata
  {
    internal ViewCollectionViewMetadata(Guid viewId, string displayName,
        Guid? parentViewId) :
        base(viewId, displayName, parentViewId, new Guid[] { }) { }

    #region Additional members for the XML serialization

    internal ViewCollectionViewMetadata() { }

    #endregion
  }
}
