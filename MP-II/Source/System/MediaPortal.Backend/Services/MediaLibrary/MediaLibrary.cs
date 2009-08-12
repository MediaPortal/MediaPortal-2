#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.MediaLibrary;

namespace MediaPortal.Services.MediaLibrary
{
  // TODO: Check that when implementing methods of IGlobalSharesManagement (RegisterShare, UpdateShare, ...),
  // the querying system only may call those methods with its own system name - else we might get attacked
  public class MediaLibrary : IMediaLibrary
  {
    #region IGlobalSharesManagement implementation

    public ShareDescriptor RegisterShare(SystemName nativeSystem, Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      sdf
    }

    public void RemoveShare(Guid shareId)
    {
      asd
    }

    public ShareDescriptor UpdateShare(Guid shareId, Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, bool relocateMediaItems)
    {
      asd
    }

    public IDictionary<Guid, ShareDescriptor> GetShares(bool onlyConnectedShares)
    {
      asdf
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      asdf
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      asdf
    }

    public ICollection<SystemName> GetManagedClients()
    {
      asdf
    }

    #endregion

    #region IMediaLibrary implementation

    public void Startup()
    {
      weiter:
        - Überprüfen, ob das Schema auf der richtigen Version ist; ggf. aktualisieren
    }

    public ICollection<MediaItem> Search(string query)
    {
      asdf
    }

    public ICollection<MediaItem> Browse(MediaItem parent)
    {
      asdf
    }

    public void Import()
    {
      asdf
    }

    public bool MediaItemAspectStorageExists(MediaItemAspectMetadata miam)
    {
      asdf
    }

    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      asdf
    }

    public void RemoveMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      asdf
    }

    #endregion
  }
}
