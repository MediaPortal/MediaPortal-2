#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public abstract class AbstractSortByFirstComparableAttribute<T> : SortByTitle where T : IComparable<T>
  {
    protected IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> _attrs;
    protected IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> _multiAttrs;

    protected AbstractSortByFirstComparableAttribute(MediaItemAspectMetadata.SingleAttributeSpecification attr)
    {
      _attrs = new[] { attr };
    }

    protected AbstractSortByFirstComparableAttribute(IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> attrs)
    {
      _attrs = attrs;
    }

    protected AbstractSortByFirstComparableAttribute(MediaItemAspectMetadata.MultipleAttributeSpecification attr)
    {
      _multiAttrs = new[] { attr };
    }

    protected AbstractSortByFirstComparableAttribute(IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> attrs)
    {
      _multiAttrs = attrs;
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      MediaItemAspect aspectX = null;
      MediaItemAspect aspectY = null;
      MediaItemAspectMetadata.AttributeSpecification attrX = null;
      MediaItemAspectMetadata.AttributeSpecification attrY = null;
      if (_attrs != null)
      {
        attrX = GetAttributeSpecification(x, _attrs, out aspectX);
        attrY = GetAttributeSpecification(y, _attrs, out aspectY);
      }
      else if (_multiAttrs != null)
      {
        attrX = GetAttributeSpecification(x, _multiAttrs, out aspectX);
        attrY = GetAttributeSpecification(y, _multiAttrs, out aspectY);
      }

      if (attrX != null && attrY != null)
        return CompareAttributes(aspectX, attrX, aspectY, attrY);
      return base.Compare(x, y);
    }

    protected int CompareAttributes(MediaItemAspect aspectX, MediaItemAspectMetadata.AttributeSpecification attrX, MediaItemAspect aspectY, MediaItemAspectMetadata.AttributeSpecification attrY)
    {
      string firstValueX = null;
      IEnumerable<string> collectionX = aspectX.GetCollectionAttribute<string>(attrX);
      if (collectionX != null)
      {
        List<string> valuesX = new List<string>(collectionX);
        valuesX.Sort();
        firstValueX = valuesX.FirstOrDefault();
      }
      string firstValueY = null;
      IEnumerable<string> collectionY = aspectY.GetCollectionAttribute<string>(attrY);
      if (collectionY != null)
      {
        List<string> valuesY = new List<string>(collectionY);
        valuesY.Sort();
        firstValueY = valuesY.FirstOrDefault();
      }
      return ObjectUtils.Compare(firstValueX, firstValueY);
    }

    public override object GetGroupByValue(MediaItem item)
    {
      MediaItemAspect aspect = null;
      MediaItemAspectMetadata.AttributeSpecification attr = null;
      if (_attrs != null)
        attr = GetAttributeSpecification(item, _attrs, out aspect);
      else if (_multiAttrs != null)
        attr = GetAttributeSpecification(item, _multiAttrs, out aspect);

      if (attr != null)
      {
        IEnumerable<string> valueColl = aspect.GetCollectionAttribute<string>(attr);
        if (valueColl != null)
        {
          List<string> values = new List<string>(valueColl);
          values.Sort();
          return values.FirstOrDefault();
        }
      }
      return base.GetGroupByValue(item);
    }
  }
}
