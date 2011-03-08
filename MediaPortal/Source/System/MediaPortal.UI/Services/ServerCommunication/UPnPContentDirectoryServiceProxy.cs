#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.UPnP;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPContentDirectoryServiceProxy : UPnPServiceProxyBase, IContentDirectory
  {
    public UPnPContentDirectoryServiceProxy(CpService serviceStub) : base(serviceStub, "ContentDirectory") { }

    // We could also provide the asynchronous counterparts of the following methods... do we need them?

    protected string SerializeOnlineState(bool onlyOnline)
    {
      return onlyOnline ? "OnlyOnline" : "All";
    }

    protected string SerializeExcludeClobs(bool excludeCLOBs)
    {
      return excludeCLOBs ? "ExcludeCLOBs" : "Normal";
    }

    protected string SerializeCapitalizationMode(bool caseSensitive)
    {
      return caseSensitive ? "CaseSensitive" : "CaseInsensitive";
    }

    protected string SerializeProjectionFunction(ProjectionFunction projectionFunction)
    {
      switch (projectionFunction)
      {
        case ProjectionFunction.None:
          return "None";
        case ProjectionFunction.DateToYear:
          return "DateToYear";
        default:
          throw new NotImplementedException(string.Format("ProjectionFunction '{0}' is not implemented", projectionFunction));
      }
    }

    #region Shares management

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
        case RelocationMode.None:
          relocationModeStr = "None";
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

    #endregion

    #region Media item aspect storage management

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
      return miaTypeIDs.Split(',').Select(MarshallingHelper.DeserializeGuid).ToList();
    }

    public MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId)
    {
      CpAction action = GetAction("GetMediaItemAspectMetadata");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(miamId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaItemAspectMetadata) outParameters[0];
    }

    #endregion

    #region Media query

    public MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      CpAction action = GetAction("LoadItem");
      IList<object> inParameters = new List<object> {systemId, path.Serialize(),
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (MediaItem) outParameters[0];
    }

    public ICollection<MediaItem> Browse(Guid parentDirectory,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes)
    {
      CpAction action = GetAction("Browse");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(parentDirectory),
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<MediaItem>) outParameters[0];
    }

    public IList<MediaItem> Search(MediaItemQuery query, bool onlyOnline)
    {
      CpAction action = GetAction("Search");
      String onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object> {query, onlineStateStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public IList<MediaItem> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes,
        IEnumerable<Guid> optionalMIATypes, IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive)
    {
      CpAction action = GetAction("SimpleTextSearch");
      String searchModeStr = SerializeExcludeClobs(excludeCLOBs);
      String onlineStateStr = SerializeOnlineState(onlyOnline);
      String capitalizationMode = SerializeCapitalizationMode(caseSensitive);
      IList<object> inParameters = new List<object> {searchText,
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
          filter, searchModeStr, onlineStateStr, capitalizationMode};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline)
    {
      CpAction action = GetAction("GetValueGroups");
      string projectionFunctionStr = SerializeProjectionFunction(projectionFunction);
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
          attributeType.AttributeName, projectionFunctionStr, MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes), filter, onlineStateStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (HomogenousMap) outParameters[0];
    }

    public IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline,
        GroupingFunction groupingFunction)
    {
      CpAction action = GetAction("GroupValueGroups");
      string projectionFunctionStr = SerializeProjectionFunction(projectionFunction);
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      string groupingFunctionStr;
      switch (groupingFunction)
      {
        case GroupingFunction.FirstCharacter:
          groupingFunctionStr = "FirstCharacter";
          break;
        default:
          throw new NotImplementedException(string.Format("GroupingFunction '{0}' is not implemented", groupingFunction));
      }
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
          attributeType.AttributeName, projectionFunctionStr, MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes), filter, onlineStateStr,
          groupingFunctionStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MLQueryResultGroup>) outParameters[0];
    }

    public int CountMediaItems(IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline)
    {
      CpAction action = GetAction("CountMediaItems");
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes), filter, onlineStateStr};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (int) outParameters[0];
    }

    #endregion

    #region Playlist management

    public ICollection<PlaylistInformationData> GetPlaylists()
    {
      CpAction action = GetAction("GetPlaylists");
      IList<object> outParameters = action.InvokeAction(null);
      return (ICollection<PlaylistInformationData>) outParameters[0];
    }

    public void SavePlaylist(PlaylistRawData playlistData)
    {
      CpAction action = GetAction("SavePlaylist");
      IList<object> inParameters = new List<object> {playlistData};
      action.InvokeAction(inParameters);
    }

    public bool DeletePlaylist(Guid playlistId)
    {
      CpAction action = GetAction("DeletePlaylist");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(playlistId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    public PlaylistRawData ExportPlaylist(Guid playlistId)
    {
      CpAction action = GetAction("ExportPlaylist");
      IList<object> inParameters = new List<object> {playlistId};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (PlaylistRawData) outParameters[0];
    }

    public PlaylistContents LoadServerPlaylist(Guid playlistId,
      ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes)
    {
      CpAction action = GetAction("LoadServerPlaylist");
      IList<object> inParameters = new List<object> {
            MarshallingHelper.SerializeGuid(playlistId),
            MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
            MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (PlaylistContents) outParameters[0];
    }

    public IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
      ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes)
    {
      CpAction action = GetAction("LoadCustomPlaylist");
      IList<object> inParameters = new List<object> {
            MarshallingHelper.SerializeGuidEnumerationToCsv(mediaItemIds),
            MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
            MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    #endregion

    #region Media import

    public Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      CpAction action = GetAction("AddOrUpdateMediaItem");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(parentDirectoryId),
            systemId,
            path.Serialize(),
            mediaItemAspects
        };
      IList<object> outParameters = action.InvokeAction(inParameters);
      return MarshallingHelper.DeserializeGuid((string) outParameters[0]);
    }

    public void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive)
    {
      CpAction action = GetAction("DeleteMediaItemOrPath");
      IList<object> inParameters = new List<object> {systemId, path.Serialize(), inclusive};
      action.InvokeAction(inParameters);
    }

    #endregion

    // TODO: State variables, if present
  }
}
