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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Settings;
using MediaPortal.Utilities;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// Shares management class for client-local shares. All shares are managed redundantly at
  /// the client's media manager via this class and at the MediaPortal server's MediaLibrary.
  /// </summary>
  public class SharesManagement : ISharesManagement
  {
    #region Protected fields

    protected IDictionary<Guid, ShareDescriptor> _shares = new Dictionary<Guid, ShareDescriptor>();

    #endregion

    #region Ctor

    public SharesManagement()
    {
      LoadSharesFromSettings();
    }

    #endregion

    protected void LoadSharesFromSettings()
    {
      // FIXME: Test-Code, creates a single share. This is only temporary code and will be
      // removed as soon as the shares management GUI is ready (Albert78, 2008-11-15)
      Guid shareId = new Guid("{04A9B3CA-0808-42e8-A0A4-B2C65D19945A}");
      Guid fsProviderId = new Guid("{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}");
      Guid musicMetadataExtractor = new Guid("{817FEE2E-8690-4355-9F24-3BDC65AEDFFE}");
      ShareDescriptor sd = new ShareDescriptor(shareId, SystemName.Loopback(), fsProviderId,
          "D:\\Tmp\\MP-Test", "Test-Share", null, new Guid[]
          {
            musicMetadataExtractor
          });
      _shares.Add(shareId, sd);
      return;
      //////////// FIXME: End test code
      SharesSettings settings = ServiceScope.Get<ISettingsManager>().Load<SharesSettings>();
      ICollection<ShareDescriptor> shares = settings.LocalShares;
      if (shares != null)
        foreach (ShareDescriptor share in shares)
          _shares.Add(share.ShareId, share);
    }

    protected void SaveSharesToSettings()
    {
      SharesSettings settings = ServiceScope.Get<ISettingsManager>().Load<SharesSettings>();
      ICollection<ShareDescriptor> shares = new List<ShareDescriptor>();
      CollectionUtils.AddAll(shares, _shares.Values);
      ServiceScope.Get<ISettingsManager>().Save(shares);
    }

    #region Implementation of IShareManagement

    public ShareDescriptor RegisterShare(SystemName systemName, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = ShareDescriptor.CreateNewShare(systemName, providerId, path,
          shareName, mediaCategories, metadataExtractorIds);
      _shares.Add(sd.ShareId, sd);
      return sd;
    }

    public void RemoveShare(Guid shareId)
    {
      _shares.Remove(shareId);
    }

    public IDictionary<Guid, ShareDescriptor> GetShares()
    {
      return _shares;
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      return _shares.ContainsKey(shareId) ? _shares[shareId] : null;
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      if (SystemName.GetLocalNames().Contains(systemName))
        return _shares;
      return new Dictionary<Guid, ShareDescriptor>();
    }

    public ICollection<SystemName> GetManagedClients()
    {
      return SystemName.GetLocalNames();
    }

    public IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName)
    {
      IDictionary<Guid, MetadataExtractorMetadata> result = new Dictionary<Guid, MetadataExtractorMetadata>();
      if (SystemName.GetLocalNames().Contains(systemName))
        foreach (MetadataExtractorMetadata metadata in
            ServiceScope.Get<MediaManager>().LocalMetadataExtractors.Values)
          result.Add(metadata.MetadataExtractorId, metadata);
      return result;
    }

    public void AddMetadataExtractorsToShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = GetShare(shareId);
      if (sd == null)
        return;
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        sd.MetadataExtractors.Add(metadataExtractorId);
      ServiceScope.Get<MediaManager>().InvalidateMediaItemsFromShare(shareId);
    }

    public void RemoveMetadataExtractorsFromShare(Guid shareId, IEnumerable<Guid> metadataExtractorIds)
    {
      ShareDescriptor sd = GetShare(shareId);
      if (sd == null)
        return;
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        sd.MetadataExtractors.Remove(metadataExtractorId);
      ServiceScope.Get<MediaManager>().InvalidateMediaItemsFromShare(shareId);
    }

    #endregion
  }
}
