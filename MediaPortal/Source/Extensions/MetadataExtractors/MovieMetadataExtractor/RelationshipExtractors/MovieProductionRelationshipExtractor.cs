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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieProductionRelationshipExtractor : IMovieRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { MovieAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CompanyAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return MovieAspect.ROLE_MOVIE; }
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

    public Guid[] MatchAspects
    {
      get { return CompanyInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetCompanySearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      if (!MovieMetadataExtractor.IncludeProductionCompanyDetails)
        return false;

      if (importOnly)
        return false;

      if (BaseInfo.IsVirtualResource(aspects))
        return false;

      MovieInfo movieInfo = new MovieInfo();
      if (!movieInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(movieInfo))
        return false;
       
      int count = 0;
      if (!MovieMetadataExtractor.SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.UpdateCompanies(movieInfo, CompanyAspect.COMPANY_PRODUCTION, importOnly);
        count = movieInfo.ProductionCompanies.Where(c => c.HasExternalId).Count();
        if (!movieInfo.IsRefreshed)
          movieInfo.HasChanged = true; //Force save to update external Ids for metadata found by other MDEs
      }
      else
      {
        count = movieInfo.ProductionCompanies.Where(c => !string.IsNullOrEmpty(c.Name)).Count();
      }

      if (movieInfo.ProductionCompanies.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < count || (BaseInfo.CountRelationships(aspects, LinkedRole) == 0 && movieInfo.ProductionCompanies.Count > 0))
        movieInfo.HasChanged = true; //Force save if no relationship exists

      if (!movieInfo.HasChanged)
        return false;

      AddToCheckCache(movieInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      foreach (CompanyInfo company in movieInfo.ProductionCompanies)
      {
        company.AssignNameId();
        company.HasChanged = movieInfo.HasChanged;
        IDictionary<Guid, IList<MediaItemAspect>> companyAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        company.SetMetadata(companyAspects);

        if (companyAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          Guid existingId;
          if (TryGetIdFromCache(company, out existingId))
            extractedLinkedAspects.Add(new RelationshipItem(companyAspects, existingId));
          else
            extractedLinkedAspects.Add(new RelationshipItem(companyAspects, Guid.Empty));
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
      if (!MediaItemAspect.TryGetAspect(aspects, MovieAspect.Metadata, out aspect))
        return false;

      IEnumerable<string> companies = aspect.GetCollectionAttribute<string>(MovieAspect.ATTR_COMPANIES);
      List<string> nameList = new List<string>(companies);

      index = nameList.IndexOf(name);
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      CompanyInfo company = new CompanyInfo();
      company.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, company);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
