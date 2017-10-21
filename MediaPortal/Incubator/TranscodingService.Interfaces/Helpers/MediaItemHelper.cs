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
using System;
using System.Collections.Generic;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Helpers
{
    public class MediaItemHelper
    {
		public static object GetAttributeValue(IDictionary<Guid, IList<MediaItemAspect>> aspects, SingleMediaItemAspectMetadata.SingleAttributeSpecification attribute)
		{
		  object value = null;

		  if (!MediaItemAspect.TryGetAttribute(aspects, attribute, out value))
			return null;

		  return value;
		}

        public static IEnumerable<T> GetCollectionAttributeValues<T>(IDictionary<Guid, IList<MediaItemAspect>> aspects, SingleMediaItemAspectMetadata.SingleAttributeSpecification attribute)
        {
            IEnumerable<T> value = null;

            if (!MediaItemAspect.TryGetAttribute(aspects, attribute, out value))
                return null;

            return value;
        }

        public static object GetAttributeValue(IDictionary<Guid, IList<MediaItemAspect>> aspects, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
		{
		  List<object> values = null;
		  
		  if (!MediaItemAspect.TryGetAttribute(aspects, attribute, out values))
			return null;

		  if (values.Count == 0)
			return null;

		  return values[0];
		}

        public static IEnumerable<T> GetCollectionAttributeValues<T>(IDictionary<Guid, IList<MediaItemAspect>> aspects, MediaItemAspectMetadata.MultipleAttributeSpecification attribute)
        {
            List<object> values = null;

            if (!MediaItemAspect.TryGetAttribute(aspects, attribute, out values))
                return null;

            if (values.Count == 0)
                return null;

            return (IEnumerable<T>)values[0];
        }

        public static IList<MultipleMediaItemAspect> GetAspects(IDictionary<Guid, IList<MediaItemAspect>> aspects, MultipleMediaItemAspectMetadata aspect)
		{
			IList<MultipleMediaItemAspect> values = null;

			if (!MediaItemAspect.TryGetAspects(aspects, aspect, out values))
				return null;

			return values;
		}
    }
}
