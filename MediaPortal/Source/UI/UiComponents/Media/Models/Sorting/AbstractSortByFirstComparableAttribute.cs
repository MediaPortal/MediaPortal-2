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
    protected MediaItemAspectMetadata.SingleAttributeSpecification _attr;
    protected MediaItemAspectMetadata.MultipleAttributeSpecification _multiAttr;

    protected AbstractSortByFirstComparableAttribute(MediaItemAspectMetadata.SingleAttributeSpecification attr)
    {
      _attr = attr;
    }

    protected AbstractSortByFirstComparableAttribute(MediaItemAspectMetadata.MultipleAttributeSpecification attr)
    {
      _multiAttr = attr;
    }

    public override int Compare(MediaItem x, MediaItem y)
    {
      if (_attr != null)
      {
        SingleMediaItemAspect aspectX;
        SingleMediaItemAspect aspectY;
        SingleMediaItemAspectMetadata metadata = _attr.ParentMIAM;
        string firstValueX = null;
        string firstValueY = null;
        if (MediaItemAspect.TryGetAspect(x.Aspects, metadata, out aspectX))
        {
          IEnumerable<string> valueList = aspectX.GetCollectionAttribute<string>(_attr);
          if (valueList != null)
          {
            List<string> valuesX = new List<string>(valueList);
            valuesX.Sort();
            firstValueX = valuesX.FirstOrDefault();
          }
        }
        if (MediaItemAspect.TryGetAspect(y.Aspects, metadata, out aspectY))
        {
          IEnumerable<string> valueList = aspectY.GetCollectionAttribute<string>(_attr);
          if (valueList != null)
          {
            List<string> valuesY = new List<string>(valueList);
            valuesY.Sort();
            firstValueY = valuesY.FirstOrDefault();
          }
        }
        return ObjectUtils.Compare(firstValueX, firstValueY);
      }
      else if (_multiAttr != null)
      {
        IList<MultipleMediaItemAspect> aspectsX;
        IList<MultipleMediaItemAspect> aspectsY;
        MultipleMediaItemAspectMetadata metadata = _multiAttr.ParentMIAM;
        string firstValueX = null;
        string firstValueY = null;
        if (_multiAttr.IsCollectionAttribute)
        {
          if (MediaItemAspect.TryGetAspects(x.Aspects, metadata, out aspectsX))
          {
            MultipleMediaItemAspect aspectX = aspectsX[0];
            IEnumerable<string> valueList = aspectX.GetCollectionAttribute<string>(_multiAttr);
            if (valueList != null)
            {
              List<string> valuesX = new List<string>(valueList);
              valuesX.Sort();
              firstValueX = valuesX.FirstOrDefault();
            }
          }
          if (MediaItemAspect.TryGetAspects(y.Aspects, metadata, out aspectsY))
          {
            MultipleMediaItemAspect aspectY = aspectsY[0];
            IEnumerable<string> valueList = aspectY.GetCollectionAttribute<string>(_multiAttr);
            if (valueList != null)
            {
              List<string> valuesY = new List<string>(valueList);
              valuesY.Sort();
              firstValueY = valuesY.FirstOrDefault();
            }
          }
        }
        else
        {
          if (MediaItemAspect.TryGetAspects(x.Aspects, metadata, out aspectsX))
          {
            List<string> valuesX = new List<string>();
            foreach (MultipleMediaItemAspect aspectX in aspectsX)
            {
              if (!string.IsNullOrEmpty(aspectX.GetAttributeValue<string>(_multiAttr)))
                valuesX.Add(aspectX.GetAttributeValue<string>(_multiAttr));
            }
            if (valuesX.Count > 0)
            {
              valuesX.Sort();
              firstValueX = valuesX.FirstOrDefault();
            }
          }
          if (MediaItemAspect.TryGetAspects(y.Aspects, metadata, out aspectsY))
          {
            List<string> valuesY = new List<string>();
            foreach (MultipleMediaItemAspect aspectY in aspectsY)
            {
              if (!string.IsNullOrEmpty(aspectY.GetAttributeValue<string>(_multiAttr)))
                valuesY.Add(aspectY.GetAttributeValue<string>(_multiAttr));
            }
            if (valuesY.Count > 0)
            {
              valuesY.Sort();
              firstValueX = valuesY.FirstOrDefault();
            }
          }
        }
        return ObjectUtils.Compare(firstValueX, firstValueY);
      }
      return 0;
    }

    public override object GetGroupByValue(MediaItem item)
    {
      if (_attr != null)
      {
        IList<MediaItemAspect> aspect;
        Guid aspectId = _attr.ParentMIAM.AspectId;
        if (item.Aspects.TryGetValue(aspectId, out aspect))
        {
          IEnumerable<string> valueList = aspect.First().GetCollectionAttribute<string>(_attr);
          if (valueList != null)
          {
            List<string> values = new List<string>(valueList);
            values.Sort();
            return values.FirstOrDefault();
          }
        }
      }
      else if (_multiAttr != null)
      {
        IList<MultipleMediaItemAspect> aspects;
        MultipleMediaItemAspectMetadata metadata = _multiAttr.ParentMIAM;
        if (_multiAttr.IsCollectionAttribute)
        {
          if (MediaItemAspect.TryGetAspects(item.Aspects, metadata, out aspects))
          {
            MultipleMediaItemAspect aspect = aspects[0];
            IEnumerable<string> valueList = aspect.GetCollectionAttribute<string>(_multiAttr);
            if (valueList != null)
            {
              List<string> valuesX = new List<string>(valueList);
              valuesX.Sort();
              return valuesX.FirstOrDefault();
            }
          }
        }
        else
        {
          if (MediaItemAspect.TryGetAspects(item.Aspects, metadata, out aspects))
          {
            List<string> values = new List<string>();
            foreach (MultipleMediaItemAspect aspectX in aspects)
            {
              if (!string.IsNullOrEmpty(aspectX.GetAttributeValue<string>(_multiAttr)))
                values.Add(aspectX.GetAttributeValue<string>(_multiAttr));
            }
            if (values.Count > 0)
            {
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
