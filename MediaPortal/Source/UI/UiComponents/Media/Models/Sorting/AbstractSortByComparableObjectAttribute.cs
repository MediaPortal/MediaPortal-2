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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public abstract class AbstractSortByComparableObjectAttribute<T> : Sorting where T : class, IComparable<T>
  {
    protected string _displayName;
    protected string _groupByDisplayName;
    protected IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> _sortAttrs;
    protected IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> _sortMultiAttrs;

    protected AbstractSortByComparableObjectAttribute(string displayName, string groupByDisplayName, MediaItemAspectMetadata.SingleAttributeSpecification sortAttr)
      : this(displayName, groupByDisplayName, new[] { sortAttr })
    {
    }

    protected AbstractSortByComparableObjectAttribute(string displayName, string groupByDisplayName, IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> sortAttrs)
    {
      _displayName = displayName;
      _groupByDisplayName = groupByDisplayName;
      _sortAttrs = sortAttrs;
    }

    protected AbstractSortByComparableObjectAttribute(string displayName, string groupByDisplayName, MediaItemAspectMetadata.MultipleAttributeSpecification sortAttr)
      : this(displayName, groupByDisplayName, new[] { sortAttr })
    {
    }

    protected AbstractSortByComparableObjectAttribute(string displayName, string groupByDisplayName, IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> sortAttrs)
    {
      _displayName = displayName;
      _groupByDisplayName = groupByDisplayName;
      _sortMultiAttrs = sortAttrs;
    }

    public override string DisplayName
    {
      get { return _displayName; }
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      MediaItemAspect aspectX = null;
      MediaItemAspect aspectY = null;
      MediaItemAspectMetadata.AttributeSpecification attrX = null;
      MediaItemAspectMetadata.AttributeSpecification attrY = null;
      if (_sortAttrs != null)
      {
        attrX = GetAttributeSpecification(x, _sortAttrs, out aspectX);
        attrY = GetAttributeSpecification(y, _sortAttrs, out aspectY);
      }
      else if (_sortMultiAttrs != null)
      {
        attrX = GetAttributeSpecification(x, _sortMultiAttrs, out aspectX);
        attrY = GetAttributeSpecification(y, _sortMultiAttrs, out aspectY);
      }

      T valX = null;
      T valY = null;
      if (attrX != null)
      {
        valX = (T)aspectX.GetAttributeValue(attrX);
      }
      if (attrY != null)
      {
        valY = (T)aspectY.GetAttributeValue(attrY);
      }
      return ObjectUtils.Compare(valX, valY);
    }

    public override string GroupByDisplayName
    {
      get {  return _groupByDisplayName; }
    }

    public override object GetGroupByValue(MediaItem item)
    {
      MediaItemAspect aspect = null;
      MediaItemAspectMetadata.AttributeSpecification attr = null;
      if (_sortAttrs != null)
        attr = GetAttributeSpecification(item, _sortAttrs, out aspect);
      else if (_sortMultiAttrs != null)
        attr = GetAttributeSpecification(item, _sortMultiAttrs, out aspect);

      if (attr != null)
        return aspect.GetAttributeValue(attr);
      return null;
    }
  }
}
