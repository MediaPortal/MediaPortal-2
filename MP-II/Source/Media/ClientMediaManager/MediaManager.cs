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
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// The client's media manager class. It holds all media providers and metadata extractors and
  /// provides the concept of "views".
  /// </summary>
  public class MediaManager : MediaManagerBase, IImporter, ILocalSharesManagement
  {
    #region Protected fields

    protected LocalSharesManagement _localLocalSharesManagement;

    #endregion

    #region Ctor & initialization

    public MediaManager()
    {
      _localLocalSharesManagement = new LocalSharesManagement();

      ServiceScope.Get<ILogger>().Debug("MediaManager: Registering global SharesManagement service");
      ServiceScope.Add<ILocalSharesManagement>(this);
    }

    public override void Initialize()
    {
      base.Initialize();
      ServiceScope.Get<ILogger>().Info("MediaManager: Initialize");
      _localLocalSharesManagement.LoadSharesFromSettings();
      if (_localLocalSharesManagement.Shares.Count == 0)
      { // The shares are still uninitialized - use defaults
        foreach (ShareDescriptor share in CreateDefaultShares())
          _localLocalSharesManagement.Shares.Add(share.ShareId, share);
        _localLocalSharesManagement.SaveSharesToSettings();
      }
    }

    #endregion

    #region IImporter implementation

    public void ForceImport(Guid? shareId, string path)
    {
      // TODO
      throw new System.NotImplementedException();
    }

    #endregion

    #region ILocalSharesManagement implementation

    public IDictionary<Guid, ShareDescriptor> Shares
    {
      get { return _localLocalSharesManagement.Shares; }
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      ShareDescriptor result = _localLocalSharesManagement.GetShare(shareId);
      // TODO: When connected and result == null, call method at the MP server's ILocalSharesManagement interface
      return result;
    }

    public ShareDescriptor RegisterShare(Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor result = _localLocalSharesManagement.RegisterShare(providerId, path,
          shareName, mediaCategories, metadataExtractorIds);
      // TODO: When connected, add share to the media library
      return result;
    }

    public void RemoveShare(Guid shareId)
    {
      // TODO: When connected, also remove the share from the media library
      _localLocalSharesManagement.RemoveShare(shareId);
    }

    public ShareDescriptor UpdateShare(Guid shareId, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, bool relocateMediaItems)
    {
      ShareDescriptor sd = _localLocalSharesManagement.UpdateShare(shareId, providerId, path,
          shareName, mediaCategories, metadataExtractorIds);
      // TODO: Trigger re-import and relocate media items (if relocateMediaItems is set)
      // TODO: When connected, also update the share at the media library
      return sd;
    }

    #endregion
  }
}
