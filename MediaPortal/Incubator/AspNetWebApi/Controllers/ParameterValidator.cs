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
  /// <summary>
  /// Helper class to validate parameters received from a WebApi request
  /// </summary>
  /// <remarks>
  /// All public methods of this class throw a <see cref="HttpException"/> with <see cref="HttpStatusCode.BadRequest"/>
  /// if the validation of the respective parameters fails.
  /// </remarks>
  public static class ParameterValidator
  {
    #region Public methods

    /// <summary>
    /// Validates MediaItemAspectId parameters
    /// </summary>
    /// <param name="necessaryMiaIds">Required MediaItemAspectIds</param>
    /// <param name="optionalMiaIds">Optional MediaItemAspectIds</param>
    /// <param name="logger"><see cref="ILogger"/> used to log warnings and debug information</param>
    /// <remarks>
    /// - Checks if the given <paramref name="necessaryMiaIds"/> and <paramref name="optionalMiaIds"/> exist in the local <see cref="IMediaItemAspectTypeRegistration"/>.
    /// - Additionally, if no <paramref name="optionalMiaIds"/> are given, sets the <paramref name="optionalMiaIds"/> to all locally known MiaIds except
    ///   those requested as <paramref name="necessaryMiaIds"/>.
    /// - If <paramref name="necessaryMiaIds"/> or <paramref name="optionalMiaIds"/> are <c>null</c>, they are set to an empty array.
    /// </remarks>
    public static void ValidateMiaIds(ref Guid[] necessaryMiaIds, ref Guid[] optionalMiaIds, ILogger logger = null)
    {
      necessaryMiaIds = necessaryMiaIds ?? new Guid[0];
      optionalMiaIds = optionalMiaIds ?? new Guid[0];
      var locallyKnownMiaIds = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes.Keys;

      var unknownMiaIds = necessaryMiaIds.Except(locallyKnownMiaIds).ToList();
      if (unknownMiaIds.Any())
        NotifyBadQueryString("necessaryMiaÍds contained the following unknown MIA IDs", string.Join(",", unknownMiaIds), logger);

      unknownMiaIds = optionalMiaIds.Except(locallyKnownMiaIds).ToList();
      if (unknownMiaIds.Any())
        NotifyBadQueryString("optionalMiaIds contained the following unknown MIA IDs", string.Join(",", unknownMiaIds), logger);

      if (!optionalMiaIds.Any())
        optionalMiaIds = locallyKnownMiaIds.Except(necessaryMiaIds).ToArray();
    }

    /// <summary>
    /// Parses <paramref name="sortInformationStrings"/>, validates them and returns a list of parsed <see cref="SortInformation"/> objects
    /// </summary>
    /// <param name="sortInformationStrings">Array of strings each representing a <see cref="SortInformation"/> object</param>
    /// <param name="logger"><see cref="ILogger"/> used to log warnings and debug information</param>
    /// <returns>A list of parsed <see cref="SortInformation"/> objects</returns>
    /// <remarks>
    /// A sortInformationString has the form "[MediaItemAspectId].[AttributeName].[SortDirection]".
    /// [SortDirection] can be "Ascending" or "Descending". The ".[SortDirection]" part is optional; if omitted, "Ascending" is assumed.
    /// </remarks>
    /// <example>
    /// "493f2b3b-8025-4db1-80dc-c3cd39683c9f.Album.Descending" sorts by the AudioAspect's Album Attribute in a descending way.
    /// </example>
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

    /// <summary>
    /// Parses an <paramref name="attributeString"/>, validates it and returns the corresponding <see cref="MediaItemAspectMetadata.AttributeSpecification"/>
    /// </summary>
    /// <param name="attributeString">A string representing an Attribute</param>
    /// <param name="logger"><see cref="ILogger"/> used to log warnings and debug information</param>
    /// <returns>A parsed <see cref="MediaItemAspectMetadata.AttributeSpecification"/> object</returns>
    /// <remarks>An attributeString has the form "[MediaItemAspectId].[AttributeName]"</remarks>
    /// <example>"493f2b3b-8025-4db1-80dc-c3cd39683c9f.Album" represents the Album Attribute of the AudioAspect</example>
    public static MediaItemAspectMetadata.AttributeSpecification ValidateAttribute(string attributeString, ILogger logger = null)
    {
      attributeString = attributeString ?? string.Empty;
      var subStrings = attributeString.Split('.');

      if (subStrings.Count() != 2)
        NotifyBadQueryString("Invalid Attribute", attributeString, logger);

      Guid miaId;
      if (!Guid.TryParse(subStrings[0], out miaId))
        NotifyBadQueryString("Invalid MIA Id in Attribute", attributeString, logger);

      var locallyKnownMias = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>().LocallyKnownMediaItemAspectTypes;
      if (!locallyKnownMias.ContainsKey(miaId))
        NotifyBadQueryString("Unknown MIA Id in Attribute", attributeString, logger);

      var miam = locallyKnownMias[miaId];
      MediaItemAspectMetadata.AttributeSpecification result;
      if (!miam.AttributeSpecifications.TryGetValue(subStrings[1], out result))
        NotifyBadQueryString("Unknown AttributeName", attributeString, logger);

      return result;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Logs a warning about a bad parameter and throws a BadRequest-Exception.
    /// </summary>
    /// <param name="message">Information about what is wrong with the parameter</param>
    /// <param name="parameter">Parameter that is wrong</param>
    /// <param name="logger"><see cref="ILogger"/> used to log the warning</param>
    private static void NotifyBadQueryString(string message, string parameter, ILogger logger)
    {
      var fullMessage = $"Bad QueryString: {message} ('{parameter}')";
      logger?.LogWarning(fullMessage);
      throw new HttpException(HttpStatusCode.BadRequest, fullMessage);
    }

    #endregion
  }
}
