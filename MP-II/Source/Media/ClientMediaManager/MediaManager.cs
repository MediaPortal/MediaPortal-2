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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// The client's media manager class. It holds all media providers and metadata extractors and
  /// provides the concept of "views".
  /// </summary>
  public class MediaManager : MediaManagerBase, IImporter, ISharesManagement
  {
    #region Protected fields

    protected LocalSharesManagement _localLocalSharesManagement;

    #endregion

    #region Ctor & initialization

    public MediaManager()
    {
      _localLocalSharesManagement = new LocalSharesManagement();

      ServiceScope.Get<ILogger>().Debug("MediaManager: Registering global SharesManagement service");
      ServiceScope.Add<ISharesManagement>(this);
    }

    public override void Initialize()
    {
      base.Initialize();
      ServiceScope.Get<ILogger>().Info("MediaManager: Startup");
      _localLocalSharesManagement.LoadSharesFromSettings();
    }

    #endregion

    #region IImporter implementation

    public void ForceImport(Guid? shareId, string path)
    {
      // TODO
      throw new System.NotImplementedException();
    }

    #endregion

    #region ISharesManagement implementation

    public ShareDescriptor RegisterShare(SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      // TODO: When connected, assign result from the call of the method at the MP server's
      // ISharesManagement interface
      ShareDescriptor result = null;
      if (nativeSystem.IsLocalSystem())
        result = _localLocalSharesManagement.RegisterShare(nativeSystem, providerId, path,
            shareName, mediaCategories, metadataExtractorIds);
      MediaManagerMessaging.SendShareMessage(MediaManagerMessaging.MessageType.ShareAdded, result.ShareId);
      return result;
    }

    public void RemoveShare(Guid shareId)
    {
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
      _localLocalSharesManagement.RemoveShare(shareId);
      MediaManagerMessaging.SendShareMessage(MediaManagerMessaging.MessageType.ShareRemoved, shareId);
    }

    public ShareDescriptor UpdateShare(Guid shareId, SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds,
        bool relocateMediaItems)
    {
      ShareDescriptor sd = _localLocalSharesManagement.UpdateShare(shareId, nativeSystem, providerId, path,
          shareName, mediaCategories, metadataExtractorIds, relocateMediaItems);
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
      MediaManagerMessaging.SendShareMessage(MediaManagerMessaging.MessageType.ShareChanged, shareId);
      return sd;
    }

    public IDictionary<Guid, ShareDescriptor> GetShares()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface instead of
      // calling it on the local shares management
      return _localLocalSharesManagement.GetShares();
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      ShareDescriptor result = _localLocalSharesManagement.GetShare(shareId);
      // TODO: When connected and result == null, call method at the MP server's ISharesManagement interface
      return result;
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      if (systemName.IsLocalSystem())
        return _localLocalSharesManagement.GetSharesBySystem(systemName);
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface and return
        // its results
        return new Dictionary<Guid, ShareDescriptor>();
    }

    public ICollection<SystemName> GetManagedClients()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface
      return _localLocalSharesManagement.GetManagedClients();
    }

    public IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName)
    {
      if (systemName.IsLocalSystem())
        return _localLocalSharesManagement.GetMetadataExtractorsBySystem(SystemName.GetLocalSystemName());
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface
        return new Dictionary<Guid, MetadataExtractorMetadata>();
    }

    #endregion
  }
}
