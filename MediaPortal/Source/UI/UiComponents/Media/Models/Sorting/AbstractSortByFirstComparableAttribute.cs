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
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.Sorting
{
  public abstract class AbstractSortByFirstComparableAttribute<T> : SortByTitle where T : IComparable<T>
  {
    protected IEnumerable<MediaItemAspectMetadata.SingleAttributeSpecification> _attrs;
    protected IEnumerable<MediaItemAspectMetadata.MultipleAttributeSpecification> _multiAttrs;
    protected bool _preSortAttributes = true;

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
        return CompareAttributes(x, aspectX, attrX, y, aspectY, attrY);
      return 0;
    }

    protected int CompareAttributes(MediaItem x, MediaItemAspect aspectX, MediaItemAspectMetadata.AttributeSpecification attrX, MediaItem y, MediaItemAspect aspectY, MediaItemAspectMetadata.AttributeSpecification attrY)
    {
      string firstValueX = null;
      string firstValueY = null;
      if (attrX.IsCollectionAttribute)
      {
        IEnumerable<string> collectionX = aspectX.GetCollectionAttribute<string>(attrX);
        if (collectionX != null)
        {
          List<string> valuesX = new List<string>(collectionX);
          if (_preSortAttributes)
            valuesX.Sort();
          firstValueX = valuesX.FirstOrDefault();
        }
      }
      else
      {
        IList<MultipleMediaItemAspect> aspectsX;
        MultipleMediaItemAspectMetadata metadata = attrX.ParentMIAM as MultipleMediaItemAspectMetadata;
        if (metadata != null && MediaItemAspect.TryGetAspects(x.Aspects, metadata, out aspectsX))
        {
          List<string> valuesX = new List<string>();
          foreach (MultipleMediaItemAspect aspect in aspectsX)
          {
            if (!string.IsNullOrEmpty(aspect.GetAttributeValue<string>(attrX)))
              valuesX.Add(aspect.GetAttributeValue<string>(attrX));
          }
          if (valuesX.Count > 0)
          {
            if (_preSortAttributes)
              valuesX.Sort();
            firstValueX = valuesX.FirstOrDefault();
          }
        }
      }

      if (attrY.IsCollectionAttribute)
      {
        IEnumerable<string> collectionY = aspectY.GetCollectionAttribute<string>(attrY);
        if (collectionY != null)
        {
          List<string> valuesY = new List<string>(collectionY);
          if (_preSortAttributes)
            valuesY.Sort();
          firstValueY = valuesY.FirstOrDefault();
        }
      }
      else
      {
        IList<MultipleMediaItemAspect> aspectsY;
        MultipleMediaItemAspectMetadata metadata = attrY.ParentMIAM as MultipleMediaItemAspectMetadata;
        if (metadata != null && MediaItemAspect.TryGetAspects(y.Aspects, metadata, out aspectsY))
        {
          List<string> valuesY = new List<string>();
          foreach (MultipleMediaItemAspect aspect in aspectsY)
          {
            if (!string.IsNullOrEmpty(aspect.GetAttributeValue<string>(attrY)))
              valuesY.Add(aspect.GetAttributeValue<string>(attrY));
          }
          if (valuesY.Count > 0)
          {
            if (_preSortAttributes)
              valuesY.Sort();
            firstValueY = valuesY.FirstOrDefault();
          }
        }
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
        if (attr.IsCollectionAttribute)
        {
          IEnumerable<string> valueColl = aspect.GetCollectionAttribute<string>(attr);
          if (valueColl != null)
          {
            List<string> values = new List<string>(valueColl);
            if (_preSortAttributes)
              values.Sort();
            return values.FirstOrDefault();
          }
        }
        else
        {
          IList<MultipleMediaItemAspect> aspects;
          MultipleMediaItemAspectMetadata metadata = attr.ParentMIAM as MultipleMediaItemAspectMetadata;
          if (metadata != null && MediaItemAspect.TryGetAspects(item.Aspects, metadata, out aspects))
          {
            List<string> values = new List<string>();
            foreach (MultipleMediaItemAspect multiAspect in aspects)
            {
              if (!string.IsNullOrEmpty(multiAspect.GetAttributeValue<string>(attr)))
                values.Add(multiAspect.GetAttributeValue<string>(attr));
            }
            if (values.Count > 0)
            {
              if (_preSortAttributes)
                values.Sort();
              return values.FirstOrDefault();
            }
          }
        }
      }
      return null;
    }
  }
}
