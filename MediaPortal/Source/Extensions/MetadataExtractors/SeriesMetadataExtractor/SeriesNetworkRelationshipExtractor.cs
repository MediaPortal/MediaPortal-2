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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesNetworkRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CompanyAspect.ASPECT_ID };

    public Guid Role
    {
      get { return SeriesAspect.ROLE_SERIES; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return CompanyAspect.ROLE_COMPANY; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      SingleMediaItemAspect seriesAspect;
      if (!MediaItemAspect.TryGetAspect(aspects, SeriesAspect.Metadata, out seriesAspect))
        return false;

      IEnumerable<string> networks = seriesAspect.GetCollectionAttribute<string>(SeriesAspect.ATTR_NETWORKS);
     
      // Build the person MI

      List<CompanyInfo> companys = new List<CompanyInfo>();
      if (networks != null)
        foreach (string company in networks)
          companys.Add(new CompanyInfo() { Name = company, Type = CompanyType.TVNetwork });

      SeriesInfo seriesInfo;
      if (!SeriesBaseTryExtractRelationships.GetBaseInfo(aspects, out seriesInfo))
        return false;

      SeriesTheMovieDbMatcher.Instance.UpdateSeriesCompanys(seriesInfo, companys, CompanyType.TVNetwork);
      SeriesTvMazeMatcher.Instance.UpdateSeriesCompanys(seriesInfo, companys, CompanyType.TVNetwork);
      SeriesTvDbMatcher.Instance.UpdateSeriesCompanys(seriesInfo, companys, CompanyType.TVNetwork);

      if (companys.Count == 0)
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      foreach (CompanyInfo company in companys)
      {
        IDictionary<Guid, IList<MediaItemAspect>> companyAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        extractedLinkedAspects.Add(companyAspects);
        company.SetMetadata(companyAspects);
      }
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(CompanyAspect.ASPECT_ID))
        return false;

      int linkedType;
      if (!MediaItemAspect.TryGetAttribute(linkedAspects, CompanyAspect.ATTR_COMPANY_TYPE, out linkedType))
        return false;

      int existingType;
      if (!MediaItemAspect.TryGetAttribute(existingAspects, CompanyAspect.ATTR_COMPANY_TYPE, out existingType))
        return false;

      return linkedType == existingType;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(aspects, SeriesAspect.ATTR_NETWORKS, out index);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
