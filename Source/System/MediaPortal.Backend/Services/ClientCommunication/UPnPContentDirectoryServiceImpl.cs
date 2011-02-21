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
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.UPnP;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;
using RelocationMode=MediaPortal.Backend.MediaLibrary.RelocationMode;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  /// <summary>
  /// Provides the UPnP service for the MediaPortal 2 content directory.
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
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Bool);

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

      // Used for several parameters and result values
      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count", new DvStandardDataType(UPnPStandardDataType.Int))
          {
            SendEvents = false
          }; // Is int sufficient here?
      AddStateVariable(A_ARG_TYPE_Count);

      // Used to transport a resource path expression
      DvStateVariable A_ARG_TYPE_ResourcePath = new DvStateVariable("A_ARG_TYPE_ResourcePath", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ResourcePath);

      // Used to transport an enumeration of directories data
      DvStateVariable A_ARG_TYPE_ResourcePaths = new DvStateVariable("A_ARG_TYPE_ResourcePaths", new DvExtendedDataType(UPnPExtendedDataTypes.DtResourcePathMetadataEnumeration))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_ResourcePaths);

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

      // Used to transport a value indicating if query has to be done case sensitive or not.
      DvStateVariable A_ARG_TYPE_CapitalizationMode = new DvStateVariable("A_ARG_TYPE_CapitalizationMode", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        AllowedValueList = new List<string> { "CaseSensitive", "CaseInsensitive" }
      };
      AddStateVariable(A_ARG_TYPE_CapitalizationMode);

      // Used to transport a single media item with some media item aspects
      DvStateVariable A_ARG_TYPE_MediaItem = new DvStateVariable("A_ARG_TYPE_MediaItem", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItem))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItem);

      // Used to transport a collection of media items with some media item aspects
      DvStateVariable A_ARG_TYPE_MediaItems = new DvStateVariable("A_ARG_TYPE_MediaItems", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemEnumeration))
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
      DvStateVariable A_ARG_TYPE_MediaItemAspects = new DvStateVariable("A_ARG_TYPE_MediaItemAspects", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspectEnumeration))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemAspects);

      // Used to transport the text to be used in a simple text search
      DvStateVariable A_ARG_TYPE_SearchText = new DvStateVariable("A_ARG_TYPE_SearchText", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_SearchText);

      // Used to transport a value indicating if only online objects are referred or all.
      DvStateVariable A_ARG_TYPE_TextSearchMode = new DvStateVariable("A_ARG_TYPE_TextSearchMode", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"Normal", "ExcludeCLOBs"}
        };
      AddStateVariable(A_ARG_TYPE_TextSearchMode);

      // Used to transport an enumeration of value group instances
      DvStateVariable A_ARG_TYPE_MLQueryResultGroupEnumeration = new DvStateVariable("A_ARG_TYPE_MLQueryResultGroupEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtMLQueryResultGroupEnumeration))
          {
            SendEvents = false,
          };
      AddStateVariable(A_ARG_TYPE_MLQueryResultGroupEnumeration);

      DvStateVariable A_ARG_TYPE_GroupingFunction = new DvStateVariable("A_ARG_TYPE_GroupingFunction", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"FirstCharacter"}
        };
      AddStateVariable(A_ARG_TYPE_GroupingFunction);

      // Used to transport the data of a PlaylistContents instance
      DvStateVariable A_ARG_TYPE_PlaylistContents = new DvStateVariable("A_ARG_TYPE_PlaylistContents", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistContents))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistContents);

      // Used to transport the data of a PlaylistRawData instance
      DvStateVariable A_ARG_TYPE_PlaylistRawData = new DvStateVariable("A_ARG_TYPE_PlaylistRawData", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistRawData))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistRawData);

      // Used to transport an enumeration of playlist identification data (id, name) instances
      DvStateVariable A_ARG_TYPE_PlaylistIdentificationDataEnumeration = new DvStateVariable("A_ARG_TYPE_PlaylistIdentificationDataEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistInformationDataEnumeration))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistIdentificationDataEnumeration);

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

      DvAction reImportShareAction = new DvAction("ReImportShare", OnReImportShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(reImportShareAction);

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

      DvAction groupSearchAction = new DvAction("GroupSearch", OnGroupSearch,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
            new DvArgument("GroupingMIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("GroupingAttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("GroupingFunction", A_ARG_TYPE_GroupingFunction, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ResultGroups", A_ARG_TYPE_MLQueryResultGroupEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(groupSearchAction);

      DvAction textSearchAction = new DvAction("SimpleTextSearch", OnTextSearch,
          new DvArgument[] {
            new DvArgument("SearchText", A_ARG_TYPE_SearchText, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("SearchMode", A_ARG_TYPE_TextSearchMode, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("CapitalizationMode", A_ARG_TYPE_CapitalizationMode, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(textSearchAction);

      DvAction loadItemAction = new DvAction("LoadItem", OnLoadItem,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItem", A_ARG_TYPE_MediaItem, ArgumentDirection.Out, true),
          });
      AddAction(loadItemAction);

      DvAction browseAction = new DvAction("Browse", OnBrowse,
          new DvArgument[] {
            new DvArgument("ParentDirectory", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(browseAction);

      DvAction getValueGroupsAction = new DvAction("GetValueGroups", OnGetValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ValueGroups", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(getValueGroupsAction);

      DvAction groupValueGroupsAction = new DvAction("GroupValueGroups", OnGroupValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("GroupingFunction", A_ARG_TYPE_GroupingFunction, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ResultGroups", A_ARG_TYPE_MLQueryResultGroupEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(groupValueGroupsAction);

      // Playlist management

      DvAction getPlaylistsAction = new DvAction("GetPlaylists", OnGetPlaylists,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("Playlists", A_ARG_TYPE_PlaylistIdentificationDataEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(getPlaylistsAction);

      DvAction savePlaylistAction = new DvAction("SavePlaylist", OnSavePlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistRawData", A_ARG_TYPE_PlaylistRawData, ArgumentDirection.In)
          },
          new DvArgument[] {
          });
      AddAction(savePlaylistAction);

      DvAction deletePlaylistAction = new DvAction("DeletePlaylist", OnDeletePlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(deletePlaylistAction);

      DvAction exportPlaylistAction = new DvAction("ExportPlaylist", OnExportPlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("PlaylistRawData", A_ARG_TYPE_PlaylistRawData, ArgumentDirection.Out, true)
          });
      AddAction(exportPlaylistAction);

      DvAction loadServerPlaylistAction = new DvAction("LoadServerPlaylist", OnLoadServerPlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("PlaylistContents", A_ARG_TYPE_PlaylistContents, ArgumentDirection.Out, true)
          });
      AddAction(loadServerPlaylistAction);

      DvAction loadCustomPlaylistAction = new DvAction("LoadCustomPlaylist", OnLoadCustomPlaylist,
          new DvArgument[] {
            new DvArgument("MediaItemIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true)
          });
      AddAction(loadCustomPlaylistAction);

      // Media import

      DvAction addOrUpdateMediaItemAction = new DvAction("AddOrUpdateMediaItem", OnAddOrUpdateMediaItem,
          new DvArgument[] {
            new DvArgument("ParentDirectoryId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("UpdatedMediaItemAspects", A_ARG_TYPE_MediaItemAspects, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.Out, true),
          });
      AddAction(addOrUpdateMediaItemAction);

      DvAction deleteMediaItemOrPathAction = new DvAction("DeleteMediaItemOrPath", OnDeleteMediaItemOrPath,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("Inclusive", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(deleteMediaItemOrPathAction);

      // More actions go here
    }

    static UPnPError ParseOnlineState(string argumentName, string onlineStateStr, out bool all)
    {
      switch (onlineStateStr)
      {
        case "All":
          all = true;
          break;
        case "OnlyOnline":
          all = false;
          break;
        default:
          all = true;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'All' or 'OnlyOnline'", argumentName));
      }
      return null;
    }

    static UPnPError ParseSearchMode(string argumentName, string searchModeStr, out bool excludeCLOBs)
    {
      switch (searchModeStr)
      {
        case "Normal":
          excludeCLOBs = false;
          break;
        case "ExcludeCLOBs":
          excludeCLOBs = true;
          break;
        default:
          excludeCLOBs = true;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'Normal' or 'ExcludeCLOBs'", argumentName));
      }
      return null;
    }

    static UPnPError ParseCapitalizationMode(string argumentName, string searchModeStr, out bool caseSensitive)
    {
      switch (searchModeStr)
      {
        case "CaseSensitive":
          caseSensitive = true;
          break;
        case "CaseInsensitive":
          caseSensitive = false;
          break;
        default:
          caseSensitive = true;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'CaseSensitive' or 'CaseInsensitive'", argumentName));
      }
      return null;
    }

    static UPnPError ParseGroupingFunction(string argumentName, string groupingFunctionStr, out GroupingFunction groupingFunction)
    {
      switch (groupingFunctionStr)
      {
        case "FirstCharacter":
          groupingFunction = GroupingFunction.FirstCharacter;
          break;
        default:
          groupingFunction = GroupingFunction.FirstCharacter;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'FirstCharacter'", argumentName));
      }
      return null;
    }

    static UPnPError ParseRelocationMode(string argumentName, string relocateMediaItemsStr, out RelocationMode relocationMode)
    {
      switch (relocateMediaItemsStr)
      {
        case "None":
          relocationMode = RelocationMode.None;
          break;
        case "Relocate":
          relocationMode = RelocationMode.Relocate;
          break;
        case "ClearAndReImport":
          relocationMode = RelocationMode.Remove;
          break;
        default:
          relocationMode = RelocationMode.Remove;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'Relocate' or 'ClearAndReImport'", argumentName));
      }
      return null;
    }

    static UPnPError OnRegisterShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Share share = (Share) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().RegisterShare(share);
      outParams = null;
      return null;
    }

    static UPnPError OnRemoveShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().RemoveShare(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnUpdateShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ResourcePath baseResourcePath = ResourcePath.Deserialize((string) inParams[1]);
      string shareName = (string) inParams[2];
      string[] mediaCategories = ((string) inParams[3]).Split(',');
      string relocateMediaItemsStr = (string) inParams[4];
      RelocationMode relocationMode;
      UPnPError error = ParseRelocationMode("RelocateMediaItems", relocateMediaItemsStr, out relocationMode);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
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
      UPnPError error = ParseOnlineState("SharesFilter", sharesFilterStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IDictionary<Guid, Share> shares = ServiceRegistration.Get<IMediaLibrary>().GetShares(systemId);
      ICollection<Share> result;
      if (all)
        result = shares.Values;
      else
      {
        ICollection<string> connectedClientsIds = new List<string>();
        foreach (ClientConnection connection in ServiceRegistration.Get<IClientManager>().ConnectedClients)
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
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Share result = ServiceRegistration.Get<IMediaLibrary>().GetShare(shareId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnReImportShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Share share = ServiceRegistration.Get<IMediaLibrary>().GetShare(shareId);
      ServiceRegistration.Get<IImporterWorker>().ScheduleRefresh(share.BaseResourcePath, share.MediaCategories, true);
      outParams = null;
      return null;
    }

    static UPnPError OnAddMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().AddMediaItemAspectStorage(miam);
      outParams = null;
      return null;
    }

    static UPnPError OnRemoveMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().RemoveMediaItemAspectStorage(aspectId);
      outParams = null;
      return null;
    }

    static UPnPError OnGetAllManagedMediaItemAspectTypes(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<Guid> result = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata().Keys;
      outParams = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(result)};
      return null;
    }

    static UPnPError OnGetMediaItemAspectMetadata(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      MediaItemAspectMetadata miam = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata(aspectId);
      outParams = new List<object> {miam};
      return null;
    }

    static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemQuery query = (MediaItemQuery) inParams[0];
      string onlineStateStr = (string) inParams[1];
      bool all;
      UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IList<MediaItem> mediaItems = ServiceRegistration.Get<IMediaLibrary>().Search(query, !all);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnGroupSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemQuery query = (MediaItemQuery) inParams[0];
      Guid groupingMIAType = MarshallingHelper.DeserializeGuid((string) inParams[1]);
      string groupingAttributeName = (string) inParams[2];
      string onlineStateStr = (string) inParams[3];
      string groupingFunctionStr = (string) inParams[4];
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata groupingMIAM;
      outParams = null;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(groupingMIAType, out groupingMIAM))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", groupingMIAType));
      MediaItemAspectMetadata.AttributeSpecification groupingAttributeType;
      if (!groupingMIAM.AttributeSpecifications.TryGetValue(groupingAttributeName, out groupingAttributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            groupingMIAType, groupingAttributeName));
      bool all;
      GroupingFunction groupingFunction = GroupingFunction.FirstCharacter;
      UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all) ??
          ParseGroupingFunction("GroupingFunction", groupingFunctionStr, out groupingFunction);
      if (error != null)
        return error;
      IList<MLQueryResultGroup> resultGroups = ServiceRegistration.Get<IMediaLibrary>().GroupSearch(query, groupingAttributeType,
          !all, groupingFunction);
      outParams = new List<object> {resultGroups};
      return null;
    }

    static UPnPError OnTextSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string searchText = (string) inParams[0];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IFilter filter = (IFilter) inParams[3];
      string searchModeStr = (string) inParams[4];
      string onlineStateStr = (string) inParams[5];
      string capitalizationMode = (string) inParams[6];
      bool excludeCLOBs;
      bool all = false;
      bool caseSensitive = true;
      UPnPError error = ParseSearchMode("SearchMode", searchModeStr, out excludeCLOBs) ?? 
        ParseOnlineState("OnlineState", onlineStateStr, out all) ??
        ParseCapitalizationMode("CapitalizationMode", capitalizationMode, out caseSensitive);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      MediaItemQuery query = mediaLibrary.BuildSimpleTextSearchQuery(searchText, necessaryMIATypes, optionalMIATypes,
          filter, !excludeCLOBs, caseSensitive);
      IList<MediaItem> mediaItems = mediaLibrary.Search(query, !all);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnLoadItem(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[3]);
      MediaItem mediaItem = ServiceRegistration.Get<IMediaLibrary>().LoadItem(systemId, path,
          necessaryMIATypes, optionalMIATypes);
      outParams = new List<object> {mediaItem};
      return null;
    }

    static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid parentDirectoryId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      ICollection<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().Browse(parentDirectoryId, necessaryMIATypes, optionalMIATypes);

      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnGetValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IFilter filter = (IFilter) inParams[3];
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miam;
      outParams = null;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", aspectId));
      MediaItemAspectMetadata.AttributeSpecification attributeType;
      if (!miam.AttributeSpecifications.TryGetValue(attributeName, out attributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            aspectId, attributeName));
      HomogenousMap values = ServiceRegistration.Get<IMediaLibrary>().GetValueGroups(attributeType,
          necessaryMIATypes, filter);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnGroupValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IFilter filter = (IFilter) inParams[3];
      string groupingFunctionStr = (string) inParams[4];
      outParams = null;
      GroupingFunction groupingFunction;
      UPnPError error = ParseGroupingFunction("GroupingFunction", groupingFunctionStr, out groupingFunction);
      if (error != null)
        return error;
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miam;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", aspectId));
      MediaItemAspectMetadata.AttributeSpecification attributeType;
      if (!miam.AttributeSpecifications.TryGetValue(attributeName, out attributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            aspectId, attributeName));
      IList<MLQueryResultGroup> values = ServiceRegistration.Get<IMediaLibrary>().GroupValueGroups(attributeType,
          necessaryMIATypes, filter, groupingFunction);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnGetPlaylists(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<PlaylistInformationData> result = ServiceRegistration.Get<IMediaLibrary>().GetPlaylists();
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnSavePlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      PlaylistRawData playlistData = (PlaylistRawData) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().SavePlaylist(playlistData);
      outParams = null;
      return null;
    }

    static UPnPError OnDeletePlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      bool result = ServiceRegistration.Get<IMediaLibrary>().DeletePlaylist(playlistId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnExportPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      PlaylistRawData result = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(playlistId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnLoadServerPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      PlaylistContents result = ServiceRegistration.Get<IMediaLibrary>().LoadServerPlaylist(
          playlistId, necessaryMIATypes, optionalMIATypes);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnLoadCustomPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IList<Guid> mediaItemIds = MarshallingHelper.ParseCsvGuidCollection((string) inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IList<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().LoadCustomPlaylist(
          mediaItemIds, necessaryMIATypes, optionalMIATypes);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnAddOrUpdateMediaItem(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid parentDirectoryId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string systemId = (string) inParams[1];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[2]);
      IEnumerable<MediaItemAspect> mediaItemAspects = (IEnumerable<MediaItemAspect>) inParams[3];
      Guid mediaItemId = ServiceRegistration.Get<IMediaLibrary>().AddOrUpdateMediaItem(parentDirectoryId, systemId, path, mediaItemAspects);
      outParams = new List<object> {MarshallingHelper.SerializeGuid(mediaItemId)};
      return null;
    }

    static UPnPError OnDeleteMediaItemOrPath(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      bool inclusive = (bool) inParams[2];
      ServiceRegistration.Get<IMediaLibrary>().DeleteMediaItemOrPath(systemId, path, inclusive);
      outParams = null;
      return null;
    }
  }
}
