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
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Services.UPnP
{
  public enum RelocationMode
  {
    Relocate,
    ClearAndReImport
  }

  public enum SharesFilter
  {
    All,
    ConnectedShares
  }

  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPContentDirectoryService
  {
    protected CpService _serviceStub;

    public UPnPContentDirectoryService(CpService serviceStub)
    {
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    // We could also provide the asynchronous counterparts of the following methods... do we need them?

    // Shares management
    public Guid RegisterShare(SystemName nativeSystem, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("RegisterShare", out action))
        throw new FatalException("Method 'RegisterShare' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object>
        {
            nativeSystem.HostName,
            providerId.ToString("B"),
            path,
            shareName,
            StringUtils.Join(",", mediaCategories)
        };
      ICollection<string> metadataExtractorIdStrings = new List<string>();
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        metadataExtractorIdStrings.Add(metadataExtractorId.ToString("B"));
      inParameters.Add(StringUtils.Join(",", metadataExtractorIdStrings));
      IList<object> outParameters = action.InvokeAction(inParameters);
      return new Guid((string) outParameters[0]);
    }

    public void RemoveShare(Guid shareId)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("RemoveShare", out action))
        throw new FatalException("Method 'RemoveShare' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {shareId.ToString("B")};
      action.InvokeAction(inParameters);
    }

    public int UpdateShare(Guid shareId, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, RelocationMode relocationMode)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("UpdateShare", out action))
        throw new FatalException("Method 'UpdateShare' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object>
        {
            shareId.ToString("B"),
            providerId.ToString("B"),
            path,
            shareName,
            StringUtils.Join(",", mediaCategories)
        };
      ICollection<string> metadataExtractorIdStrings = new List<string>();
      foreach (Guid metadataExtractorId in metadataExtractorIds)
        metadataExtractorIdStrings.Add(metadataExtractorId.ToString("B"));
      inParameters.Add(StringUtils.Join(",", metadataExtractorIdStrings));
      string relocationModeStr;
      switch (relocationMode)
      {
        case RelocationMode.Relocate:
          relocationModeStr = "Relocate";
          break;
        case RelocationMode.ClearAndReImport:
          relocationModeStr = "ClearAndReImport";
          break;
        default:
          throw new NotImplementedException(string.Format("RelocationMode '{0}' is not implemented", relocationMode));
      }
      inParameters.Add(relocationModeStr);
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (int) outParameters[0];
    }

    public ICollection<Share> GetShares(SystemName system, SharesFilter sharesFilter)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("GetShares", out action))
        throw new FatalException("Method 'GetShares' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {system.HostName};
      String sharesFilterStr;
      switch (sharesFilter)
      {
        case SharesFilter.All:
          sharesFilterStr = "All";
          break;
        case SharesFilter.ConnectedShares:
          sharesFilterStr = "ConnectedShares";
          break;
        default:
          throw new NotImplementedException(string.Format("SharesFilter '{0}' is not implemented", sharesFilter));
      }
      inParameters.Add(sharesFilterStr);
      IList<object> outParameters = action.InvokeAction(inParameters);
      return new List<Share>((IEnumerable<Share>) outParameters[0]);
    }

    public Share GetShare(Guid shareId)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("GetShare", out action))
        throw new FatalException("Method 'GetShare' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {shareId.ToString("B")};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (Share) outParameters[0];
    }

    // Client management
    public void ConnectClient()
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("ConnectClient", out action))
        throw new FatalException("Method 'ConnectClient' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {SystemName.GetLocalSystemName()};
      action.InvokeAction(inParameters);
    }

    public void DisconnectClient()
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("DisconnectClient", out action))
        throw new FatalException("Method 'DisconnectClient' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {SystemName.GetLocalSystemName()};
      action.InvokeAction(inParameters);
    }

    // Media item aspect storage management
    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("AddMediaItemAspectStorage", out action))
        throw new FatalException("Method 'AddMediaItemAspectStorage' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {miam};
      action.InvokeAction(inParameters);
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("RemoveMediaItemAspectStorage", out action))
        throw new FatalException("Method 'RemoveMediaItemAspectStorage' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {aspectId.ToString("B")};
      action.InvokeAction(inParameters);
    }

    public ICollection<Guid> GetAllManagedMediaItemAspectMetadataIds()
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("GetAllManagedMediaItemAspectMetadataIds", out action))
        throw new FatalException("Method 'GetAllManagedMediaItemAspectMetadataIds' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object>();
      IList<object> outParameters = action.InvokeAction(inParameters);
      ICollection<Guid> result = new List<Guid>();
      foreach (string miamIdStr in (IEnumerable<string>) outParameters[0])
        result.Add(new Guid(miamIdStr));
      return result;
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId)
    {
      CpAction action;
      if (!_serviceStub.Actions.TryGetValue("GetMediaItemAspectMetadata", out action))
        throw new FatalException("Method 'GetMediaItemAspectMetadata' is not present in the connected MP-II UPnP server");
      IList<object> inParameters = new List<object> {miamId.ToString("B")};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaItemAspectMetadata) outParameters[0];
    }

    // TODO: State variables, if present
  }
}
