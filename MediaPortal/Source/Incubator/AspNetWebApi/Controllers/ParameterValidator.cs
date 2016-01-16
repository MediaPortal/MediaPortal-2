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
using System.Net;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using Microsoft.Extensions.Logging;

namespace MediaPortal.Plugins.AspNetWebApi.Controllers
{
  public static class ParameterValidator
  {
    public static readonly ICollection<Guid> LOCALLY_KNOWN_MIA_IDS = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes.Keys;
    public static readonly IDictionary<Guid, MediaItemAspectMetadata> LOCALLY_KNOWN_MIAS = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes;

    public static void ValidateMiaIds(ref Guid[] necessaryMiaIds, ref Guid[] optionalMiaIds, ILogger logger = null)
    {
      necessaryMiaIds = necessaryMiaIds ?? new Guid[0];
      optionalMiaIds = optionalMiaIds ?? new Guid[0];

      var unknownMiaIds = necessaryMiaIds.Except(LOCALLY_KNOWN_MIA_IDS).ToList();
      if (unknownMiaIds.Any())
        NotifyBadQueryString("necessaryMiaÍds contained the following unknown MIA IDs", string.Join(",", unknownMiaIds), logger);

      unknownMiaIds = optionalMiaIds.Except(LOCALLY_KNOWN_MIA_IDS).ToList();
      if (unknownMiaIds.Any())
        NotifyBadQueryString("optionalMiaIds contained the following unknown MIA IDs", string.Join(",", unknownMiaIds), logger);

      if (!optionalMiaIds.Any())
        optionalMiaIds = LOCALLY_KNOWN_MIA_IDS.Except(necessaryMiaIds).ToArray();
    }

    public static List<SortInformation> ValidateSortInformation(string[] sortInformationStrings, ILogger logger = null)
    {
      sortInformationStrings = sortInformationStrings ?? new string[0];

      var result = new List<SortInformation>();
      foreach (var sortInformationString in sortInformationStrings)
      {
        var subStrings = sortInformationString?.Split('.');
        if (subStrings == null || subStrings.Count() < 2 || subStrings.Count() > 3 || subStrings.Any(string.IsNullOrEmpty))
          NotifyBadQueryString("Invalid sortInfomration", sortInformationString, logger);

        var attributeString = string.Join(".", subStrings?[0], subStrings?[1]);
        var attributeType = ValidateAttribute(attributeString, logger);

        var sortDirection = SortDirection.Ascending;
        if (subStrings?.Count() == 3 && !Enum.TryParse(subStrings[2], out sortDirection))
          NotifyBadQueryString("Invald sortDirection", subStrings[2], logger);

        result.Add(new SortInformation(attributeType, sortDirection));
      }
      return result;
    }

    public static MediaItemAspectMetadata.AttributeSpecification ValidateAttribute(string attributeString, ILogger logger = null)
    {
      attributeString = attributeString ?? string.Empty;
      var subStrings = attributeString.Split('.');

      if (subStrings.Count() != 2)
        NotifyBadQueryString("Invalid Attribute", attributeString, logger);

      Guid miaId;
      if (!Guid.TryParse(subStrings[0], out miaId))
        NotifyBadQueryString("Invalid MIA Id in Attribute", attributeString, logger);
      if (!LOCALLY_KNOWN_MIA_IDS.Contains(miaId))
        NotifyBadQueryString("Unknown MIA Id in Attribute", attributeString, logger);

      var miam = LOCALLY_KNOWN_MIAS[miaId];
      MediaItemAspectMetadata.AttributeSpecification result;
      if (!miam.AttributeSpecifications.TryGetValue(subStrings[1], out result))
        NotifyBadQueryString("Unknown AttributeName", attributeString, logger);

      return result;
    }

    public static void NotifyBadQueryString(string message, string parameter, ILogger logger)
    {
      var fullMessage = $"Bad QueryString: {message} ('{parameter}')";
      logger?.LogWarning(fullMessage);
      throw new HttpException(HttpStatusCode.BadRequest, fullMessage);
    }

  }
}
