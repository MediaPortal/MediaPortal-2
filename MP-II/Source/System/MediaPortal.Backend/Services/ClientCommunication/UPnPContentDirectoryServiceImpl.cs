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
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.UPnP;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Utilities;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Provides the UPnP service for the MediaPortal-II content directory.
  /// </summary>
  /// <remarks>
  /// This service works similar to the ContentDirectory service of the UPnP standard MediaServer device, but it uses a bit
  /// different data structure for media items, so it isn't compatible with the standard ContentDirectory service. It also
  /// provides special actions to manage shares and media item aspect metadata schemas.
  /// </remarks>
  public class UPnPContentDirectoryServiceImpl : DvService
  {
    public UPnPContentDirectoryServiceImpl() : base(
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE, UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_ID)
    {
      // Used for several parameters and result values
      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count", new DvStandardDataType(UPnPStandardDataType.Int))
          {
            SendEvents = false
          }; // Is int sufficient here?
      AddStateVariable(A_ARG_TYPE_Count);

      // Used for any single GUID value
      DvStateVariable A_ARG_TYPE_Uuid = new DvStateVariable("A_ARG_TYPE_Id", new DvStandardDataType(UPnPStandardDataType.Uuid))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Uuid);

      // CSV of GUID strings
      DvStateVariable A_ARG_TYPE_UuidEnumeration = new DvStateVariable("A_ARG_TYPE_UuidEnumeration", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_UuidEnumeration);

      // Used for a system ID string
      DvStateVariable A_ARG_TYPE_SystemId = new DvStateVariable("A_ARG_TYPE_SystemId", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemId);

      // Used to transport a resource path expression
      DvStateVariable A_ARG_TYPE_ResourcePath = new DvStateVariable("A_ARG_TYPE_ResourcePath", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ResourcePath);

      // Used to hold names for several objects
      DvStateVariable A_ARG_TYPE_Name = new DvStateVariable("A_ARG_TYPE_Name", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Name);

      // CSV of media category strings
      DvStateVariable A_ARG_TYPE_MediaCategoryEnumeration = new DvStateVariable("A_ARG_TYPE_MediaCategoryEnumeration", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_MediaCategoryEnumeration);

      // Used to transport the data of a share structure
      DvStateVariable A_ARG_TYPE_Share = new DvStateVariable("A_ARG_TYPE_Share", new DvExtendedDataType(UPnPExtendedDataTypes.DtShare))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Share);

      // Used to transport an enumeration of shares data
      DvStateVariable A_ARG_TYPE_ShareEnumeration = new DvStateVariable("A_ARG_TYPE_ShareEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtShareEnumeration))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ShareEnumeration);

      // Used to transport a media item aspect metadata structure
      DvStateVariable A_ARG_TYPE_MediaItemAspectMetadata = new DvStateVariable("A_ARG_TYPE_MediaItemAspectMetadata", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspectMetadata))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_MediaItemAspectMetadata);

      // Used to give a mode of relocating media items after a share edit.
      DvStateVariable A_ARG_TYPE_MediaItemRelocationMode = new DvStateVariable("A_ARG_TYPE_MediaItemRelocationMode", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false,
            AllowedValueList = new List<string> {"Relocate", "ClearAndReImport"}
          };
      AddStateVariable(A_ARG_TYPE_MediaItemRelocationMode);

      // Used to transport an argument of type MediaItemQuery
      DvStateVariable A_ARG_TYPE_MediaItemQuery = new DvStateVariable("A_ARG_TYPE_MediaItemQuery", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemQuery))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemQuery);

      // Used to transport a value indicating if only online objects are referred or all.
      DvStateVariable A_ARG_TYPE_OnlineState = new DvStateVariable("A_ARG_TYPE_OnlineState", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"All", "OnlyOnline"}
        };
      AddStateVariable(A_ARG_TYPE_OnlineState);

      // Used to transport a collection of media items with some media item aspects
      DvStateVariable A_ARG_TYPE_MediaItems = new DvStateVariable("A_ARG_TYPE_MediaItems", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItems))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItems);

      // Used to transport a single media item filter
      DvStateVariable A_ARG_TYPE_MediaItemFilter = new DvStateVariable("A_ARG_TYPE_MediaItemFilter", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemsFilter))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemFilter);

      // Used to transport a collection of media item attribute values
      DvStateVariable A_ARG_TYPE_MediaItemAttributeValues = new DvStateVariable("A_ARG_TYPE_MediaItemAttributeValues", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAttributeValues))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemAttributeValues);

      // Used to transport an enumeration of media item aspects for a media item specified elsewhere
      DvStateVariable A_ARG_TYPE_MediaItemAspects = new DvStateVariable("A_ARG_TYPE_MediaItemAspects", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspects))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemAspects);

      // More state variables go here

      // Shares management
      DvAction registerShareAction = new DvAction("RegisterShare", OnRegisterShare,
          new DvArgument[] {
            new DvArgument("Share", A_ARG_TYPE_Share, ArgumentDirection.In),
          },
          new DvArgument[] {
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
            new DvArgument("BaseResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("ShareName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.In),
            new DvArgument("RelocateMediaItems", A_ARG_TYPE_MediaItemRelocationMode, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("NumAffectedMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true)
          });
      AddAction(updateShareAction);

      DvAction getSharesAction = new DvAction("GetShares", OnGetShares,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("SharesFilter", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
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

      DvAction getAllManagedMediaItemAspectTypesAction = new DvAction("GetAllManagedMediaItemAspectTypes", OnGetAllManagedMediaItemAspectTypes,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("MIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(getAllManagedMediaItemAspectTypesAction);

      DvAction getMediaItemAspectMetadataAction = new DvAction("GetMediaItemAspectMetadata", OnGetMediaItemAspectMetadata,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.Out, true),
          });
      AddAction(getMediaItemAspectMetadataAction);

      // Media query

      DvAction searchAction = new DvAction("Search", OnSearch,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(searchAction);

      DvAction browseAction = new DvAction("Browse", OnBrowse,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(browseAction);

      DvAction getDistinctAssociatedValuesAction = new DvAction("GetDistinctAssociatedValues", OnGetDistinctAssociatedValues,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("DistinctValues", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(getDistinctAssociatedValuesAction);

      // Media import

      DvAction addOrUpdateMediaItemAction = new DvAction("AddOrUpdateMediaItem", OnAddOrUpdateMediaItem,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("UpdatedMediaItemAspects", A_ARG_TYPE_MediaItemAspects, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(addOrUpdateMediaItemAction);

      DvAction deleteMediaItemOrPathAction = new DvAction("DeleteMediaItemOrPath", OnDeleteMediaItemOrPath,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(deleteMediaItemOrPathAction);

      // More actions go here
    }

    static UPnPError ParseOnlineState(string onlineStateStr, out bool all)
    {
      switch (onlineStateStr)
      {
        case "All":
          all = true;
          break;
        case "OnlyConnected":
          all = false;
          break;
        default:
          all = true;
          return new UPnPError(600, "Argument 'OnlineState' must be of value 'All' or 'OnlyConnected'");
      }
      return null;
    }

    static UPnPError OnRegisterShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Share share = (Share) inParams[0];
      ServiceScope.Get<IMediaLibrary>().RegisterShare(share);
      outParams = null;
      return null;
    }

    static UPnPError OnRemoveShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = new Guid((string) inParams[0]);
      ServiceScope.Get<IMediaLibrary>().RemoveShare(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnUpdateShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = new Guid((string) inParams[0]);
      ResourcePath baseResourcePath = ResourcePath.Deserialize((string) inParams[1]);
      string shareName = (string) inParams[2];
      string[] mediaCategories = ((string) inParams[3]).Split(',');
      string relocateMediaItems = (string) inParams[4];
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
      IMediaLibrary mediaLibrary = ServiceScope.Get<IMediaLibrary>();
      int numAffected = mediaLibrary.UpdateShare(shareId, baseResourcePath, shareName, mediaCategories, relocationMode);
      outParams = new List<object> {numAffected};
      return null;
    }

    static UPnPError OnGetShares(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      string sharesFilterStr = (string) inParams[1];
      bool all;
      UPnPError error = ParseOnlineState(sharesFilterStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IDictionary<Guid, Share> shares = ServiceScope.Get<IMediaLibrary>().GetShares(systemId);
      ICollection<Share> result;
      if (all)
        result = shares.Values;
      else
      {
        ICollection<string> connectedClientsIds = new List<string>();
        foreach (ClientConnection connection in ServiceScope.Get<IClientManager>().ConnectedClients)
          connectedClientsIds.Add(connection.Descriptor.MPFrontendServerUUID);
        result = new List<Share>();
        foreach (Share share in shares.Values)
          if (connectedClientsIds.Contains(share.SystemId))
            result.Add(share);
      }
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnGetShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = new Guid((string) inParams[0]);
      Share result = ServiceScope.Get<IMediaLibrary>().GetShare(shareId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnAddMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) inParams[0];
      ServiceScope.Get<IMediaLibrary>().AddMediaItemAspectStorage(miam);
      outParams = null;
      return null;
    }

    static UPnPError OnRemoveMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      ServiceScope.Get<IMediaLibrary>().RemoveMediaItemAspectStorage(aspectId);
      outParams = null;
      return null;
    }

    static UPnPError OnGetAllManagedMediaItemAspectTypes(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<Guid> result = ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata().Keys;
      string miaTypeIDs = StringUtils.Join(",", result);
      outParams = new List<object> {miaTypeIDs};
      return null;
    }

    static UPnPError OnGetMediaItemAspectMetadata(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      MediaItemAspectMetadata miam = ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata(aspectId);
      outParams = new List<object> {miam};
      return null;
    }

    static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemQuery query = (MediaItemQuery) inParams[0];
      string onlineStateStr = (string) inParams[1];
      bool all;
      UPnPError error = ParseOnlineState(onlineStateStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IList<MediaItem> mediaItems = ServiceScope.Get<IMediaLibrary>().Search(query, !all);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[3]);
      string onlineStateStr = (string) inParams[4];
      bool all;
      UPnPError error = ParseOnlineState(onlineStateStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      ICollection<MediaItem> mediaItems = ServiceScope.Get<IMediaLibrary>().Browse(systemId, path,
          necessaryMIATypes, optionalMIATypes, !all);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnGetDistinctAssociatedValues(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IFilter filter = (IFilter) inParams[3];
      IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miam;
      outParams = null;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", aspectId));
      MediaItemAspectMetadata.AttributeSpecification attributeType;
      if (!miam.AttributeSpecifications.TryGetValue(attributeName, out attributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            aspectId, attributeName));
      HomogenousCollection values = ServiceScope.Get<IMediaLibrary>().GetDistinctAssociatedValues(attributeType,
          necessaryMIATypes, filter);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnAddOrUpdateMediaItem(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      IEnumerable<MediaItemAspect> mediaItemAspects = (IEnumerable<MediaItemAspect>) inParams[2];
      ServiceScope.Get<IMediaLibrary>().AddOrUpdateMediaItem(systemId, path, mediaItemAspects);
      outParams = null;
      return null;
    }

    static UPnPError OnDeleteMediaItemOrPath(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      ServiceScope.Get<IMediaLibrary>().DeleteMediaItemOrPath(systemId, path);
      outParams = null;
      return null;
    }
  }
}
