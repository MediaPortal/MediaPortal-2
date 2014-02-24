#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UPnP;
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
  /// This service implements the ContentDirectory service of the UPnP standard MediaServer device along with custom
  /// functions specific to MediaPortal. The major difference is in the data structure for media items and the
  /// addition of special actions to manage shares and media item aspect metadata schemas.
  /// </remarks>
  public class UPnPContentDirectoryServiceImpl : DvService
  {
    protected AsynchronousMessageQueue _messageQueue;

    protected DvStateVariable PlaylistsChangeCounter;
    protected DvStateVariable MIATypeRegistrationsChangeCounter;
    protected DvStateVariable CurrentlyImportingSharesChangeCounter;
    protected DvStateVariable RegisteredSharesChangeCounter;

    protected UInt32 _playlistsChangeCt = 0;
    protected UInt32 _miaTypeRegistrationsChangeCt = 0;
    protected UInt32 _currentlyImportingSharesChangeCt = 0;
    protected UInt32 _registeredSharesChangeCt = 0;

    public UPnPContentDirectoryServiceImpl() : base(
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE, UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_ID)
    {
      // DvStateVariables
      // Generic Types

      // Used for a boolean value
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Bool);

      // Used for any single GUID value
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Uuid = new DvStateVariable("A_ARG_TYPE_Id", new DvStandardDataType(UPnPStandardDataType.Uuid))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Uuid);

      // CSV of GUID strings
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_UuidEnumeration = new DvStateVariable("A_ARG_TYPE_UuidEnumeration", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_UuidEnumeration);

      // Used for a system ID string
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_SystemId = new DvStateVariable("A_ARG_TYPE_SystemId", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemId);

      // UPnP 1.0 State variables

      // (Optional) TransferIDs                 string (CSV ui4),             2.5.2
      //  Evented = true (Not Moderated)

      // Used for several parameters
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ObjectId = new DvStateVariable("A_ARG_TYPE_ObjectId", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_ObjectId);

      // Used for several return values
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Result = new DvStateVariable("A_ARG_TYPE_Result", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Result);

      // (Optional) A_ARG_TYPE_SearchCriteria   string,                       2.5.5
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_SearchCriteria = new DvStateVariable("A_ARG_TYPE_SearchCriteria", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_SearchCriteria);

      // Used for several parameters
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_BrowseFlag = new DvStateVariable("A_ARG_TYPE_BrowseFlag", new DvStandardDataType(UPnPStandardDataType.String))
      {
        AllowedValueList = new string[] { "BrowseMetadata", "BrowseDirectChildren" },
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_BrowseFlag);

      // Used for several parameters
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Filter = new DvStateVariable("A_ARG_TYPE_Filter", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Filter);

      // Used for several parameters
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_SortCriteria = new DvStateVariable("A_ARG_TYPE_SortCriteria", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_SortCriteria);

      // Used for several parameters
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Index = new DvStateVariable("A_ARG_TYPE_Index", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      }; // Is int sufficent here?
      AddStateVariable(A_ARG_TYPE_Index);

      // Used for several parameters and result values
      // Warning: UPnPStandardDataType.Int used before, changed to follow UPnP standard.
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      }; // Is int sufficient here?
      AddStateVariable(A_ARG_TYPE_Count);

      // Used to indicate a change has occured
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_UpdateID = new DvStateVariable("A_ARG_TYPE_UpdateID", new DvStandardDataType(UPnPStandardDataType.Ui4));
      AddStateVariable(A_ARG_TYPE_UpdateID);

      // (Optional) A_ARG_TYPE_TransferId,      ui4,                          2.5.12
      // (Optional) A_ARG_TYPE_TransferStatus   string,                       2.5.13
      // (Optional) A_ARG_TYPE_TransferLength   string,                       2.5.14
      // (Optional) A_ARG_TYPE_TransferTotal    string                        2.5.15
      // (Optional) A_ARG_TYPE_TagValueList     string (CSV string),          2.5.16

      // (Optional)
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable A_ARG_TYPE_URI = new DvStateVariable("A_ARG_TYPE_URI", new DvStandardDataType(UPnPStandardDataType.Uri))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_URI);

      // TODO: Define
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable SearchCapabilities = new DvStateVariable("SearchCapabilities", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(SearchCapabilities);

      // TODO: Define
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable SortCapabilities = new DvStateVariable("SortCapabilities", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(SortCapabilities);

      // TODO: Define
      // Evented, Moderated Event, Max Event Rate = 2
      // ReSharper disable once InconsistentNaming - Following UPnP 1.0 standards variable naming convention.
      DvStateVariable SystemUpdateID = new DvStateVariable("SystemUpdateID", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = true,
        ModeratedMaximumRate = TimeSpan.FromSeconds(2)
      };
      AddStateVariable(SystemUpdateID);

      // (Optional) ContainerUpdateIDs          string (CSV {string, ui4}),   2.5.21
      // Evented, Moderated Event, Max Event Rate = 2


      // MPnP 1.0 State variables

      // Used to transport a resource path expression
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ResourcePath = new DvStateVariable("A_ARG_TYPE_ResourcePath", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ResourcePath);

      // Used to transport an enumeration of directories data
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ResourcePaths = new DvStateVariable("A_ARG_TYPE_ResourcePaths", new DvExtendedDataType(UPnPExtendedDataTypes.DtResourcePathMetadataEnumeration))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_ResourcePaths);

      // Used to hold names for several objects
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Name = new DvStateVariable("A_ARG_TYPE_Name", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Name);

      // CSV of media category strings
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaCategoryEnumeration = new DvStateVariable("A_ARG_TYPE_MediaCategoryEnumeration", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_MediaCategoryEnumeration);

      // Used to transport the data of a share structure
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_Share = new DvStateVariable("A_ARG_TYPE_Share", new DvExtendedDataType(UPnPExtendedDataTypes.DtShare))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_Share);

      // Used to transport an enumeration of shares data
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ShareEnumeration = new DvStateVariable("A_ARG_TYPE_ShareEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtShareEnumeration))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_ShareEnumeration);

      // Used to transport a media item aspect metadata structure
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemAspectMetadata = new DvStateVariable("A_ARG_TYPE_MediaItemAspectMetadata", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspectMetadata))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_MediaItemAspectMetadata);

      // Used to give a mode of relocating media items after a share edit.
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemRelocationMode = new DvStateVariable("A_ARG_TYPE_MediaItemRelocationMode", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false,
            AllowedValueList = new List<string> {"Relocate", "ClearAndReImport"}
          };
      AddStateVariable(A_ARG_TYPE_MediaItemRelocationMode);

      // Used to transport an argument of type MediaItemQuery
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemQuery = new DvStateVariable("A_ARG_TYPE_MediaItemQuery", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemQuery))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemQuery);

      // Used to transport a value indicating if only online objects are referred or all.
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_OnlineState = new DvStateVariable("A_ARG_TYPE_OnlineState", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"All", "OnlyOnline"}
        };
      AddStateVariable(A_ARG_TYPE_OnlineState);

      // Used to transport a value indicating if query has to be done case sensitive or not.
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_CapitalizationMode = new DvStateVariable("A_ARG_TYPE_CapitalizationMode", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        AllowedValueList = new List<string> { "CaseSensitive", "CaseInsensitive" }
      };
      AddStateVariable(A_ARG_TYPE_CapitalizationMode);

      // Used to transport a single media item with some media item aspects
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItem = new DvStateVariable("A_ARG_TYPE_MediaItem", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItem))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItem);

      // Used to transport a collection of media items with some media item aspects
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItems = new DvStateVariable("A_ARG_TYPE_MediaItems", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemEnumeration))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItems);

      // Used to transport a single media item filter
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemFilter = new DvStateVariable("A_ARG_TYPE_MediaItemFilter", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemsFilter))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemFilter);

      // Used to transport a collection of media item attribute values
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemAttributeValues = new DvStateVariable("A_ARG_TYPE_MediaItemAttributeValues", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAttributeValues))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemAttributeValues);

      // Used to transport an enumeration of media item aspects for a media item specified elsewhere
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MediaItemAspects = new DvStateVariable("A_ARG_TYPE_MediaItemAspects", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemAspectEnumeration))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemAspects);

      // Used to transport the text to be used in a simple text search
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_SearchText = new DvStateVariable("A_ARG_TYPE_SearchText", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_SearchText);

      // Used to transport a value indicating if only online objects are referred or all.
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_TextSearchMode = new DvStateVariable("A_ARG_TYPE_TextSearchMode", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"Normal", "ExcludeCLOBs"}
        };
      AddStateVariable(A_ARG_TYPE_TextSearchMode);

      // Used to transport an enumeration of value group instances
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_MLQueryResultGroupEnumeration = new DvStateVariable("A_ARG_TYPE_MLQueryResultGroupEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtMLQueryResultGroupEnumeration))
          {
            SendEvents = false,
          };
      AddStateVariable(A_ARG_TYPE_MLQueryResultGroupEnumeration);

      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_ProjectionFunction = new DvStateVariable("A_ARG_TYPE_ProjectionFunction", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"None", "DateToYear"}
        };
      AddStateVariable(A_ARG_TYPE_ProjectionFunction);

      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_GroupingFunction = new DvStateVariable("A_ARG_TYPE_GroupingFunction", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"FirstCharacter"}
        };
      AddStateVariable(A_ARG_TYPE_GroupingFunction);

      // Used to transport the data of a PlaylistContents instance
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_PlaylistContents = new DvStateVariable("A_ARG_TYPE_PlaylistContents", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistContents))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistContents);

      // Used to transport the data of a PlaylistRawData instance
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_PlaylistRawData = new DvStateVariable("A_ARG_TYPE_PlaylistRawData", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistRawData))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistRawData);

      // Used to transport an enumeration of playlist identification data (id, name) instances
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_PlaylistIdentificationDataEnumeration = new DvStateVariable("A_ARG_TYPE_PlaylistIdentificationDataEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtPlaylistInformationDataEnumeration))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_PlaylistIdentificationDataEnumeration);

      // Used to transport an IDictionary<Guid, DateTime> such as the MediaItemAspectCreationDates
      DvStateVariable A_ARG_TYPE_DictionaryGuidDateTime = new DvStateVariable("A_ARG_TYPE_DictionaryGuidDateTime", new DvExtendedDataType(UPnPExtendedDataTypes.DtDictionaryGuidDateTime))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_DictionaryGuidDateTime);

      // Change event for playlists
      PlaylistsChangeCounter = new DvStateVariable("PlaylistsChangeCounter", new DvStandardDataType(UPnPStandardDataType.Ui4))
        {
            SendEvents = true,
            Value = (uint) 0
        };
      AddStateVariable(PlaylistsChangeCounter);

      // Change event for MIA type registrations
      MIATypeRegistrationsChangeCounter = new DvStateVariable("MIATypeRegistrationsChangeCounter", new DvStandardDataType(UPnPStandardDataType.Ui4))
        {
            SendEvents = true,
            Value = (uint) 0
        };
      AddStateVariable(MIATypeRegistrationsChangeCounter);

      // Change event for registered shares
      RegisteredSharesChangeCounter = new DvStateVariable("RegisteredSharesChangeCounter", new DvStandardDataType(UPnPStandardDataType.Ui4))
        {
            SendEvents = true,
            Value = (uint) 0
        };
      AddStateVariable(RegisteredSharesChangeCounter);

      // Change event for currently importing shares
      CurrentlyImportingSharesChangeCounter = new DvStateVariable("CurrentlyImportingSharesChangeCounter", new DvStandardDataType(UPnPStandardDataType.Ui4))
        {
            SendEvents = true,
            Value = (uint) 0
        };
      AddStateVariable(CurrentlyImportingSharesChangeCounter);

      // More state variables go here


      // Device Actions

      // UPnP 1.0 Device Actions

      // UPnP 1.0 - 2.7.1 GetSearchCapabilities - Required
      DvAction getSearchCapabiltiesAction = new DvAction("GetSearchCapabilities", OnGetSearchCapabilities,
        new DvArgument[] {
        },
          new DvArgument[] {
            new DvArgument("SearchCaps", SearchCapabilities, ArgumentDirection.Out, true), 
          });
      AddAction(getSearchCapabiltiesAction);

      // UPnP 1.0 - 2.7.2 GetSortCapabilities - Required
      DvAction getSortCapabilitiesAction = new DvAction("GetSortCapabilities", OnGetSortCapabilities,
        new DvArgument[] {
        },
        new DvArgument[] {
          new DvArgument("SortCapabilities", SortCapabilities, ArgumentDirection.Out, true)
        });
      AddAction(getSortCapabilitiesAction);

      // UPnP 1.0 - 2.7.3 GetSystemUpdateId - Required
      DvAction getSystemUpdateIDAction = new DvAction("GetSystemUpdateID", OnGetSystemUpdateID,
        new DvArgument[]
        {
          
        },
        new DvArgument[]
        {
          new DvArgument("Id", SystemUpdateID, ArgumentDirection.Out), 
        });
      AddAction(getSystemUpdateIDAction);

      // UPnP 1.0 - 2.7.4 Browse - Required
      DvAction browseAction = new DvAction("Browse", OnBrowse,
          new DvArgument[] {
            new DvArgument("ObjectId", A_ARG_TYPE_Uuid, ArgumentDirection.In),               // ParentDirectory
            new DvArgument("BrowseFlag", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),  // NecessaryMIATypes
            new DvArgument("Filter", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),      // OptionalMIATypes
            new DvArgument("StartingIndex", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("RequestedCount", A_ARG_TYPE_Count, ArgumentDirection.In), 
            new DvArgument("SortCriteria", A_ARG_TYPE_SortCriteria, ArgumentDirection.In), 
          },
          new DvArgument[] {
            new DvArgument("Result", A_ARG_TYPE_Result, ArgumentDirection.Out, true),
            new DvArgument("NumberReturned", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
            new DvArgument("TotalMatches", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
            new DvArgument("UpdateID", A_ARG_TYPE_UpdateID, ArgumentDirection.Out, true), 
          });
      AddAction(browseAction);

      // UPnP 1.0 - 2.7.5 Search - Optional
      DvAction searchAction = new DvAction("Search", OnSearch,
          new DvArgument[] {
            new DvArgument("ContainerID", A_ARG_TYPE_ObjectId, ArgumentDirection.In),
            new DvArgument("SearchCriteria", A_ARG_TYPE_SearchCriteria, ArgumentDirection.In),
            new DvArgument("StartingIndex", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("RequestedCount", A_ARG_TYPE_Count, ArgumentDirection.In), 
            new DvArgument("SortCriteria", A_ARG_TYPE_SortCriteria, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Result", A_ARG_TYPE_Result, ArgumentDirection.Out, true),
            new DvArgument("NumberReturned", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
            new DvArgument("TotalMatches", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
            new DvArgument("UpdateID", A_ARG_TYPE_UpdateID, ArgumentDirection.Out, true), 
          });
      AddAction(searchAction);

      // UPnP 1.0 - 2.7.6 CreateObject - Optional
      // UPnP 1.0 - 2.7.7 DestoryObject - Optional
      // UPnP 1.0 - 2.7.8 UpdateObject - Optional
      // UPnP 1.0 - 2.7.9 ImportResource - Optional
      // UPnP 1.0 - 2.7.10 ExportResource - Optional
      // UPnP 1.0 - 2.7.11 StopTransferResource - Optional
      // UPnP 1.0 - 2.7.12 GetTransferProgress - Optional
      // UPnP 1.0 - 2.7.13 DeleteResource - Optional
      // UPnP 1.0 - 2.7.14 CreateReference - Optional

      // UPnP 1.0 - 2.7.15 Non-Stanard Actions Implementations


      // MPnP 1.0 Actions

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

      DvAction setupDefaultServerSharesAction = new DvAction("SetupDefaultServerShares", OnSetupDefaultServerShares,
          new DvArgument[] {
          },
          new DvArgument[] {
          });
      AddAction(setupDefaultServerSharesAction);

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

      DvAction getAllManagedMediaItemAspectCreationDatesAction = new DvAction("GetAllManagedMediaItemAspectCreationDates", OnGetAllManagedMediaItemAspectCreationDates,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("MIACreationDates", A_ARG_TYPE_DictionaryGuidDateTime, ArgumentDirection.Out, true),
          });
      AddAction(getAllManagedMediaItemAspectCreationDatesAction);

      DvAction getMediaItemAspectMetadataAction = new DvAction("GetMediaItemAspectMetadata", OnGetMediaItemAspectMetadata,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.Out, true),
          });
      AddAction(getMediaItemAspectMetadataAction);

      // Media query

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

      DvAction searchAction = new DvAction("Search", OnSearch,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(searchAction);

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

      DvAction getValueGroupsAction = new DvAction("GetValueGroups", OnGetValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ValueGroups", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(getValueGroupsAction);

      DvAction groupValueGroupsAction = new DvAction("GroupValueGroups", OnGroupValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("GroupingFunction", A_ARG_TYPE_GroupingFunction, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ResultGroups", A_ARG_TYPE_MLQueryResultGroupEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(groupValueGroupsAction);

      DvAction countMediaItemsAction = new DvAction("CountMediaItems", OnCountMediaItems,
          new DvArgument[] {
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("NumMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
          });
      AddAction(countMediaItemsAction);

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

      DvAction clientStartedShareImportAction = new DvAction("ClientStartedShareImport", OnClientStartedShareImport,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(clientStartedShareImportAction);

      DvAction clientCompletedShareImportAction = new DvAction("ClientCompletedShareImport", OnClientCompletedShareImport,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(clientCompletedShareImportAction);

      DvAction getCurrentlyImportingSharesAction = new DvAction("GetCurrentlyImportingShares", OnGetCurrentlyImportingShares,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("ShareIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.Out, true), 
          });
      AddAction(getCurrentlyImportingSharesAction);

      // Media playback

      DvAction notifyPlaybackAction = new DvAction("NotifyPlayback", OnNotifyPlayback,
          new DvArgument[] {
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.In), 
          },
          new DvArgument[] {
          });
      AddAction(notifyPlaybackAction);

      
      // MPnP 1.1 Actions

      DvAction browseAction = new DvAction("Browse", OnBrowse,
          new DvArgument[] {
            new DvArgument("ParentDirectory", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In), 
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(browseAction);

      DvAction searchAction = new DvAction("Search", OnSearch,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In), 
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(searchAction);

      DvAction textSearchAction = new DvAction("SimpleTextSearch", OnTextSearch,
          new DvArgument[] {
            new DvArgument("SearchText", A_ARG_TYPE_SearchText, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("SearchMode", A_ARG_TYPE_TextSearchMode, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("CapitalizationMode", A_ARG_TYPE_CapitalizationMode, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In), 
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(textSearchAction);

      DvAction loadCustomPlaylistAction = new DvAction("LoadCustomPlaylist", OnLoadCustomPlaylist,
          new DvArgument[] {
            new DvArgument("MediaItemIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In), 
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true)
          });
      AddAction(loadCustomPlaylistAction);

      // More actions go here

      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ContentDirectoryMessaging.CHANNEL,
            ImporterWorkerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public override void Dispose()
    {
      base.Dispose();
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.PlaylistsChanged:
            PlaylistsChangeCounter.Value = ++_playlistsChangeCt;
            break;
          case ContentDirectoryMessaging.MessageType.MIATypesChanged:
            MIATypeRegistrationsChangeCounter.Value = ++_miaTypeRegistrationsChangeCt;
            break;
          case ContentDirectoryMessaging.MessageType.RegisteredSharesChanged:
            RegisteredSharesChangeCounter.Value = ++_registeredSharesChangeCt;
            break;
          case ContentDirectoryMessaging.MessageType.ShareImportStarted:
          case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
            CurrentlyImportingSharesChangeCounter.Value = ++_currentlyImportingSharesChangeCt;
            break;
        }
      }
      else if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            CurrentlyImportingSharesChangeCounter.Value = ++_currentlyImportingSharesChangeCt;
            break;
        }
      }
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

    static UPnPError ParseProjectionFunction(string argumentName, string projectionFunctionStr, out ProjectionFunction projectionFunction)
    {
      switch (projectionFunctionStr)
      {
        case "None":
          projectionFunction = ProjectionFunction.None;
          break;
        case "DateToYear":
          projectionFunction = ProjectionFunction.DateToYear;
          break;
        default:
          projectionFunction = ProjectionFunction.None;
          return new UPnPError(600, string.Format("Argument '{0}' must be of value 'DateToYear' or 'None'", argumentName));
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
        ICollection<string> connectedClientsIds = ServiceRegistration.Get<IClientManager>().ConnectedClients.Select(
            connection => connection.Descriptor.MPFrontendServerUUID).ToList();
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

    static UPnPError OnSetupDefaultServerShares(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ServiceRegistration.Get<IMediaLibrary>().SetupDefaultLocalShares();
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

    static UPnPError OnGetAllManagedMediaItemAspectCreationDates(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IDictionary<Guid, DateTime> result = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectCreationDates();
      outParams = new List<object> { result };
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

    static UPnPError OnGetValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IFilter selectAttributeFilter = (IFilter) inParams[2];
      string projectionFunctionStr = (string) inParams[3];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[4]);
      IFilter filter = (IFilter) inParams[5];
      string onlineStateStr = (string) inParams[6];
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miam;
      outParams = null;
      ProjectionFunction projectionFunction;
      bool all = true;
      UPnPError error = ParseProjectionFunction("ProjectionFunction", projectionFunctionStr, out projectionFunction) ??
          ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
        return error;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", aspectId));
      MediaItemAspectMetadata.AttributeSpecification attributeType;
      if (!miam.AttributeSpecifications.TryGetValue(attributeName, out attributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            aspectId, attributeName));
      HomogenousMap values = ServiceRegistration.Get<IMediaLibrary>().GetValueGroups(attributeType, selectAttributeFilter,
          projectionFunction, necessaryMIATypes, filter, !all);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnGroupValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IFilter selectAttributeFilter = (IFilter) inParams[2];
      string projectionFunctionStr = (string) inParams[3];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[4]);
      IFilter filter = (IFilter) inParams[5];
      string onlineStateStr = (string) inParams[6];
      string groupingFunctionStr = (string) inParams[7];
      outParams = null;
      ProjectionFunction projectionFunction;
      bool all = true;
      GroupingFunction groupingFunction = GroupingFunction.FirstCharacter;
      UPnPError error = ParseProjectionFunction("ProjectionFunction", projectionFunctionStr, out projectionFunction) ??
          ParseOnlineState("OnlineState", onlineStateStr, out all) ??
          ParseGroupingFunction("GroupingFunction", groupingFunctionStr, out groupingFunction);
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
          selectAttributeFilter, projectionFunction, necessaryMIATypes, filter, !all, groupingFunction);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnCountMediaItems(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[0]);
      IFilter filter = (IFilter) inParams[1];
      string onlineStateStr = (string) inParams[2];
      outParams = null;
      bool all;
      UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
        return error;
      int numMediaItems = ServiceRegistration.Get<IMediaLibrary>().CountMediaItems(necessaryMIATypes, filter, !all);
      outParams = new List<object> {numMediaItems};
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

    static UPnPError OnClientStartedShareImport(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().ClientStartedShareImport(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnClientCompletedShareImport(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().ClientCompletedShareImport(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnGetCurrentlyImportingShares(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      outParams = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(mediaLibrary.GetCurrentlyImportingShareIds())};
      return null;
    }

    static UPnPError OnNotifyPlayback(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      Guid mediaItemId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      mediaLibrary.NotifyPlayback(mediaItemId);
      outParams = null;
      return null;
    }
  }
}
