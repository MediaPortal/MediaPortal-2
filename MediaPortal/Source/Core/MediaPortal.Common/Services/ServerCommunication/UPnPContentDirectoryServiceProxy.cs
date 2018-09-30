#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPContentDirectoryServiceProxy : UPnPServiceProxyBase, IContentDirectory
  {
    protected const string SV_PLAYLISTS_CHANGE_COUNTER = "PlaylistsChangeCounter";
    protected const string SV_MIA_TYPE_REGISTRATIONS_CHANGE_COUNTER = "MIATypeRegistrationsChangeCounter";
    protected const string SV_REGISTERED_SHARES_CHANGE_COUNTER = "RegisteredSharesChangeCounter";

    public UPnPContentDirectoryServiceProxy(CpService serviceStub) : base(serviceStub, "ContentDirectory")
    {
      serviceStub.StateVariableChanged += OnStateVariableChanged;
      serviceStub.SubscribeStateVariables();
    }

    private void OnStateVariableChanged(CpStateVariable statevariable, object newValue)
    {
      if (statevariable.Name == SV_PLAYLISTS_CHANGE_COUNTER)
        FirePlaylistsChanged();
      else if (statevariable.Name == SV_MIA_TYPE_REGISTRATIONS_CHANGE_COUNTER)
        FireMIATypeRegistrationsChanged();
      else if (statevariable.Name == SV_REGISTERED_SHARES_CHANGE_COUNTER)
        FireRegisteredSharesChangeCounterChanged();
    }

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

    protected void FirePlaylistsChanged()
    {
      ParameterlessMethod dlgt = PlaylistsChanged;
      if (dlgt != null)
        dlgt();
    }

    protected void FireMIATypeRegistrationsChanged()
    {
      ParameterlessMethod dlgt = MIATypeRegistrationsChanged;
      if (dlgt != null)
        dlgt();
    }

    protected void FireRegisteredSharesChangeCounterChanged()
    {
      ParameterlessMethod dlgt = RegisteredSharesChangeCounterChanged;
      if (dlgt != null)
        dlgt();
    }

    #region State variables

    // We don't make those events available via the public interface because .net event registrations are not allowed between MP2 modules.
    // It is the job of the class which instanciates this class to publicize those events.

    public event ParameterlessMethod PlaylistsChanged;
    public event ParameterlessMethod MIATypeRegistrationsChanged;
    public event ParameterlessMethod RegisteredSharesChangeCounterChanged;

    #endregion

    #region Shares management

    public async Task RegisterShareAsync(Share share)
    {
      CpAction action = GetAction("X_MediaPortal_RegisterShare");
      IList<object> inParameters = new List<object> {share};
      await action.InvokeAsync(inParameters);
    }

    public async Task RemoveShareAsync(Guid shareId)
    {
      CpAction action = GetAction("X_MediaPortal_RemoveShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      await action.InvokeAsync(inParameters);
    }

    public async Task<int> UpdateShareAsync(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode)
    {
      CpAction action = GetAction("X_MediaPortal_UpdateShare");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(shareId),
            baseResourcePath.Serialize(),
            shareName,
            useShareWatcher,
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
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (int)(uint) outParameters[0];
    }

    public async Task<ICollection<Share>> GetSharesAsync(string systemId, SharesFilter sharesFilter)
    {
      CpAction action = GetAction("X_MediaPortal_GetShares");
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
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return new List<Share>((IEnumerable<Share>) outParameters[0]);
    }

    public async Task<Share> GetShareAsync(Guid shareId)
    {
      CpAction action = GetAction("X_MediaPortal_GetShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (Share) outParameters[0];
    }

    public async Task ReImportShareAsync(Guid shareId)
    {
      CpAction action = GetAction("X_MediaPortal_ReImportShare");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      await action.InvokeAsync(inParameters);
    }

    public async Task SetupDefaultServerSharesAsync()
    {
      CpAction action = GetAction("X_MediaPortal_SetupDefaultServerShares");
      await action.InvokeAsync(null);
    }

    #endregion

    #region Media item aspect storage management

    public async Task AddMediaItemAspectStorageAsync(MediaItemAspectMetadata miam)
    {
      CpAction action = GetAction("X_MediaPortal_AddMediaItemAspectStorage");
      IList<object> inParameters = new List<object> {miam};
      await action.InvokeAsync(inParameters);
    }

    public async Task RemoveMediaItemAspectStorageAsync(Guid aspectId)
    {
      CpAction action = GetAction("X_MediaPortal_RemoveMediaItemAspectStorage");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(aspectId)};
      await action.InvokeAsync(inParameters);
    }

    public async Task<ICollection<Guid>> GetAllManagedMediaItemAspectTypesAsync()
    {
      CpAction action = GetAction("X_MediaPortal_GetAllManagedMediaItemAspectTypes");
      IList<object> inParameters = new List<object>();
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      string miaTypeIDs = (string) outParameters[0];
      return miaTypeIDs.Split(',').Select(MarshallingHelper.DeserializeGuid).ToList();
    }

    public async Task<IDictionary<Guid, DateTime>> GetAllManagedMediaItemAspectCreationDatesAsync()
    {
      CpAction action = GetAction("GetAllManagedMediaItemAspectCreationDates");
      IList<object> inParameters = new List<object>();
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IDictionary<Guid, DateTime>) outParameters[0];
    }

    public async Task<MediaItemAspectMetadata> GetMediaItemAspectMetadataAsync(Guid miamId)
    {
      CpAction action = GetAction("X_MediaPortal_GetMediaItemAspectMetadata");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(miamId)};
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (MediaItemAspectMetadata) outParameters[0];
    }

    #endregion

    #region Media query

    public async Task<MediaItem> LoadItemAsync(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile)
    {
      CpAction action = GetAction("X_MediaPortal_LoadItem");
      IList<object> inParameters = new List<object> {systemId, path.Serialize(),
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
          userProfile.HasValue ? MarshallingHelper.SerializeGuid(userProfile.Value) : null };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (MediaItem) outParameters[0];
    }

    public async Task<MediaItem> LoadItemAsync(string systemId, Guid mediaItemId,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile)
    {
      CpAction action = GetAction("X_MediaPortal_LoadItemId");
      IList<object> inParameters = new List<object> {systemId, MarshallingHelper.SerializeGuid(mediaItemId),
          MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
          MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
          userProfile.HasValue ? MarshallingHelper.SerializeGuid(userProfile.Value) : null };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (MediaItem)outParameters[0];
    }

    public async Task RefreshMediaItemMetadataAsync(Guid mediaItemId, bool clearMetadata)
    {
      CpAction action = GetAction("X_MediaPortal_RefreshMediaItem");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(mediaItemId), clearMetadata };
      await action.InvokeAsync(inParameters);
    }

    public async Task ReimportMediaItemMetadataAsync(Guid mediaItemId, IEnumerable<MediaItemAspect> matchedAspects)
    {
      CpAction action = GetAction("X_MediaPortal_ReimportMediaItem");
      IList<object> inParameters = new List<object> { MarshallingHelper.SerializeGuid(mediaItemId), matchedAspects };
      await action.InvokeAsync(inParameters);
    }

    public async Task<IList<MediaItem>> BrowseAsync(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
        Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("X_MediaPortal_Browse");
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(parentDirectoryId),
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
        userProfile.HasValue ? MarshallingHelper.SerializeGuid(userProfile.Value) : null,
        includeVirtual,
        offset,
        limit,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MediaItem>)outParameters[0];
    }

    public async Task<IList<MediaItem>> SearchAsync(MediaItemQuery query, bool onlyOnline, Guid? userProfile, bool includeVirtual,  uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("X_MediaPortal_Search");
      String onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object>
      {
        query,
        onlineStateStr,
        userProfile.HasValue ? MarshallingHelper.SerializeGuid(userProfile.Value) : null,
        includeVirtual,
        offset,
        limit,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    public async Task<IList<MediaItem>> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
      IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive,
      Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("X_MediaPortal_SimpleTextSearch");
      String searchModeStr = SerializeExcludeClobs(excludeCLOBs);
      String onlineStateStr = SerializeOnlineState(onlyOnline);
      String capitalizationMode = SerializeCapitalizationMode(caseSensitive);
      IList<object> inParameters = new List<object>
      {
        searchText,
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
        filter,
        searchModeStr,
        onlineStateStr,
        capitalizationMode,
        userProfile.HasValue ? MarshallingHelper.SerializeGuid(userProfile.Value) : null,
        includeVirtual,
        offset,
        limit,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MediaItem>)outParameters[0];
    }

    public async Task<HomogenousMap> GetValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
    {
      CpAction action = GetAction("X_MediaPortal_GetValueGroups");
      string projectionFunctionStr = SerializeProjectionFunction(projectionFunction);
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
        attributeType.AttributeName,
        selectAttributeFilter,
        projectionFunctionStr,
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        filter,
        onlineStateStr,
        includeVirtual,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (HomogenousMap) outParameters[0];
    }

    public async Task<Tuple<HomogenousMap, HomogenousMap>> GetKeyValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType,
      IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
    {
      CpAction action = GetAction("X_MediaPortal_GetKeyValueGroups");
      string projectionFunctionStr = SerializeProjectionFunction(projectionFunction);
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(keyAttributeType.ParentMIAM.AspectId),
        keyAttributeType.AttributeName,
        MarshallingHelper.SerializeGuid(valueAttributeType.ParentMIAM.AspectId),
        valueAttributeType.AttributeName,
        selectAttributeFilter,
        projectionFunctionStr,
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        filter,
        onlineStateStr,
        includeVirtual,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return new Tuple<HomogenousMap, HomogenousMap>((HomogenousMap)outParameters[0], (HomogenousMap)outParameters[1]);
    }

    public async Task<IList<MLQueryResultGroup>> GroupValueGroupsAsync(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes,
        IFilter filter, bool onlyOnline, GroupingFunction groupingFunction, bool includeVirtual)
    {
      CpAction action = GetAction("X_MediaPortal_GroupValueGroups");
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
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(attributeType.ParentMIAM.AspectId),
        attributeType.AttributeName, selectAttributeFilter,
        projectionFunctionStr,
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        filter,
        onlineStateStr,
        groupingFunctionStr,
        includeVirtual,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MLQueryResultGroup>) outParameters[0];
    }

    public async Task<int> CountMediaItemsAsync(IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual)
    {
      CpAction action = GetAction("X_MediaPortal_CountMediaItems");
      string onlineStateStr = SerializeOnlineState(onlyOnline);
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
        filter,
        onlineStateStr,
        includeVirtual,
      };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (int)(uint) outParameters[0];
    }

    #endregion

    #region Playlist management

    public async Task<ICollection<PlaylistInformationData>> GetPlaylistsAsync()
    {
      CpAction action = GetAction("X_MediaPortal_GetPlaylists");
      IList<object> outParameters = await action.InvokeAsync(null);
      return (ICollection<PlaylistInformationData>) outParameters[0];
    }

    public async Task SavePlaylistAsync(PlaylistRawData playlistData)
    {
      CpAction action = GetAction("X_MediaPortal_SavePlaylist");
      IList<object> inParameters = new List<object> {playlistData};
      await action.InvokeAsync(inParameters);
    }

    public async Task<bool> DeletePlaylistAsync(Guid playlistId)
    {
      CpAction action = GetAction("X_MediaPortal_DeletePlaylist");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(playlistId)};
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (bool) outParameters[0];
    }

    public async Task<PlaylistRawData> ExportPlaylistAsync(Guid playlistId)
    {
      CpAction action = GetAction("X_MediaPortal_ExportPlaylist");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(playlistId)};
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (PlaylistRawData) outParameters[0];
    }

    public async Task<IList<MediaItem>> LoadCustomPlaylistAsync(IList<Guid> mediaItemIds,
      ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes,
      uint? offset = null, uint? limit = null)
    {
      CpAction action = GetAction("X_MediaPortal_LoadCustomPlaylist");
      IList<object> inParameters = new List<object> {
            MarshallingHelper.SerializeGuidEnumerationToCsv(mediaItemIds),
            MarshallingHelper.SerializeGuidEnumerationToCsv(necessaryMIATypes),
            MarshallingHelper.SerializeGuidEnumerationToCsv(optionalMIATypes),
            offset, limit};
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MediaItem>) outParameters[0];
    }

    #endregion

    #region Media import

    public async Task<Guid> AddOrUpdateMediaItemAsync(Guid parentDirectoryId, string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      CpAction action = GetAction("X_MediaPortal_AddOrUpdateMediaItem");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(parentDirectoryId),
            systemId,
            path.Serialize(),
            mediaItemAspects
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return MarshallingHelper.DeserializeGuid((string) outParameters[0]);
    }

    public async Task<Guid> AddOrUpdateMediaItemAsync(Guid parentDirectoryId, string systemId, ResourcePath path,
        Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects)
    {
      CpAction action = GetAction("X_MediaPortal_AddOrUpdateMediaItemId");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(parentDirectoryId),
            systemId,
            path.Serialize(),
            MarshallingHelper.SerializeGuid(mediaItemId),
            mediaItemAspects
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return MarshallingHelper.DeserializeGuid((string)outParameters[0]);
    }

    public async Task<IList<MediaItem>> ReconcileMediaItemRelationshipsAsync(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects,
      IEnumerable<RelationshipItem> relationshipItems)
    {
      CpAction action = GetAction("X_MediaPortal_ReconcileMediaItemRelationships");
      IList<object> inParameters = new List<object>
        {
            MarshallingHelper.SerializeGuid(mediaItemId),
            mediaItemAspects,
            relationshipItems
        };
      IList<object> outParameters = await action.InvokeAsync(inParameters);
      return (IList<MediaItem>)outParameters[0];
    }

    public async Task DeleteMediaItemOrPathAsync(string systemId, ResourcePath path, bool inclusive)
    {
      CpAction action = GetAction("X_MediaPortal_DeleteMediaItemOrPath");
      IList<object> inParameters = new List<object> {systemId, path.Serialize(), inclusive};
      await action.InvokeAsync(inParameters);
    }

    public async Task ClientStartedShareImportAsync(Guid shareId)
    {
      CpAction action = GetAction("X_MediaPortal_ClientStartedShareImport");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      await action.InvokeAsync(inParameters);
    }

    public async Task ClientCompletedShareImportAsync(Guid shareId)
    {
      CpAction action = GetAction("X_MediaPortal_ClientCompletedShareImport");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(shareId)};
      await action.InvokeAsync(inParameters);
    }

    public async Task<ICollection<Guid>> GetCurrentlyImportingSharesAsync()
    {
      CpAction action = GetAction("X_MediaPortal_GetCurrentlyImportingShares");
      IList<object> outParameters = await action.InvokeAsync(null);
      return MarshallingHelper.ParseCsvGuidCollection((string) outParameters[0]);
    }

    public async Task NotifyPlaybackAsync(Guid mediaItemId, bool watched)
    {
      CpAction action = GetAction("X_MediaPortal_NotifyPlayback");
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(mediaItemId),
        watched
      };
      await action.InvokeAsync(inParameters);
    }

    public async Task NotifyUserPlaybackAsync(Guid userId, Guid mediaItemId, int percentage, bool updatePlayDate = true)
    {
      CpAction action = GetAction("X_MediaPortal_NotifyUserPlayback");
      IList<object> inParameters = new List<object>
      {
        MarshallingHelper.SerializeGuid(userId),
        MarshallingHelper.SerializeGuid(mediaItemId),
        percentage,
        updatePlayDate
      };
      await action.InvokeAsync(inParameters);
    }

    #endregion

    // TODO: State variables, if present
  }
}
