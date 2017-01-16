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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using System.Linq;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class AlbumLabelRelationshipExtractor : IAudioRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { AudioAlbumAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { CompanyAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return AudioAlbumAspect.ROLE_ALBUM; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return CompanyAspect.ROLE_MUSIC_LABEL; }
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

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      extractedLinkedAspects = null;

      if (!AudioMetadataExtractor.IncludeMusicLabelDetails)
        return false;

      if (importOnly)
        return false;

      AlbumInfo albumInfo = new AlbumInfo();
      if (!albumInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(albumInfo))
        return false;
       
      int count = 0;
      if (!AudioMetadataExtractor.SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.UpdateAlbumCompanies(albumInfo, CompanyAspect.COMPANY_MUSIC_LABEL, importOnly);
        count = albumInfo.MusicLabels.Where(c => c.HasExternalId).Count();
        if (!albumInfo.IsRefreshed)
          albumInfo.HasChanged = true; //Force save to update external Ids for metadata found by other MDEs
      }
      else
      {
        count = albumInfo.MusicLabels.Where(c => !string.IsNullOrEmpty(c.Name)).Count();
      }

      if (albumInfo.MusicLabels.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < count || (BaseInfo.CountRelationships(aspects, LinkedRole) == 0 && albumInfo.MusicLabels.Count > 0))
        albumInfo.HasChanged = true; //Force save if no relationship exists

      if (!albumInfo.HasChanged)
        return false;

      AddToCheckCache(albumInfo);

      extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
      foreach (CompanyInfo company in albumInfo.MusicLabels)
      {
        company.AssignNameId();
        company.HasChanged = albumInfo.HasChanged;
        IDictionary<Guid, IList<MediaItemAspect>> companyAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        company.SetMetadata(companyAspects);

        if (companyAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        {
          Guid existingId;
          if (TryGetIdFromCache(company, out existingId))
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
      if (!MediaItemAspect.TryGetAspect(aspects, AudioAlbumAspect.Metadata, out aspect))
        return false;

      IEnumerable<string> labels = aspect.GetCollectionAttribute<string>(AudioAlbumAspect.ATTR_LABELS);
      List<string> nameList = new List<string>(labels);

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
