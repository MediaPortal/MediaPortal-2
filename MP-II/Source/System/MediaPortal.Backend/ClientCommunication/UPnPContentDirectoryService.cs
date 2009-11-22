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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Core.UPnP;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Utilities;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Provides the UPnP service for the MediaPortal-II content directory.
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

      // Used to transport a system name - contains the hostname string
      DvStateVariable A_ARG_TYPE_SystemName = new DvStateVariable("A_ARG_TYPE_SystemName", new DvStandardDataType(UPnPStandardDataType.String))
          {
            SendEvents = false
          };
      AddStateVariable(A_ARG_TYPE_SystemName);

      // Used to transport a provider path expression
      DvStateVariable A_ARG_TYPE_ResourcePath = new DvStateVariable("A_ARG_TYPE_ProviderPath", new DvStandardDataType(UPnPStandardDataType.String))
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

      // Used to filter requested shares.
      DvStateVariable A_ARG_TYPE_SharesFilter = new DvStateVariable("A_ARG_TYPE_SharesFilter", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
            AllowedValueList = new List<string> {"All", "ConnectedShares"}
        };
      AddStateVariable(A_ARG_TYPE_SharesFilter);

      // Used to transport an argument of type MediaItemQuery
      DvStateVariable A_ARG_TYPE_MediaItemQuery = new DvStateVariable("A_ARG_TYPE_MediaItemQuery", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaItemQuery))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_MediaItemQuery);

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

      // Used to transport a collection of media item aspects for a media item specified elsewhere
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
            new DvArgument("System", A_ARG_TYPE_SystemName, ArgumentDirection.In),
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

      // Media query

      DvAction searchAction = new DvAction("Search", OnSearch,
          new DvArgument[] {
            new DvArgument("Query", A_ARG_TYPE_MediaItemQuery, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(searchAction);

      DvAction browseAction = new DvAction("Browse", OnBrowse,
          new DvArgument[] {
            new DvArgument("System", A_ARG_TYPE_SystemName, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("NecessaryMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
            new DvArgument("OptionalMIATypes", A_ARG_TYPE_UuidEnumeration, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("MediaItems", A_ARG_TYPE_MediaItems, ArgumentDirection.Out, true),
          });
      AddAction(browseAction);

      DvAction getDistinctAssociatedValuesAction = new DvAction("GetDistinctAssociatedValues", OnGetDistinctAssociatedValues,
          new DvArgument[] {
            new DvArgument("MIAType", A_ARG_TYPE_Uuid, ArgumentDirection.In),
            new DvArgument("AttributeName", A_ARG_TYPE_Name, ArgumentDirection.In),
            new DvArgument("Filter", A_ARG_TYPE_MediaItemFilter, ArgumentDirection.In),
          },
          new DvArgument[] {
            new DvArgument("DistinctValues", A_ARG_TYPE_MediaItemAttributeValues, ArgumentDirection.Out, true),
          });
      AddAction(getDistinctAssociatedValuesAction);

      // Media import

      DvAction addOrUpdateMediaItemAction = new DvAction("AddOrUpdateMediaItem", OnAddOrUpdateMediaItem,
          new DvArgument[] {
            new DvArgument("System", A_ARG_TYPE_SystemName, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
            new DvArgument("UpdatedMediaItemAspects", A_ARG_TYPE_MediaItemAspects, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(addOrUpdateMediaItemAction);

      DvAction deleteMediaItemOrPathAction = new DvAction("DeleteMediaItemOrPath", OnDeleteMediaItemOrPath,
          new DvArgument[] {
            new DvArgument("System", A_ARG_TYPE_SystemName, ArgumentDirection.In),
            new DvArgument("Path", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
          });
      AddAction(deleteMediaItemOrPathAction);

      // More actions go here
    }

    static UPnPError OnRegisterShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Share share = (Share) inParams[0];
      ServiceScope.Get<IMediaLibrary>().RegisterShare(share);
      outParams = null;
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
      Share oldShare = mediaLibrary.GetShare(shareId);
      int numAffected = mediaLibrary.UpdateShare(
          shareId, oldShare.NativeSystem, baseResourcePath, shareName, mediaCategories, relocationMode);
      // TODO
      //if (relocationMode == RelocationMode.Remove)
      //  ... schedule reimport of new diretory ...
      outParams = new List<object> {numAffected};
      return null;
    }

    static UPnPError OnGetShares(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName system = new SystemName((string) inParams[0]);
      string sharesFilterStr = (string) inParams[1];
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
      IDictionary<Guid, Share> shares = ServiceScope.Get<IMediaLibrary>().GetShares(system, onlyConnected);
      outParams = new List<object> {shares.Values};
      return null;
    }

    static UPnPError OnGetShare(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid shareId = new Guid((string) inParams[0]);
      Share result = ServiceScope.Get<IMediaLibrary>().GetShare(shareId);
      outParams = new List<object> {result};
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
      ICollection<Guid> result = ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata().Keys;
      string miaTypeIDs = StringUtils.Join(",", result);
      outParams = new List<object> {miaTypeIDs};
      return null;
    }

    static UPnPError OnGetMediaItemAspectMetadata(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      MediaItemAspectMetadata miam = ServiceScope.Get<IMediaLibrary>().GetManagedMediaItemAspectMetadata(aspectId);
      outParams = new List<object> {miam};
      return null;
    }

    static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      MediaItemQuery query = (MediaItemQuery) inParams[0];
      IList<MediaItem> mediaItems = ServiceScope.Get<IMediaLibrary>().Search(query);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName system = (SystemName) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      IEnumerable<Guid> necessaryMIATypes = ParserHelper.ParseCsvGuidCollection((string) inParams[2]);
      IEnumerable<Guid> optionalMIATypes = ParserHelper.ParseCsvGuidCollection((string) inParams[3]);
      ICollection<MediaItem> mediaItems = ServiceScope.Get<IMediaLibrary>().Browse(system, path, necessaryMIATypes, optionalMIATypes);
      outParams = new List<object> {mediaItems};
      return null;
    }

    static UPnPError OnGetDistinctAssociatedValues(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      Guid aspectId = new Guid((string) inParams[0]);
      string attributeName = (string) inParams[1];
      IFilter filter = (IFilter) inParams[2];
      IMediaItemAspectTypeRegistration miatr = ServiceScope.Get<IMediaItemAspectTypeRegistration>();
      MediaItemAspectMetadata miam;
      outParams = null;
      if (!miatr.LocallyKnownMediaItemAspectTypes.TryGetValue(aspectId, out miam))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' is unknown", aspectId));
      MediaItemAspectMetadata.AttributeSpecification attributeType;
      if (!miam.AttributeSpecifications.TryGetValue(attributeName, out attributeType))
        return new UPnPError(600, string.Format("Media item aspect type '{0}' doesn't contain an attribute of name '{1}'",
            aspectId, attributeName));
      HomogenousCollection values = ServiceScope.Get<IMediaLibrary>().GetDistinctAssociatedValues(attributeType, filter);
      outParams = new List<object> {values};
      return null;
    }

    static UPnPError OnAddOrUpdateMediaItem(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName system = (SystemName) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      ICollection<MediaItemAspect> mediaItemAspects = (ICollection<MediaItemAspect>) inParams[2];
      ServiceScope.Get<IMediaLibrary>().AddOrUpdateMediaItem(system, path, mediaItemAspects);
      outParams = null;
      return null;
    }

    static UPnPError OnDeleteMediaItemOrPath(DvAction action, IList<object> inParams, out IList<object> outParams)
    {
      SystemName system = (SystemName) inParams[0];
      ResourcePath path = ResourcePath.Deserialize((string) inParams[1]);
      ServiceScope.Get<IMediaLibrary>().DeleteMediaItemOrPath(system, path);
      outParams = null;
      return null;
    }
  }
}
