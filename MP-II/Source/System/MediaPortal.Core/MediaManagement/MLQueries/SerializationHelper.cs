#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  public class SerializationHelper
  {
    public static string SerializeAttributeTypeReference(MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      return attributeType.ParentMIAM.AspectId + ":" + attributeType.AttributeName;
    }

    public static MediaItemAspectMetadata.AttributeSpecification DeserializeAttributeTypeReference(string atStr)
    {
      int index = atStr.IndexOf(':');
      IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
      Guid aspectId = new Guid(atStr.Substring(0, index));
      string attributeName = atStr.Substring(index + 1);
      MediaItemAspectMetadata miam;
      MediaItemAspectMetadata.AttributeSpecification result;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam) ||
          !miam.AttributeSpecifications.TryGetValue(attributeName, out result))
      {
        ServiceScope.Get<ILogger>().Warn("SortInformation: Could not deserialize SortInformation '{0}'", atStr);
        return null;
      }
      return result;
    }
  }
}