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
using MediaPortal.Core.UPnP;
using MediaPortal.MediaLibrary;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Backend.Services.UPnP
{
  /// <summary>
  /// Encapsulates the UPnP service for the MediaPortal-II content directory.
  /// </summary>
  /// <remarks>
  /// This service works similar to the ContentDirectory service of the UPnP standard MediaServer device, but it uses a bit
  /// different data structure for media items, so it isn't compatible with the standard ContentDirectory service. It also
  /// provides special actions to manage shares and media item aspect metadata schemas.
  /// </remarks>
  public class UPnPContentDirectoryService : DvService
  {
    public const string SERVICE_TYPE = "schemas-team-mediaportal-com:service:ContentDirectory";
    public const int SERVICE_TYPE_VERSION = 1;
    public const string SERVICE_ID = "urn:team-mediaportal-com:serviceId:ContentDirectory";

    public UPnPContentDirectoryService() : base(SERVICE_TYPE, SERVICE_TYPE_VERSION, SERVICE_ID)
    {
      // Used for several parameters and result values
      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count", new DvStandardDataType(UPnPStandardDataType.Int)); // Is int sufficient here?
      AddStateVariable(A_ARG_TYPE_Count);

      // Used for any single GUID value
      DvStateVariable A_ARG_TYPE_Uuid = new DvStateVariable("A_ARG_TYPE_Id", new DvStandardDataType(UPnPStandardDataType.Uuid));
      AddStateVariable(A_ARG_TYPE_Uuid);

      // CSV of GUID strings
      DvStateVariable A_ARG_TYPE_UuidEnumeration = new DvStateVariable("A_ARG_TYPE_UuidEnumeration", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_UuidEnumeration);

      // Used to transport a provider path expression
      DvStateVariable A_ARG_TYPE_ProviderPath = new DvStateVariable("A_ARG_TYPE_ProviderPath", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_ProviderPath);

      // Used to hold names for several objects
      DvStateVariable A_ARG_TYPE_Name = new DvStateVariable("A_ARG_TYPE_Name", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_Name);

      // CSV of media category strings
      DvStateVariable A_ARG_TYPE_MediaCategoryEnumeration = new DvStateVariable("A_ARG_TYPE_MediaCategoryEnumeration", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_MediaCategoryEnumeration);

      // Used to transport the data of a share structure
      DvStateVariable A_ARG_TYPE_Share = new DvStateVariable("A_ARG_TYPE_Share", new DvExtendedDataType(UPnPExtendedDataTypes.DtShare));
      AddStateVariable(A_ARG_TYPE_Share);

      // Used to transport an enumeration of shares data
      DvStateVariable A_ARG_TYPE_ShareEnumeration = new DvStateVariable("A_ARG_TYPE_ShareEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtShareEnumeration));
      AddStateVariable(A_ARG_TYPE_ShareEnumeration);

      // Used to transport a system name
      DvStateVariable A_ARG_TYPE_SystemName = new DvStateVariable("A_ARG_TYPE_SystemName", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_SystemName);

      // Used to transport a media item aspect metadata structure
      DvStateVariable A_ARG_TYPE_MediaItemAspectMetadata = new DvStateVariable("A_ARG_TYPE_MediaItemAspectMetadata", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspectMetadata));
      AddStateVariable(A_ARG_TYPE_MediaItemAspectMetadata);

      // Used to give a mode of relocating media items after a share edit. RelocationMode can be "Relocate" or "ClearAndReImport"
      DvStateVariable A_ARG_TYPE_MediaItemRelocationMode = new DvStateVariable("A_ARG_TYPE_MediaItemRelocationMode", new DvStandardDataType(UPnPStandardDataType.String));
      AddStateVariable(A_ARG_TYPE_MediaItemRelocationMode);

      // Used to filter requested shares. SharesFilter can be "All" or "ConnectedShares"
      DvStateVariable A_ARG_TYPE_SharesFilter = new DvStateVariable("A_ARG_TYPE_SharesFilter", new DvStandardDataType(UPnPStandardDataType.Boolean));
      AddStateVariable(A_ARG_TYPE_SharesFilter);
      
      // More state variables go here

      // Shares management
      DvAction registerShareAction = new DvAction("RegisterShare", OnRegisterShare,
          new DvArgument[] {
            new DvArgument("NativeSystem", A_ARG_TYPE_SystemName, ArgumentDirection.In),
            new DvArgument("ProviderId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ProviderPath, ArgumentDirection.In),
            new DvArgument("ShareName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.In),
            new DvArgument("MetadataExtractorIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.Out, true)
          });
      AddAction(registerShareAction);

      DvAction removeShareAction = new DvAction("RemoveShare", OnRemoveShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In)
          },
          new DvArgument[] {
          });
      AddAction(removeShareAction);

      DvAction updateShareAction = new DvAction("UpdateShare", OnUpdateShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("ProviderId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ProviderPath, ArgumentDirection.In),
            new DvArgument("ShareName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.In),
            new DvArgument("MetadataExtractorIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("RelocateMediaItems", A_ARG_TYPE_MediaItemRelocationMode, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("NumRelocatedMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true)
          });
      AddAction(updateShareAction);

      DvAction getSharesAction = new DvAction("GetShares", OnGetShares,
          new DvArgument[] {
            new DvArgument("SharesFilter", A_ARG_TYPE_SharesFilter, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Shares", A_ARG_TYPE_ShareEnumeration, ArgumentDirection.Out, true)
          });
      AddAction(getSharesAction);

      DvAction getShareAction = new DvAction("GetShare", OnGetShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Share", A_ARG_TYPE_Share, ArgumentDirection.Out, true)
          });
      AddAction(getShareAction);

      // Client management
      DvAction connectClientAction = new DvAction("ConnectClient", OnConnectClient,
          new DvArgument[] {
            new DvArgument("SystemName", A_ARG_TYPE_SystemName, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(connectClientAction);

      DvAction disconnectClientAction = new DvAction("DisconnectClient", OnDisconnectClient,
          new DvArgument[] {
            new DvArgument("SystemName", A_ARG_TYPE_SystemName, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(disconnectClientAction);

      // Media item aspect storage management
      DvAction addMediaItemAspectStorageAction = new DvAction("AddMediaItemAspectStorage", OnAddMediaItemAspectStorage,
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(addMediaItemAspectStorageAction);

      DvAction removeMediaItemAspectStorageAction = new DvAction("RemoveMediaItemAspectStorage", OnRemoveMediaItemAspectStorage,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(removeMediaItemAspectStorageAction);

      DvAction getAllManagedMediaItemAspectMetadataIdsAction = new DvAction("GetAllManagedMediaItemAspectMetadataIds", OnGetAllManagedMediaItemAspectMetadataIds,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("MIAMIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(getAllManagedMediaItemAspectMetadataIdsAction);

      DvAction getMediaItemAspectMetadataAction = new DvAction("GetMediaItemAspectMetadata", OnGetMediaItemAspectMetadata,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.Out, true),
          });
      AddAction(getMediaItemAspectMetadataAction);

      // TODO: Add more actions
    }

    static UPnPError OnRegisterShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName nativeSystem = new SystemName((string) inParams[0]);
      Guid providerId = new Guid((string) inParams[1]);
      string path = (string) inParams[2];
      string shareName = (string) inParams[3];
      string[] mediaCategories = ((string) inParams[4]).Split(',');
      string[] metadataExtractorIdStrings = ((string) inParams[5]).Split(',');
      ICollection<Guid> metadataExtractorIds = new List<Guid>();
      foreach (string extractorIdString in metadataExtractorIdStrings)
        metadataExtractorIds.Add(new Guid(extractorIdString));
      Guid shareId = ServiceScope.Get<IMediaLibrary>().RegisterShare(
          nativeSystem, providerId, path, shareName, mediaCategories, metadataExtractorIds);
      outParams = new List<object> {shareId.ToString("B")};
      return null;
    }

    static UPnPError OnRemoveShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid shareId = new Guid((string) inParams[0]);
      ServiceScope.Get<IMediaLibrary>().RemoveShare(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnUpdateShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid shareId = new Guid((string) inParams[0]);
      Guid providerId = new Guid((string) inParams[1]);
      string path = (string) inParams[2];
      string shareName = (string) inParams[3];
      string[] mediaCategories = ((string) inParams[4]).Split(',');
      string[] metadataExtractorIdStrings = ((string) inParams[5]).Split(',');
      ICollection<Guid> metadataExtractorIds = new List<Guid>();
      string relocateMediaItems = (string) inParams[6];
      RelocationMode relocationMode;
      switch (relocateMediaItems)
      {
        case "Relocate":
          relocationMode = RelocationMode.Relocate;
          break;
        case "ClearAndReImport":
          relocationMode = RelocationMode.Remove;
          break;
        default:
          outParams = null;
          return new UPnPError(600, "Argument 'RelocateMediaItems' must be of value 'Relocate' or 'ClearAndReImport'");
      }
      foreach (string extractorIdString in metadataExtractorIdStrings)
        metadataExtractorIds.Add(new Guid(extractorIdString));
      IMediaLibrary mediaLibrary = ServiceScope.Get<IMediaLibrary>();
      Share oldShare = mediaLibrary.GetShare(shareId);
      int numRelocated = mediaLibrary.UpdateShare(
          shareId, oldShare.NativeSystem, providerId, path, shareName, mediaCategories, metadataExtractorIds, relocationMode);
      // TODO
      //if (relocationMode == RelocationMode.Remove)
      //  ... schedule reimport ...
      outParams = new List<object> {numRelocated};
      return null;
    }

    static UPnPError OnGetShares(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      string sharesFilterStr = (string) inParams[0];
      bool onlyConnected;
      switch (sharesFilterStr)
      {
        case "All":
          onlyConnected = false;
          break;
        case "ConnectedShares":
          onlyConnected = true;
          break;
        default:
          outParams = null;
          return new UPnPError(600, "Argument 'SharesFilter' must be of value 'All' or 'ConnectedShares'");
      }
      IDictionary<Guid, Share> shares = ServiceScope.Get<IMediaLibrary>().GetShares(onlyConnected);
      outParams = new List<object>() {shares.Values};
      return null;
    }

    static UPnPError OnGetShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid shareId = new Guid((string) inParams[0]);
      Share result = ServiceScope.Get<IMediaLibrary>().GetShare(shareId);
      outParams = new List<object>() {result};
      return null;
    }

    static UPnPError OnConnectClient(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName systemName = new SystemName((string) inParams[0]);
      // TODO: call ClientManager.ClientConnected(systemName)
      outParams = null;
      return null;
    }

    static UPnPError OnDisconnectClient(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName systemName = new SystemName((string) inParams[0]);
      // TODO: call ClientManager.ClientDisconnected(systemName)
      outParams = null;
      return null;
    }

    static UPnPError OnAddMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) inParams[0];
      ServiceScope.Get<IMediaLibrary>().AddMediaItemAspectStorage(miam);
      outParams = null;
      return null;
    }

    static UPnPError OnRemoveMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      ServiceScope.Get<IMediaLibrary>().RemoveMediaItemAspectStorage(aspectId);
      outParams = null;
      return null;
    }

    static UPnPError OnGetAllManagedMediaItemAspectMetadataIds(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      ICollection<Guid> result = new List<Guid>();
      foreach (MediaItemAspectMetadata miam in ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata())
        result.Add(miam.AspectId);
      outParams = new List<object>() {result};
      return null;
    }

    static UPnPError OnGetMediaItemAspectMetadata(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      MediaItemAspectMetadata miam = ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata(aspectId);
      outParams = new List<object>() {miam};
      return null;
    }
  }
}
