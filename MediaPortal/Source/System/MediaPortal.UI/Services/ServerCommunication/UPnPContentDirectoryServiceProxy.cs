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
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPContentDirectoryServiceProxy : IContentDirectory
  {
    protected CpService _serviceStub;

    public UPnPContentDirectoryServiceProxy(CpService serviceStub)
    {
      _serviceStub = serviceStub;
      _serviceStub.SubscribeStateVariables();
    }

    static bool ParseValidity(string validityStr)
    {
      switch (validityStr)
      {
        case "Valid":
          return true;
        case "Invalid":
          return false;
        default:
          throw new UPnPRemoteException(new UPnPError(402, string.Format("Validity argument '{0}' could not be parsed", validityStr)));
      }
    }

    protected CpAction GetAction(string actionName)
    {
      CpAction result;
      if (!_serviceStub.Actions.TryGetValue(actionName, out result))
        throw new FatalException("Method '{0}' is not present in the connected MP 2 ContentDirectoryService", actionName);
      return result;
    }

    // We could also provide the asynchronous counterparts of the following methods... do we need them?

    // Shares management

    public void RegisterShare(Share share)
    {
      CpAction action = GetAction("RegisterShare");
      IList<object> inParameters = new List<object> {share};
      action.InvokeAction(inParameters);
    }

    public void RemoveShare(Guid shareId)
    {
      CpAction action = GetAction("RemoveShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      action.InvokeAction(inParameters);
    }

    public int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      CpAction action = GetAction("UpdateShare");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(shareId),
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
      String onlineStateStr;
      switch (sharesFilter)
      {
        case SharesFilter.All:
          onlineStateStr = "All";
          break;
        case SharesFilter.ConnectedShares:
          onlineStateStr = "OnlyOnline";
          break;
        default:
          throw new NotImplementedException(string.Format("SharesFilter '{0}' is not implemented", sharesFilter));
      }
      inParameters.Add(onlineStateStr);
      IList<object> outParameters = action.InvokeAction(inParameters);
      return new List<Share>((IEnumerable<Share>) outParameters[0]);
    }

    public Share GetShare(Guid shareId)
    {
      CpAction action = GetAction("GetShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (Share) outParameters[0];
    }

    public void ReImportShare(Guid shareId)
    {
      CpAction action = GetAction("ReImportShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      action.InvokeAction(inParameters);
    }

    public ICollection<string> GetMediaCategoriesFromMetadataExtractors()
    {
      CpAction action = GetAction("GetMediaCategoriesFromMetadataExtractors");
      IList<object> outParameters = action.InvokeAction(null);
      return new List<string>(((string) outParameters[0]).Split(','));
    }

    public ICollection<MediaProviderMetadata> GetAllBaseMediaProviderMetadata()
    {
      CpAction action = GetAction("GetAllBaseMediaProviderMetadata");
      IList<object> outParameters = action.InvokeAction(null);
      return (ICollection<MediaProviderMetadata>) outParameters[0];
    }

    public MediaProviderMetadata GetMediaProviderMetadata(Guid mediaProviderId)
    {
      CpAction action = GetAction("GetMediaProviderMetadata");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(mediaProviderId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaProviderMetadata) outParameters[0];
    }

    public string GetResourcePathDisplayName(ResourcePath path)
    {
      CpAction action = GetAction("GetResourcePathDisplayName");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (string) outParameters[0];
    }

    public string GetResourceDisplayName(ResourcePath path)
    {
      CpAction action = GetAction("GetResourceDisplayName");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (string) outParameters[0];
    }

    public ICollection<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      CpAction action = GetAction("GetChildDirectoriesData");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<ResourcePathMetadata>) outParameters[0];
    }

    public bool GetIsPathValid(ResourcePath path)
    {
      CpAction action = GetAction("GetIsPathValid");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return ParseValidity((string) outParameters[0]);
    }

    public ResourcePath ExpandResourcePathFromString(Guid mediaProviderId, string path)
    {
      CpAction action = GetAction("ExpandResourcePathFromString");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(mediaProviderId), path};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return ResourcePath.Deserialize((string) outParameters[0]);
    }

    public bool DoesMediaProviderSupportTreeListing(Guid mediaProviderId)
    {
      CpAction action = GetAction("DoesMediaProviderSupportTreeListing");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(mediaProviderId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
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
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(aspectId)};
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
        result.Add(MarshallingHelper.DeserializeGuid(miamIdStr));
      return result;
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId)
    {
      CpAction action = GetAction("GetMediaItemAspectMetadata");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(miamId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaItemAspectMetadata) outParameters[0];
    }

    // Media query

    public IList<MediaItem> Search(MediaItemQuery query, bool onlyOnline)
    {
      CpAction action = GetAction("Search");
      String onlineStateStr = onlyOnline ? "OnlyOnline" : "All";
      IList<object> inParameters = new List<object> {query, onlineStateStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public IList<ValueGroup> GroupSearch(MediaItemQuery query, MediaItemAspectMetadata.AttributeSpecification groupingAttributeType,
        bool onlyOnline, GroupingFunction groupingFunction)
    {
      CpAction action = GetAction("Search");
      string onlineStateStr = onlyOnline ? "OnlyOnline" : "All";
      string groupingFunctionStr;
      switch (groupingFunction)
      {
        case GroupingFunction.FirstLetter:
          groupingFunctionStr = "FirstLetter";
          break;
        default:
          throw new NotImplementedException(string.Format("GroupingFunction '{0}' is not implemented", groupingFunction));
      }
      IList<object> inParameters = new List<object> {query,
          MarshallingHelper.SerializeGuid(groupingAttributeType.ParentMIAM.AspectId),
          groupingAttributeType.AttributeName, onlineStateStr, groupingFunctionStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<ValueGroup>) outParameters[0];
    }

    public IList<MediaItem> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes,
        IEnumerable<Guid> optionalMIATypes, IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive)
    {
      CpAction action = GetAction("SimpleTextSearch");
      String searchModeStr = excludeCLOBs ? "ExcludeCLOBs" : "Normal";
      String onlineStateStr = onlyOnline ? "OnlyOnline" : "All";
      String capitalizationMode = caseSensitive ? "CaseSensitive" : "CaseInsensitive";
      IList<object> inParameters = new List<object> {searchText,
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
          filter, searchModeStr, onlineStateStr, capitalizationMode};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public ICollection<MediaItem> Browse(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, bool onlyOnline)
    {
      CpAction action = GetAction("Browse");
      String onlineStateStr = onlyOnline ? "OnlyOnline" : "All";
      IList<object> inParameters = new List<object> {systemId, path.Serialize(),
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes), onlineStateStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<MediaItem>) outParameters[0];
    }

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypes, IFilter filter)
    {
      CpAction action = GetAction("GetValueGroups");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
          attributeType.AttributeName, MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes), filter};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (HomogenousMap) outParameters[0];
    }

    public IList<ValueGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IEnumerable<Guid> necessaryMIATypes, IFilter filter, GroupingFunction groupingFunction)
    {
      CpAction action = GetAction("GroupValueGroups");
      string groupingFunctionStr;
      switch (groupingFunction)
      {
        case GroupingFunction.FirstLetter:
          groupingFunctionStr = "FirstLetter";
          break;
        default:
          throw new NotImplementedException(string.Format("GroupingFunction '{0}' is not implemented", groupingFunction));
      }
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
          attributeType.AttributeName, MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes), filter, groupingFunctionStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<ValueGroup>) outParameters[0];
    }

    // Media import

    public void AddOrUpdateMediaItem(string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      CpAction action = GetAction("AddOrUpdateMediaItem");
      IList<object> inParameters = new List<object> {systemId, path.Serialize(), mediaItemAspects};
      action.InvokeAction(inParameters);
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path)
    {
      CpAction action = GetAction("DeleteMediaItemOrPath");
      IList<object> inParameters = new List<object> {systemId, path.Serialize()};
      action.InvokeAction(inParameters);
    }

    // TODO: State variables, if present
  }
}
