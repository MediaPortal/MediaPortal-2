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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.MetaData
{
  public static class ProviderResourceExtensions
  {
    public static MultipleMediaItemAspect PrimaryProviderResourceAspect(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryResources[mediaItem.ActiveResourceLocatorIndex];
    }

    public static string PrimaryProviderResourcePath(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryProviderResourceAspect().GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
    }
  }

  public static class MediaItemAspectExtension
  {
    public static MediaItemAspect GetAspect(this MediaItem mediaItem, SingleMediaItemAspectMetadata aspectMetadata)
    {
      return mediaItem[aspectMetadata.AspectId][0];
    }

    public static T GetAttributeEnum<T>(this MediaItemAspect aspect, MediaItemAspectMetadata.AttributeSpecification attributeSpecification) where T : struct
    {
      T oEnum;
      if (!Enum.TryParse(aspect.GetAttributeValue<string>(attributeSpecification), out oEnum))
        oEnum = default(T);
      return oEnum;
    }

    public static object GetAttributeValue(this MediaItem mediaItem, MediaItemAspectMetadata.SingleAttributeSpecification attribute)
    {
      object value = null;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attribute, out value))
        return null;

      return value;
    }

    public static IEnumerable<T> GetCollectionAttributeValues<T>(this MediaItem mediaItem, MediaItemAspectMetadata.SingleAttributeSpecification attribute)
    {
      IEnumerable<T> value = null;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attribute, out value))
        return null;

      return value;
    }

    public static object GetAttributeValue(this MediaItem mediaItem, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      List<object> values = null;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attribute, out values))
        return null;

      if (values.Count == 0)
        return null;

      return values[0];
    }

    public static IEnumerable<T> GetCollectionAttributeValues<T>(this MediaItem mediaItem, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
    {
      List<object> values = null;

      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attribute, out values))
        return null;

      if (values.Count == 0)
        return null;

      return (IEnumerable<T>)values[0];
    }

    public static IList<MultipleMediaItemAspect> GetAspects(this MediaItem mediaItem, MultipleMediaItemAspectMetadata aspect)
    {
      IList<MultipleMediaItemAspect> values = null;

      if (!MediaItemAspect.TryGetAspects(mediaItem.Aspects, aspect, out values))
        return null;

      return values;
    }

    public static bool IsLiveTvItem(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryProviderResourceAspect().GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE) == LiveTvMediaItem.MIME_TYPE_TV;
    }

    public static bool IsLiveRadioItem(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryProviderResourceAspect().GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE) == LiveTvMediaItem.MIME_TYPE_RADIO;
    }
  }
}
