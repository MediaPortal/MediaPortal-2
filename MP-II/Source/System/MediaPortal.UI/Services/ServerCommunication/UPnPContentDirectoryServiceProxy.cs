#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPContentDirectoryServiceProxy : IContentDirectory
  {
    protected CpService _serviceStub;

    public UPnPContentDirectoryServiceProxy(CpService serviceStub)
    {
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    protected CpAction GetAction(string actionName)
    {
      CpAction result;
      if (!_serviceStub.Actions.TryGetValue(actionName, out result))
        throw new FatalException("Method '{0}' is not present in the connected MP-II ContentDirectoryService", actionName);
      return result;
    }

    // We could also provide the asynchronous counterparts of the following methods... do we need them?

    // Shares management
    public void RegisterShare(Share share)
    {
      CpAction action = GetAction("RegisterShare");
      IList<object> inParameters = new List<object>
        {
            share
        };
      action.InvokeAction(inParameters);
    }

    public void RemoveShare(Guid shareId)
    {
      CpAction action = GetAction("RemoveShare");
      IList<object> inParameters = new List<object> {shareId.ToString("B")};
      action.InvokeAction(inParameters);
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      CpAction action = GetAction("UpdateShare");
      IList<object> inParameters = new List<object>
        {
            shareId.ToString("B"),
            baseResourcePath.Serialize(),
            shareName,
            StringUtils.Join(",", mediaCategories)
        };
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

    public ICollection<Share> GetShares(string systemId, SharesFilter sharesFilter)
    {
      CpAction action = GetAction("GetShares");
      IList<object> inParameters = new List<object> {systemId};
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
      CpAction action = GetAction("GetShare");
      IList<object> inParameters = new List<object> {shareId.ToString("B")};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (Share) outParameters[0];
    }

    // Media item aspect storage management
    public void AddMediaItemAspectStorage(MediaItemAspectMetadata miam)
    {
      CpAction action = GetAction("AddMediaItemAspectStorage");
      IList<object> inParameters = new List<object> {miam};
      action.InvokeAction(inParameters);
    }

    public void RemoveMediaItemAspectStorage(Guid aspectId)
    {
      CpAction action = GetAction("RemoveMediaItemAspectStorage");
      IList<object> inParameters = new List<object> {aspectId.ToString("B")};
      action.InvokeAction(inParameters);
    }

    public ICollection<Guid> GetAllManagedMediaItemAspectTypes()
    {
      CpAction action = GetAction("GetAllManagedMediaItemAspectTypes");
      IList<object> inParameters = new List<object>();
      IList<object> outParameters = action.InvokeAction(inParameters);
      string miaTypeIDs = (string) outParameters[0];
      ICollection<Guid> result = new List<Guid>();
      foreach (string miamIdStr in miaTypeIDs.Split(','))
        result.Add(new Guid(miamIdStr));
      return result;
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId)
    {
      CpAction action = GetAction("GetMediaItemAspectMetadata");
      IList<object> inParameters = new List<object> {miamId.ToString("B")};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaItemAspectMetadata) outParameters[0];
    }

    // Media query
    public IList<MediaItem> Search(MediaItemQuery query)
    {
      CpAction action = GetAction("Search");
      IList<object> inParameters = new List<object> {query};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public ICollection<MediaItem> Browse(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      CpAction action = GetAction("Browse");
      IList<object> inParameters = new List<object> {systemId, path, necessaryMIATypes, optionalMIATypes};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<MediaItem>) outParameters[0];
    }

    public HomogenousCollection GetDistinctAssociatedValues(Guid aspectId, string attributeName, IFilter filter)
    {
      CpAction action = GetAction("GetDistinctAssociatedValues");
      IList<object> inParameters = new List<object> {aspectId, attributeName, filter};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (HomogenousCollection) outParameters[0];
    }

    // Media import
    public void AddOrUpdateMediaItem(string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      CpAction action = GetAction("AddOrUpdateMediaItem");
      IList<object> inParameters = new List<object> {systemId, path, mediaItemAspects};
      action.InvokeAction(inParameters);
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path)
    {
      CpAction action = GetAction("DeleteMediaItemOrPath");
      IList<object> inParameters = new List<object> {systemId, path};
      action.InvokeAction(inParameters);
    }

    // TODO: State variables, if present
  }
}
