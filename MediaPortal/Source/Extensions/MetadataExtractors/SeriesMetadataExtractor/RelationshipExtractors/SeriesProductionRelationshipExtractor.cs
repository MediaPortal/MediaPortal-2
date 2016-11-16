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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesProductionRelationshipExtractor : ISeriesRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CompanyAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

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

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetCompanySearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      extractedLinkedAspects = null;

      if (!SeriesMetadataExtractor.IncludeProductionCompanyDetails)
        return false;

      if (importOnly)
        return false;

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(seriesInfo))
        return false;

      if (!SeriesMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateSeriesCompanies(seriesInfo, CompanyAspect.COMPANY_PRODUCTION, importOnly);

      if (seriesInfo.ProductionCompanies.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < seriesInfo.ProductionCompanies.Count)
        seriesInfo.HasChanged = true; //Force save if no relationship exists

      if (!seriesInfo.HasChanged && !importOnly)
        return false;

      AddToCheckCache(seriesInfo);

      extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
      foreach (CompanyInfo company in seriesInfo.ProductionCompanies)
      {
        company.AssignNameId();
        company.HasChanged = seriesInfo.HasChanged;
        IDictionary<Guid, IList<MediaItemAspect>> companyAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        company.SetMetadata(companyAspects);

        if (companyAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          Guid existingId;
          if (TryGetIdFromStudioCache(company, out existingId))
            extractedLinkedAspects.Add(companyAspects, existingId);
          else
            extractedLinkedAspects.Add(companyAspects, Guid.Empty);
        }
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(CompanyAspect.ASPECT_ID))
        return false;

      CompanyInfo linkedCompany = new CompanyInfo();
      if (!linkedCompany.FromMetadata(extractedAspects))
        return false;

      CompanyInfo existingCompany = new CompanyInfo();
      if (!existingCompany.FromMetadata(existingAspects))
        return false;

      return linkedCompany.Equals(existingCompany);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, CompanyAspect.Metadata, out linkedAspect))
        return false;

      string name = linkedAspect.GetAttributeValue<string>(CompanyAspect.ATTR_COMPANY_NAME);

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, SeriesAspect.Metadata, out aspect))
        return false;

      IEnumerable<object> actors = aspect.GetCollectionAttribute<object>(SeriesAspect.ATTR_COMPANIES);
      List<string> nameList = new List<string>(actors.Cast<string>());

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      CompanyInfo company = new CompanyInfo();
      company.FromMetadata(extractedAspects);
      AddToStudioCache(extractedItemId, company);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
