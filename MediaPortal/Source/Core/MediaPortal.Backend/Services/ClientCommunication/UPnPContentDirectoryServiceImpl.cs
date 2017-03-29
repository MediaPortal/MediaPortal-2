#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
    protected DvStateVariable RegisteredSharesChangeCounter;

    protected UInt32 _playlistsChangeCt = 0;
    protected UInt32 _miaTypeRegistrationsChangeCt = 0;
    protected UInt32 _registeredSharesChangeCt = 0;

    public UPnPContentDirectoryServiceImpl() : base(
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE, UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.CONTENT_DIRECTORY_SERVICE_ID)
    {
      #region Device State Variables
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

      // Used to hold names for several objects
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_UseShareWatcher = new DvStateVariable("A_ARG_TYPE_UseShareWatcher", new DvStandardDataType(UPnPStandardDataType.Boolean))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_UseShareWatcher);

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

      // Used to transport a collection of share import progresses
      // ReSharper disable once InconsistentNaming - Following UPnP standards variable naming convention.
      DvStateVariable A_ARG_TYPE_DictionaryGuidInt32 = new DvStateVariable("A_ARG_TYPE_DictionaryGuidInt32", new DvExtendedDataType(UPnPExtendedDataTypes.DtDictionaryGuidInt32))
      {
        SendEvents = false,
      };
      AddStateVariable(A_ARG_TYPE_DictionaryGuidInt32);

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

      // More state variables go here

      #endregion

      #region Device Actions

      #region UPnP 1.0 Device Actions

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

      #endregion

      // UPnP 1.0 - 2.7.15 Non-Stanard Actions Implementations

      #region MPnP 1.0 Device Actions

      // Shares management
      DvAction mpnp10RegisterShareAction = new DvAction("X_MediaPortal_RegisterShare", OnMPnP10RegisterShare,
          new DvArgument[] {
            new DvArgument("Share", A_ARG_TYPE_Share, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10RegisterShareAction);

      DvAction mpnp10RemoveShareAction = new DvAction("X_MediaPortal_RemoveShare", OnMPnP10RemoveShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In)
          },
          new DvArgument[] {
          });
      AddAction(mpnp10RemoveShareAction);

      DvAction mpnp10UpdateShareAction = new DvAction("X_MediaPortal_UpdateShare", OnMPnP10UpdateShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("BaseResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("ShareName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("UseShareWatcher", A_ARG_TYPE_UseShareWatcher, ArgumentDirection.In),
            new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.In),
            new DvArgument("RelocateMediaItems", A_ARG_TYPE_MediaItemRelocationMode, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("NumAffectedMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true)
          });
      AddAction(mpnp10UpdateShareAction);

      DvAction mpnp10GetSharesAction = new DvAction("X_MediaPortal_GetShares", OnMPnP10GetShares,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("SharesFilter", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Shares", A_ARG_TYPE_ShareEnumeration, ArgumentDirection.Out, true)
          });
      AddAction(mpnp10GetSharesAction);

      DvAction mpnp10GetShareAction = new DvAction("X_MediaPortal_GetShare", OnMPnP10GetShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Share", A_ARG_TYPE_Share, ArgumentDirection.Out, true)
          });
      AddAction(mpnp10GetShareAction);

      DvAction mpnp10ReImportShareAction = new DvAction("X_MediaPortal_ReImportShare", OnMPnP10ReImportShare,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10ReImportShareAction);

      DvAction mpnp10SetupDefaultServerSharesAction = new DvAction("X_MediaPortal_SetupDefaultServerShares", OnMPnP10SetupDefaultServerShares,
          new DvArgument[] {
          },
          new DvArgument[] {
          });
      AddAction(mpnp10SetupDefaultServerSharesAction);

      // Media item aspect storage management

      DvAction mpnp10AddMediaItemAspectStorageAction = new DvAction("X_MediaPortal_AddMediaItemAspectStorage", OnMPnP10AddMediaItemAspectStorage,
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10AddMediaItemAspectStorageAction);

      DvAction mpnp10RemoveMediaItemAspectStorageAction = new DvAction("X_MediaPortal_RemoveMediaItemAspectStorage", OnMPnP10RemoveMediaItemAspectStorage,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10RemoveMediaItemAspectStorageAction);

      DvAction mpnp10GetAllManagedMediaItemAspectTypesAction = new DvAction("X_MediaPortal_GetAllManagedMediaItemAspectTypes", OnMPnP10GetAllManagedMediaItemAspectTypes,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("MIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(mpnp10GetAllManagedMediaItemAspectTypesAction);

      DvAction mpnp10GetMediaItemAspectMetadataAction = new DvAction("X_MediaPortal_GetMediaItemAspectMetadata", OnMPnP10GetMediaItemAspectMetadata,
          new DvArgument[] {
            new DvArgument("MIAM_Id", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MIAM", A_ARG_TYPE_MediaItemAspectMetadata, ArgumentDirection.Out, true),
          });
      AddAction(mpnp10GetMediaItemAspectMetadataAction);

      DvAction mpnp10GetAllManagedMediaItemAspectCreationDatesAction = new DvAction("X_MediaPortal_GetAllManagedMediaItemAspectCreationDates", OnMPnP10GetAllManagedMediaItemAspectCreationDates,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("MIACreationDates", A_ARG_TYPE_DictionaryGuidDateTime, ArgumentDirection.Out, true),
          });
      AddAction(mpnp10GetAllManagedMediaItemAspectCreationDatesAction);

      // Media query

      // Superseded MPnP 1.1
      //DvAction mpnp10LoadItemAction = new DvAction("X_MediaPortal_LoadItem", OnMPnP10LoadItem,
      //    new DvArgument[] {
      //      new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
      //      new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("MediaItem", A_ARG_TYPE_MediaItem, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10LoadItemAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10BrowseAction = new DvAction("X_MediaPortal_Browse", OnMPnP10Browse,
      //    new DvArgument[] {
      //      new DvArgument("ParentDirectory", A_ARG_TYPE_Uuid, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10BrowseAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10SearchAction = new DvAction("X_MediaPortal_Search", OnMPnP10Search,
      //    new DvArgument[] {
      //      new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
      //      new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10SearchAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10TextSearchAction = new DvAction("X_MediaPortal_SimpleTextSearch", OnMPnP10TextSearch,
      //    new DvArgument[] {
      //      new DvArgument("SearchText", A_ARG_TYPE_SearchText, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("SearchMode", A_ARG_TYPE_TextSearchMode, ArgumentDirection.In),
      //      new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
      //      new DvArgument("CapitalizationMode", A_ARG_TYPE_CapitalizationMode, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10TextSearchAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10GetValueGroupsAction = new DvAction("X_MediaPortal_GetValueGroups", OnMPnP10GetValueGroups,
      //    new DvArgument[] {
      //      new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
      //      new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
      //      new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("ValueGroups", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10GetValueGroupsAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10GroupValueGroupsAction = new DvAction("X_MediaPortal_GroupValueGroups", OnMPnP10GroupValueGroups,
      //    new DvArgument[] {
      //      new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
      //      new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
      //      new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
      //      new DvArgument("GroupingFunction", A_ARG_TYPE_GroupingFunction, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("ResultGroups", A_ARG_TYPE_MLQueryResultGroupEnumeration, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10GroupValueGroupsAction);

      // Superseded MPnP 1.1
      //DvAction mpnp10CountMediaItemsAction = new DvAction("X_MediaPortal_CountMediaItems", OnMPnP10CountMediaItems,
      //    new DvArgument[] {
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
      //      new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("NumMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
      //    });
      //AddAction(mpnp10CountMediaItemsAction);

      // Playlist management

      DvAction mpnp10GetPlaylistsAction = new DvAction("X_MediaPortal_GetPlaylists", OnMPnP10GetPlaylists,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("Playlists", A_ARG_TYPE_PlaylistIdentificationDataEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(mpnp10GetPlaylistsAction);

      DvAction mpnp10SavePlaylistAction = new DvAction("X_MediaPortal_SavePlaylist", OnMPnP10SavePlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistRawData", A_ARG_TYPE_PlaylistRawData, ArgumentDirection.In)
          },
          new DvArgument[] {
          });
      AddAction(mpnp10SavePlaylistAction);

      DvAction mpnp10DeletePlaylistAction = new DvAction("X_MediaPortal_DeletePlaylist", OnMPnP10DeletePlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("Success", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
          });
      AddAction(mpnp10DeletePlaylistAction);

      DvAction mpnp10ExportPlaylistAction = new DvAction("X_MediaPortal_ExportPlaylist", OnMPnP10ExportPlaylist,
          new DvArgument[] {
            new DvArgument("PlaylistId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("PlaylistRawData", A_ARG_TYPE_PlaylistRawData, ArgumentDirection.Out, true)
          });
      AddAction(mpnp10ExportPlaylistAction);

      // Superseded
      //DvAction mpnp10LoadCustomPlaylistAction = new DvAction("X_MediaPortal_LoadCustomPlaylist", OnMPnP10LoadCustomPlaylist,
      //    new DvArgument[] {
      //      new DvArgument("MediaItemIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //      new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
      //    },
      //    new DvArgument[] {
      //      new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true)
      //    });
      //AddAction(mpnp10LoadCustomPlaylistAction);

      // Media import

      DvAction mpnp10AddOrUpdateMediaItemAction = new DvAction("X_MediaPortal_AddOrUpdateMediaItem", OnMPnP10AddOrUpdateMediaItem,
          new DvArgument[] {
            new DvArgument("ParentDirectoryId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("UpdatedMediaItemAspects", A_ARG_TYPE_MediaItemAspects, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.Out, true),
          });
      AddAction(mpnp10AddOrUpdateMediaItemAction);

      DvAction mpnp10DeleteMediaItemOrPathAction = new DvAction("X_MediaPortal_DeleteMediaItemOrPath", OnMPnP10DeleteMediaItemOrPath,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("Inclusive", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10DeleteMediaItemOrPathAction);

      DvAction mpnp10ClientStartedShareImportAction = new DvAction("X_MediaPortal_ClientStartedShareImport", OnMPnP10ClientStartedShareImport,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10ClientStartedShareImportAction);

      DvAction mpnp10ClientCompletedShareImportAction = new DvAction("X_MediaPortal_ClientCompletedShareImport", OnMPnP10ClientCompletedShareImport,
          new DvArgument[] {
            new DvArgument("ShareId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp10ClientCompletedShareImportAction);

      DvAction mpnp10GetCurrentlyImportingSharesAction = new DvAction("X_MediaPortal_GetCurrentlyImportingShares", OnMPnP10GetCurrentlyImportingShares,
          new DvArgument[] {
          },
          new DvArgument[] {
            new DvArgument("ShareIds", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.Out, true), 
          });
      AddAction(mpnp10GetCurrentlyImportingSharesAction);

      // Media playback

      //Superseded
      //DvAction mpnp10NotifyPlaybackAction = new DvAction("X_MediaPortal_NotifyPlayback", OnMPnP10NotifyPlayback,
      //    new DvArgument[] {
      //      new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.In), 
      //    },
      //    new DvArgument[] {
      //    });
      //AddAction(mpnp10NotifyPlaybackAction);
      
      #endregion

      #region MPnP 1.1 Device Actions

      DvAction mpnp11BrowseAction = new DvAction("X_MediaPortal_Browse", OnMPnP11Browse,
          new DvArgument[] {
            new DvArgument("ParentDirectory", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In), 
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In),
            new DvArgument("UserProfile", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11BrowseAction);

      DvAction mpnp11SearchAction = new DvAction("X_MediaPortal_Search", OnMPnP11Search,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("Offset", A_ARG_TYPE_Index, ArgumentDirection.In),
            new DvArgument("Limit", A_ARG_TYPE_Count, ArgumentDirection.In),
            new DvArgument("UserProfile", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11SearchAction);

      DvAction mpnp11TextSearchAction = new DvAction("X_MediaPortal_SimpleTextSearch", OnMPnP11TextSearch,
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
            new DvArgument("UserProfile", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11TextSearchAction);

      DvAction mpnp11GetValueGroupsAction = new DvAction("X_MediaPortal_GetValueGroups", OnMPnP11GetValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ValueGroups", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11GetValueGroupsAction);

      DvAction mpnp11GetKeyValueGroupsAction = new DvAction("X_MediaPortal_GetKeyValueGroups", OnMPnP11GetKeyValueGroups,
          new DvArgument[] {
            new DvArgument("KeyMIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("KeyAttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("ValueMIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("ValueAttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ValueGroups", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
            new DvArgument("ValueKeys", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11GetKeyValueGroupsAction);

      DvAction mpnp11GroupValueGroupsAction = new DvAction("X_MediaPortal_GroupValueGroups", OnMPnP11GroupValueGroups,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("SelectAttributeFilter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("ProjectionFunction", A_ARG_TYPE_ProjectionFunction, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("GroupingFunction", A_ARG_TYPE_GroupingFunction, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("ResultGroups", A_ARG_TYPE_MLQueryResultGroupEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11GroupValueGroupsAction);

      DvAction mpnp11CountMediaItemsAction = new DvAction("X_MediaPortal_CountMediaItems", OnMPnP11CountMediaItems,
          new DvArgument[] {
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
            new DvArgument("OnlineState", A_ARG_TYPE_OnlineState, ArgumentDirection.In),
            new DvArgument("IncludeVirtual", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("NumMediaItems", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11CountMediaItemsAction);

      DvAction mpnp11LoadCustomPlaylistAction = new DvAction("X_MediaPortal_LoadCustomPlaylist", OnMPnP11LoadCustomPlaylist,
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
      AddAction(mpnp11LoadCustomPlaylistAction);

      DvAction mpnp11LoadItemAction = new DvAction("X_MediaPortal_LoadItem", OnMPnP11LoadItem,
          new DvArgument[] {
            new DvArgument("SystemId", A_ARG_TYPE_SystemId, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("UserProfile", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItem", A_ARG_TYPE_MediaItem, ArgumentDirection.Out, true),
          });
      AddAction(mpnp11LoadItemAction);

      // Media playback

      DvAction mpnp11NotifyPlaybackAction = new DvAction("X_MediaPortal_NotifyPlayback", OnMPnP11NotifyPlayback,
          new DvArgument[] {
            new DvArgument("MediaItemId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("Watched", A_ARG_TYPE_Bool, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(mpnp11NotifyPlaybackAction);

      #endregion

      // More actions go here

      #endregion

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

    #region Device Actions Implementation

    #region UPnP 1.0 Device Actions Implementation

    /// <summary>
    /// UPnP 1.0 - 2.7.1 GetSearchCapabilities - Required
    /// </summary>
    /// <param name="action"></param>
    /// <param name="inParams"></param>
    /// <param name="outParams"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    static UPnPError OnGetSearchCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>
      {
        "", // fields which can be used to sort, examples: dc:title,dc:creator,dc:date,res@size
      };
      return null;
    }

    /// <summary>
    /// UPnP 1.0 - 2.7.2 GetSortCapabilities - Required
    /// </summary>
    /// <param name="action"></param>
    /// <param name="inParams"></param>
    /// <param name="outParams"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    static UPnPError OnGetSortCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>
      {
        "", // fields which can be used to sort, examples: dc:title,dc:creator,dc:date,res@size
      };
      return null;
    }

    /// <summary>
    /// UPnP 1.0 - 2.7.3 GetSystemUpdateId - Required
    /// </summary>
    /// <param name="action"></param>
    /// <param name="inParams"></param>
    /// <param name="outParams"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    static UPnPError OnGetSystemUpdateID(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>
      {
        0, // SystemUpdateID, always zero unless someone wishes to implement it.
      };
      return null;
    }

    /// <summary>
    /// UPnP 1.0 - 2.7.4 Browse - Required
    /// </summary>
    /// <param name="action"></param>
    /// <param name="inParams"></param>
    /// <param name="outParams"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <example>
    ///  new DvArgument[] {
    ///  new DvArgument("ObjectId", A_ARG_TYPE_Uuid, ArgumentDirection.In),               // ParentDirectory
    ///  new DvArgument("BrowseFlag", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),  // NecessaryMIATypes
    ///  new DvArgument("Filter", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),      // OptionalMIATypes
    ///  new DvArgument("StartingIndex", A_ARG_TYPE_Index, ArgumentDirection.In), 
    ///  new DvArgument("RequestedCount", A_ARG_TYPE_Count, ArgumentDirection.In), 
    ///  new DvArgument("SortCriteria", A_ARG_TYPE_SortCriteria, ArgumentDirection.In), 
    ///},
    ///new DvArgument[] {
    ///  new DvArgument("Result", A_ARG_TYPE_Result, ArgumentDirection.Out, true),
    ///  new DvArgument("NumberReturned", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
    ///  new DvArgument("TotalMatches", A_ARG_TYPE_Count, ArgumentDirection.Out, true),
    ///  new DvArgument("UpdateID", A_ARG_TYPE_UpdateID, ArgumentDirection.Out, true), 
    ///});
    /// </example>
    static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      //Guid parentDirectoryId = (string)inParams[0] == "0" ? Guid.Empty : MarshallingHelper.DeserializeGuid((string)inParams[0]);
      //string browseFlag = inParams[1].ToString();
      //string filter = inParams[2].ToString();
      //int startIndex = Convert.ToInt32(inParams[3]);
      //int requestedCount = Convert.ToInt32(inParams[4]);
      //string sortCriteria = (string)inParams[5];

      //var necessaryMIATypes = new List<Guid> { DirectoryAspect.ASPECT_ID };

      //MediaItemQuery query = new MediaItemQuery(necessaryMIATypes, null);

      //IList<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().Search(query, false, startIndex, requestedCount);

      //outParams = new List<object>
      //{
      //  result,       // Result
      //  null,         // NumberReturned
      //  result.Count, // TotalMatches
      //  null          // UpdateID
      //};
      outParams = null;
      return null;
    }

    /// <summary>
    /// UPnP 1.0 - 2.7.5 Search - Optional
    /// </summary>
    /// <param name="action"></param>
    /// <param name="inParams"></param>
    /// <param name="outParams"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      //MediaItemQuery query = (MediaItemQuery)inParams[0];
      //string onlineStateStr = (string)inParams[1];
      //bool all;
      //UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all);
      //if (error != null)
      //{
      //  outParams = null;
      //  return error;
      //}
      //IList<MediaItem> mediaItems = ServiceRegistration.Get<IMediaLibrary>().Search(query, !all);
      //outParams = new List<object>
      //{
      //  mediaItems,       // Result
      //  null,             // NumberReturned
      //  mediaItems.Count, // TotalMatches
      //  null              // UpdateID
      //};
      outParams = null;
      return null;
    }

    // UPnP 1.0 - 2.7.6 CreateObject - Optional
    // UPnP 1.0 - 2.7.7 DestoryObject - Optional
    // UPnP 1.0 - 2.7.8 UpdateObject - Optional
    // UPnP 1.0 - 2.7.9 ImportResource - Optional
    // UPnP 1.0 - 2.7.10 ExportResource - Optional
    // UPnP 1.0 - 2.7.11 StopTransferResource - Optional
    // UPnP 1.0 - 2.7.12 GetTransferProgress - Optional
    // UPnP 1.0 - 2.7.13 DeleteResource - Optional
    // UPnP 1.0 - 2.7.14 CreateReference - Optional

    #endregion

    #region MPnP 1.0 Device Actions Implementation

    static UPnPError OnMPnP10RegisterShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Share share = (Share) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().RegisterShare(share);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10RemoveShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().RemoveShare(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10UpdateShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ResourcePath baseResourcePath = ResourcePath.Deserialize((string) inParams[1]);
      string shareName = (string) inParams[2];
      bool useShareWatcher = (bool) inParams[3];
      string[] mediaCategories = ((string) inParams[4]).Split(',');
      string relocateMediaItemsStr = (string) inParams[5];
      RelocationMode relocationMode;
      UPnPError error = ParseRelocationMode("RelocateMediaItems", relocateMediaItemsStr, out relocationMode);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      int numAffected = mediaLibrary.UpdateShare(shareId, baseResourcePath, shareName, useShareWatcher, mediaCategories, relocationMode);
      outParams = new List<object> {numAffected};
      return null;
    }

    static UPnPError OnMPnP10GetShares(DvAction action, IList<object> inParams, out IList<object> outParams,
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

    static UPnPError OnMPnP10GetShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Share result = ServiceRegistration.Get<IMediaLibrary>().GetShare(shareId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10ReImportShare(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      Share share = ServiceRegistration.Get<IMediaLibrary>().GetShare(shareId);
      ServiceRegistration.Get<IImporterWorker>().ScheduleRefresh(share.BaseResourcePath, share.MediaCategories, true);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10SetupDefaultServerShares(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ServiceRegistration.Get<IMediaLibrary>().SetupDefaultLocalShares();
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10AddMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().AddMediaItemAspectStorage(miam);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10RemoveMediaItemAspectStorage(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().RemoveMediaItemAspectStorage(aspectId);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10GetAllManagedMediaItemAspectTypes(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<Guid> result = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata().Keys;
      outParams = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(result)};
      return null;
    }

    static UPnPError OnMPnP10GetMediaItemAspectMetadata(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      MediaItemAspectMetadata miam = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata(aspectId);
      outParams = new List<object> {miam};
      return null;
    }

    static UPnPError OnMPnP10GetAllManagedMediaItemAspectCreationDates(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IDictionary<Guid, DateTime> result = ServiceRegistration.Get<IMediaLibrary>().GetManagedMediaItemAspectCreationDates();
      outParams = new List<object> { result };
      return null;
    }

    static UPnPError OnMPnP10LoadItem(DvAction action, IList<object> inParams, out IList<object> outParams,
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

    static UPnPError OnMPnP10Browse(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid parentDirectoryId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      ICollection<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().Browse(parentDirectoryId, necessaryMIATypes, 
        optionalMIATypes, null, false, null, null);

      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10Search(DvAction action, IList<object> inParams, out IList<object> outParams,
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
      IList<MediaItem> mediaItems = ServiceRegistration.Get<IMediaLibrary>().Search(query, !all, null, false);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnMPnP10TextSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
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
      IList<MediaItem> mediaItems = mediaLibrary.Search(query, !all, null, false);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnMPnP10GetValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
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
          projectionFunction, necessaryMIATypes, filter, !all, false);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnMPnP10GroupValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
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
          selectAttributeFilter, projectionFunction, necessaryMIATypes, filter, !all, groupingFunction, false);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnMPnP10CountMediaItems(DvAction action, IList<object> inParams, out IList<object> outParams,
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
      int numMediaItems = ServiceRegistration.Get<IMediaLibrary>().CountMediaItems(necessaryMIATypes, filter, !all, false);
      outParams = new List<object> {numMediaItems};
      return null;
    }

    static UPnPError OnMPnP10GetPlaylists(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      ICollection<PlaylistInformationData> result = ServiceRegistration.Get<IMediaLibrary>().GetPlaylists();
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10SavePlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      PlaylistRawData playlistData = (PlaylistRawData) inParams[0];
      ServiceRegistration.Get<IMediaLibrary>().SavePlaylist(playlistData);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10DeletePlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      bool result = ServiceRegistration.Get<IMediaLibrary>().DeletePlaylist(playlistId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10ExportPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid playlistId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      PlaylistRawData result = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(playlistId);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10LoadCustomPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IList<Guid> mediaItemIds = MarshallingHelper.ParseCsvGuidCollection((string) inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string) inParams[2]);
      IList<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().LoadCustomPlaylist(
        mediaItemIds, necessaryMIATypes, optionalMIATypes, null, null);
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnMPnP10AddOrUpdateMediaItem(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid parentDirectoryId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string systemId = (string) inParams[1];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[2]);
      IEnumerable<MediaItemAspect> mediaItemAspects = (IEnumerable<MediaItemAspect>) inParams[3];
      Guid mediaItemId = ServiceRegistration.Get<IMediaLibrary>().AddOrUpdateMediaItem(parentDirectoryId, systemId, path, mediaItemAspects, true);
      outParams = new List<object> {MarshallingHelper.SerializeGuid(mediaItemId)};
      return null;
    }

    static UPnPError OnMPnP10DeleteMediaItemOrPath(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      bool inclusive = (bool) inParams[2];
      ServiceRegistration.Get<IMediaLibrary>().DeleteMediaItemOrPath(systemId, path, inclusive);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10ClientStartedShareImport(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().ClientStartedShareImport(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10ClientCompletedShareImport(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid shareId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      ServiceRegistration.Get<IMediaLibrary>().ClientCompletedShareImport(shareId);
      outParams = null;
      return null;
    }

    static UPnPError OnMPnP10GetCurrentlyImportingShares(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      outParams = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(mediaLibrary.GetCurrentlyImportingShareIds())};
      return null;
    }

    static UPnPError OnMPnP10NotifyPlayback(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      Guid mediaItemId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      mediaLibrary.NotifyPlayback(mediaItemId, true);
      outParams = null;
      return null;
    }

    #endregion

    #region MPnP 1.1 Device Actions Implementation

    static UPnPError OnMPnP11Browse(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid parentDirectoryId = MarshallingHelper.DeserializeGuid((string)inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[2]);
      uint? offset = (uint?)inParams[3];
      uint? limit = (uint?)inParams[4];
      Guid? userProfile = null;
      if (!string.IsNullOrEmpty((string)inParams[5]))
        userProfile = MarshallingHelper.DeserializeGuid((string)inParams[5]);
      bool includeVirtual = (bool)inParams[6];
      IList<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().Browse(parentDirectoryId, necessaryMIATypes, optionalMIATypes, userProfile, includeVirtual, offset, limit);

      outParams = new List<object> { result };
      return null;
    }

    static UPnPError OnMPnP11Search(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      MediaItemQuery query = (MediaItemQuery)inParams[0];
      string onlineStateStr = (string)inParams[1];
      uint? offset = (uint?)inParams[2];
      uint? limit = (uint?)inParams[3];
      Guid? userProfile = null;
      if (!string.IsNullOrEmpty((string)inParams[4]))
        userProfile = MarshallingHelper.DeserializeGuid((string)inParams[4]);
      bool includeVirtual = (bool)inParams[5];
      bool all;
      UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
      {
        outParams = null;
        return error;
      }
      if (!query.Limit.HasValue)
        query.Limit = limit;
      if (!query.Offset.HasValue)
        query.Offset = offset;
      IList<MediaItem> mediaItems = ServiceRegistration.Get<IMediaLibrary>().Search(query, !all, userProfile, includeVirtual);
      outParams = new List<object> { mediaItems };
      return null;
    }

    static UPnPError OnMPnP11TextSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string searchText = (string)inParams[0];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[2]);
      IFilter filter = (IFilter)inParams[3];
      string searchModeStr = (string)inParams[4];
      string onlineStateStr = (string)inParams[5];
      string capitalizationMode = (string)inParams[6];
      uint? offset = (uint?)inParams[7];
      uint? limit = (uint?)inParams[8];
      Guid? userProfile = null;
      if (!string.IsNullOrEmpty((string)inParams[9]))
        userProfile = MarshallingHelper.DeserializeGuid((string)inParams[9]);
      bool includeVirtual = (bool)inParams[10];
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
      query.Limit = limit;
      query.Offset = offset;
      IList<MediaItem> mediaItems = mediaLibrary.Search(query, !all, userProfile, includeVirtual);
      outParams = new List<object> { mediaItems };
      return null;
    }

    static UPnPError OnMPnP11GetValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string)inParams[0]);
      string attributeName = (string)inParams[1];
      IFilter selectAttributeFilter = (IFilter)inParams[2];
      string projectionFunctionStr = (string)inParams[3];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[4]);
      IFilter filter = (IFilter)inParams[5];
      string onlineStateStr = (string)inParams[6];
      bool includeVirtual = (bool)inParams[7];
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
          projectionFunction, necessaryMIATypes, filter, !all, includeVirtual);
      outParams = new List<object> { values };
      return null;
    }

    static UPnPError OnMPnP11GetKeyValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid keyAspectId = MarshallingHelper.DeserializeGuid((string)inParams[0]);
      string keyAttributeName = (string)inParams[1];
      Guid valueAspectId = MarshallingHelper.DeserializeGuid((string)inParams[2]);
      string valueAttributeName = (string)inParams[3];
      IFilter selectAttributeFilter = (IFilter)inParams[4];
      string projectionFunctionStr = (string)inParams[5];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[6]);
      IFilter filter = (IFilter)inParams[7];
      string onlineStateStr = (string)inParams[8];
      bool includeVirtual = (bool)inParams[9];
      
      outParams = null;
      ProjectionFunction projectionFunction;
      bool all = true;
      UPnPError error = ParseProjectionFunction("ProjectionFunction", projectionFunctionStr, out projectionFunction) ??
          ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
        return error;
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata keyMiam;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(keyAspectId, out keyMiam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", keyAspectId));
      MediaItemAspectMetadata.AttributeSpecification keyAttributeType;
      if (!keyMiam.AttributeSpecifications.TryGetValue(keyAttributeName, out keyAttributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            keyAspectId, keyAttributeName));
      MediaItemAspectMetadata valueMiam;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(valueAspectId, out valueMiam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", valueAspectId));
      MediaItemAspectMetadata.AttributeSpecification valueAttributeType;
      if (!valueMiam.AttributeSpecifications.TryGetValue(valueAttributeName, out valueAttributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            valueAspectId, valueAttributeName));
      Tuple<HomogenousMap, HomogenousMap> values = ServiceRegistration.Get<IMediaLibrary>().GetKeyValueGroups(keyAttributeType, valueAttributeType, selectAttributeFilter,
          projectionFunction, necessaryMIATypes, filter, !all, includeVirtual);
      outParams = new List<object> { values.Item1, values.Item2 };
      return null;
    }

    static UPnPError OnMPnP11GroupValueGroups(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid aspectId = MarshallingHelper.DeserializeGuid((string)inParams[0]);
      string attributeName = (string)inParams[1];
      IFilter selectAttributeFilter = (IFilter)inParams[2];
      string projectionFunctionStr = (string)inParams[3];
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[4]);
      IFilter filter = (IFilter)inParams[5];
      string onlineStateStr = (string)inParams[6];
      string groupingFunctionStr = (string)inParams[7];
      bool includeVirtual = (bool)inParams[8];
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
          selectAttributeFilter, projectionFunction, necessaryMIATypes, filter, !all, groupingFunction, includeVirtual);
      outParams = new List<object> { values };
      return null;
    }

    static UPnPError OnMPnP11CountMediaItems(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[0]);
      IFilter filter = (IFilter)inParams[1];
      string onlineStateStr = (string)inParams[2];
      bool includeVirtual = (bool)inParams[3];
      outParams = null;
      bool all;
      UPnPError error = ParseOnlineState("OnlineState", onlineStateStr, out all);
      if (error != null)
        return error;
      int numMediaItems = ServiceRegistration.Get<IMediaLibrary>().CountMediaItems(necessaryMIATypes, filter, !all, includeVirtual);
      outParams = new List<object> { numMediaItems };
      return null;
    }

    static UPnPError OnMPnP11LoadCustomPlaylist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IList<Guid> mediaItemIds = MarshallingHelper.ParseCsvGuidCollection((string)inParams[0]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[1]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[2]);
      uint? offset = (uint?)inParams[3];
      uint? limit = (uint?)inParams[4];
      IList<MediaItem> result = ServiceRegistration.Get<IMediaLibrary>().LoadCustomPlaylist(
          mediaItemIds, necessaryMIATypes, optionalMIATypes, offset, limit);
      outParams = new List<object> { result };
      return null;
    }

    static UPnPError OnMPnP11LoadItem(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      string systemId = (string)inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string)inParams[1]);
      IEnumerable<Guid> necessaryMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[2]);
      IEnumerable<Guid> optionalMIATypes = MarshallingHelper.ParseCsvGuidCollection((string)inParams[3]);
      Guid? userProfile = null;
      if(!string.IsNullOrEmpty((string)inParams[4]))
        userProfile = MarshallingHelper.DeserializeGuid((string)inParams[4]);
      MediaItem mediaItem = ServiceRegistration.Get<IMediaLibrary>().LoadItem(systemId, path,
          necessaryMIATypes, optionalMIATypes, userProfile);
      outParams = new List<object> { mediaItem };
      return null;
    }

    static UPnPError OnMPnP11NotifyPlayback(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      Guid mediaItemId = MarshallingHelper.DeserializeGuid((string)inParams[0]);
      bool watched = (bool)inParams[1];
      mediaLibrary.NotifyPlayback(mediaItemId, watched);
      outParams = null;
      return null;
    }

    #endregion

    #endregion
  }
}
