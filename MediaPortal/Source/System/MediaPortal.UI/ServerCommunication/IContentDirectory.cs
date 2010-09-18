#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.UI.ServerCommunication
{
  public enum SharesFilter
  {
    All,
    ConnectedShares
  }

  public enum  GroupingFunction
  {
    FirstCharacter
  }

  /// <summary>
  /// Interface of the MediaPortal 2 server's ContentDirectory service. This interface is implemented by the
  /// MediaPortal 2 server.
  /// </summary>
  public interface IContentDirectory
  {
    // Shares management
    void RegisterShare(Share share);
    void RemoveShare(Guid shareId);
    int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode);
    ICollection<Share> GetShares(string systemId, SharesFilter sharesFilter);
    Share GetShare(Guid shareId);
    void ReImportShare(Guid guid);

    // Media item aspect storage management
    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);
    void RemoveMediaItemAspectStorage(Guid aspectId);
    ICollection<Guid> GetAllManagedMediaItemAspectTypes();
    MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId);

    // Media query
    IList<MediaItem> Search(MediaItemQuery query, bool onlyOnline);
    IList<MLQueryResultGroup> GroupSearch(MediaItemQuery query, MediaItemAspectMetadata.AttributeSpecification groupingAttributeType,
        bool onlyOnline, GroupingFunction groupingFunction);
    IList<MediaItem> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
        IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive);
    ICollection<MediaItem> Browse(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, bool onlyOnline);
    HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypes, IFilter filter);
    IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypes, IFilter filter, GroupingFunction groupingFunction);

    // Media import
    void AddOrUpdateMediaItem(string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects);
    void DeleteMediaItemOrPath(string systemId, ResourcePath path);
  }
}
