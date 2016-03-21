#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.DIDL;
using MediaPortal.Plugins.MediaServer.Filters;
using MediaPortal.Plugins.MediaServer.Objects;
using MediaPortal.Plugins.MediaServer.Objects.Basic;
using MediaPortal.Plugins.MediaServer.Objects.MediaLibrary;
using MediaPortal.Plugins.MediaServer.Parser;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;

namespace MediaPortal.Plugins.MediaServer
{
  public class UPnPContentDirectoryServiceImpl : DvService
  {
    public UPnPContentDirectoryServiceImpl()
      : base(
        UPnPMediaServerDevice.CONTENT_DIRECTORY_SERVICE_TYPE,
        UPnPMediaServerDevice.CONTENT_DIRECTORY_SERVICE_TYPE_VERSION,
        UPnPMediaServerDevice.CONTENT_DIRECTORY_SERVICE_ID)
    {
      // Used for a boolean value
      DvStateVariable A_ARG_TYPE_BrowseFlag = new DvStateVariable("A_ARG_TYPE_BrowseFlag", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false,
        AllowedValueList = new List<string> { "BrowseMetadata", "BrowseDirectChildren" }
      };
      AddStateVariable(A_ARG_TYPE_BrowseFlag);

      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Count);

      DvStateVariable A_ARG_TYPE_Filter = new DvStateVariable("A_ARG_TYPE_Filter", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Filter);

      DvStateVariable A_ARG_TYPE_Index = new DvStateVariable("A_ARG_TYPE_Index", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Index);

      DvStateVariable A_ARG_TYPE_ObjectID = new DvStateVariable("A_ARG_TYPE_ObjectID", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_ObjectID);

      DvStateVariable A_ARG_TYPE_Result = new DvStateVariable("A_ARG_TYPE_Result", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Result);

      DvStateVariable A_ARG_TYPE_SearchCriteria = new DvStateVariable("A_ARG_TYPE_SearchCriteria", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_SearchCriteria);

      DvStateVariable A_ARG_TYPE_SortCriteria = new DvStateVariable("A_ARG_TYPE_SortCriteria", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_SortCriteria);

      DvStateVariable A_ARG_TYPE_UpdateID = new DvStateVariable("A_ARG_TYPE_UpdateID", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_UpdateID);

      DvStateVariable SearchCapabilities = new DvStateVariable("SearchCapabilities", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(SearchCapabilities);

      DvStateVariable SortCapabilities = new DvStateVariable("SortCapabilities", new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(SortCapabilities);

      DvStateVariable SystemUpdateID = new DvStateVariable("SystemUpdateID", new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = true
      };
      AddStateVariable(SystemUpdateID);

      DvStateVariable A_ARG_TYPE_Featurelist = new DvStateVariable("A_ARG_TYPE_Featurelist",
                                                          new DvStandardDataType(UPnPStandardDataType.String))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_Featurelist);

      DvStateVariable A_ARG_TYPE_CategoryType = new DvStateVariable("A_ARG_TYPE_CategoryType",
                                                    new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_CategoryType);

      DvStateVariable A_ARG_TYPE_RID = new DvStateVariable("A_ARG_TYPE_RID",
                                                    new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_RID);

      DvStateVariable A_ARG_TYPE_PosSec = new DvStateVariable("A_ARG_TYPE_PosSec",
                                                    new DvStandardDataType(UPnPStandardDataType.Ui4))
      {
        SendEvents = false
      };
      AddStateVariable(A_ARG_TYPE_PosSec);

      DvAction browseAction = new DvAction("Browse", OnBrowse,
                                           new DvArgument[]
                                             {
                                               new DvArgument("ObjectID", A_ARG_TYPE_ObjectID,
                                                              ArgumentDirection.In),
                                               new DvArgument("BrowseFlag", A_ARG_TYPE_BrowseFlag,
                                                              ArgumentDirection.In),
                                               new DvArgument("Filter", A_ARG_TYPE_Filter,
                                                              ArgumentDirection.In),
                                               new DvArgument("StartingIndex",
                                                              A_ARG_TYPE_Index,
                                                              ArgumentDirection.In),
                                               new DvArgument("RequestedCount",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.In),
                                               new DvArgument("SortCriteria",
                                                              A_ARG_TYPE_SortCriteria,
                                                              ArgumentDirection.In)
                                             },
                                           new DvArgument[]
                                             {
                                               new DvArgument("Result",
                                                              A_ARG_TYPE_Result,
                                                              ArgumentDirection.Out),
                                               new DvArgument("NumberReturned",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out),
                                               new DvArgument("TotalMatches",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out),
                                               new DvArgument("UpdateID",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out)
                                             });
      AddAction(browseAction);

      DvAction getSearchCapabilitiesAction = new DvAction("GetSearchCapabilities", OnGetSearchCapabilities,
                                                          new DvArgument[]
                                                            {
                                                            },
                                                          new DvArgument[]
                                                            {
                                                              new DvArgument("SearchCaps",
                                                                             SearchCapabilities,
                                                                             ArgumentDirection.Out),
                                                            });
      AddAction(getSearchCapabilitiesAction);

      DvAction getSortCapabilitiesAction = new DvAction("GetSortCapabilities", OnGetSortCapabilities,
                                                        new DvArgument[]
                                                          {
                                                          },
                                                        new DvArgument[]
                                                          {
                                                            new DvArgument("SortCaps",
                                                                           SortCapabilities,
                                                                           ArgumentDirection.Out),
                                                          });
      AddAction(getSortCapabilitiesAction);

      DvAction getSystemUpdateIDAcion = new DvAction("GetSystemUpdateID", OnGetSystemUpdateID,
                                                     new DvArgument[]
                                                       {
                                                       },
                                                     new DvArgument[]
                                                       {
                                                         new DvArgument("Id",
                                                                        SystemUpdateID,
                                                                        ArgumentDirection.Out),
                                                       });
      AddAction(getSystemUpdateIDAcion);

      DvAction searchAction = new DvAction("Search", OnSearch,
                                           new DvArgument[]
                                             {
                                               new DvArgument("ContainerID", A_ARG_TYPE_ObjectID,
                                                              ArgumentDirection.In),
                                               new DvArgument("SearchCriteria", A_ARG_TYPE_SearchCriteria,
                                                              ArgumentDirection.In),
                                               new DvArgument("Filter", A_ARG_TYPE_Filter,
                                                              ArgumentDirection.In),
                                               new DvArgument("StartingIndex",
                                                              A_ARG_TYPE_Index,
                                                              ArgumentDirection.In),
                                               new DvArgument("RequestedCount",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.In),
                                               new DvArgument("SortCriteria",
                                                              A_ARG_TYPE_SortCriteria,
                                                              ArgumentDirection.In)
                                             },
                                           new DvArgument[]
                                             {
                                               new DvArgument("Result",
                                                              A_ARG_TYPE_Result,
                                                              ArgumentDirection.Out),
                                               new DvArgument("NumberReturned",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out),
                                               new DvArgument("TotalMatches",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out),
                                               new DvArgument("UpdateID",
                                                              A_ARG_TYPE_Count,
                                                              ArgumentDirection.Out)
                                             });
      AddAction(searchAction);

      DvAction getX_GetFeatureList = new DvAction("X_GetFeatureList", OnX_GetFeatureList,
                                         new DvArgument[]
                                                          {
                                                          },
                                         new DvArgument[]
                                                          {
                                                              new DvArgument("FeatureList",
                                                                             A_ARG_TYPE_Featurelist,
                                                                             ArgumentDirection.Out),
                                                          });
      AddAction(getX_GetFeatureList);

      DvAction getX_SetBookmark = new DvAction("X_SetBookmark", OnX_SetBookmark,
                                         new DvArgument[]
                                                          {
                                                              new DvArgument("CategoryType", A_ARG_TYPE_CategoryType,
                                                                             ArgumentDirection.In),
                                                              new DvArgument("RID", A_ARG_TYPE_RID,
                                                                             ArgumentDirection.In),
                                                              new DvArgument("ObjectID", A_ARG_TYPE_ObjectID,
                                                                             ArgumentDirection.In),
                                                              new DvArgument("PosSecond",
                                                                             A_ARG_TYPE_PosSec,
                                                                             ArgumentDirection.In)
                                                          },
                                         new DvArgument[]
                                                          {
                                                          });
      AddAction(getX_SetBookmark);
    }

    private static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      // In parameters
      var objectId = (string)inParams[0];
      var browseFlag = inParams[1].ToString();
      var filter = inParams[2].ToString();
      var startingIndex = Convert.ToInt32(inParams[3]);
      var requestedCount = Convert.ToInt32(inParams[4]);
      var sortCriteria = (string)inParams[5];

      Logger.Debug(
        "MediaServer - entry OnBrowse(objectId=\"{0}\",browseFlag=\"{1}\",filter=\"{2}\",startingIndex=\"{3}\",requestedCount=\"{4}\",sortCriteria=\"{5}\")",
        objectId, browseFlag, filter, startingIndex, requestedCount, sortCriteria);

      // Out parameters
      int numberReturned = 0;
      int totalMatches = 0;
      int containterUpdateId;

      EndPointSettings deviceClient = ProfileManager.DetectProfile(context.Request.Headers);

      if (deviceClient == null || deviceClient.Profile == null)
      {
        outParams = null;
        return null;
      }

      GenericContentDirectoryFilter deviceFilter = GenericContentDirectoryFilter.GetContentFilter(deviceClient.Profile.DirectoryContentFilter);
      var newObjectId = deviceFilter.FilterObjectId(objectId, false);
      if (newObjectId == null)
      {
        Logger.Debug("MediaServer: Request for container ID {0} ignored", objectId);
        outParams = null;
        return null;
      }
      if (objectId != newObjectId)
      {
        Logger.Debug("MediaServer: Request for container ID {0} intercepted, changing it to {1}", objectId, newObjectId);
        objectId = newObjectId;
      }

      // Find the container object requested
      //var parentDirectoryId = objectId == "0" ? Guid.Empty : MarshallingHelper.DeserializeGuid(objectId);
      var o = deviceClient.RootContainer.FindObject(objectId);
      if (o as BasicContainer == null)
      {
        // We failed to find the container requested
        // throw error!
        throw new ArgumentException(string.Format("Container with ObjectID {0} not found", objectId));
      }
      deviceFilter.FilterContainerClassType(objectId, ref o);
      deviceFilter.FilterClassProperties(objectId, ref o);

      BasicContainer c = o as BasicContainer;
      Logger.Debug("MediaServer Got object {0} / {1} : {2}, {3}", c, c.Id, c.Key, c.Title, c.ChildCount);
      Logger.Debug("MediaServer: Using DIDL content builder {0}", deviceClient.Profile.DirectoryContentBuilder);
      var msgBuilder = GenericDidlMessageBuilder.GetDidlMessageBuilder(deviceClient.Profile.DirectoryContentBuilder);

      // Start to build the XML DIDL-Lite document.
      switch (browseFlag)
      {
        case "BrowseMetadata":
          // Render the container as XML
          msgBuilder.Build(filter, o);

          // We are only after information about 1 container
          numberReturned = 1;
          totalMatches = 1;
          break;
        case "BrowseDirectChildren":
          // Create a new object based on search criteria
          c.Initialise();
          var resultList = c.Browse(sortCriteria);
          Logger.Debug("MediaServer: Browse has {0} results", resultList.Count);
          totalMatches = resultList.Count;

          // Reduce number of items down to a specific range
          if (requestedCount != 0)
          {
            var itemCount = requestedCount;
            // Make sure that the requested itemCount value doesn't exceed total items in the list
            // otherwise we will get an exception.
            if (itemCount + startingIndex > resultList.Count) itemCount = resultList.Count - startingIndex;
            if (itemCount > 0) resultList = resultList.GetRange(startingIndex, itemCount);
            else resultList.Clear();
          }
          numberReturned = resultList.Count;

          // Render this list of containers as XML.
          msgBuilder.BuildAll(filter, resultList);

          break;
        default:
          // Error! invalid browseFlag value.
          break;
      }

      // Grab the container updateid
      //TODO: sort out object updating
      containterUpdateId = 0; // c.UpdateId;

      // Construct the return arguments.
      var xml = msgBuilder.ToString();
      outParams = new List<object>(4) { xml, numberReturned, totalMatches, containterUpdateId };

      Logger.Debug(
        "MediaServer - exit OnBrowse(objectId=\"{0}\"...) = (numberReturned=\"{1}\",totalMatches=\"{2}\",containerUpdateId=\"{3}\") {4}",
        objectId, numberReturned, totalMatches, containterUpdateId, xml);

      // This upnp action doesn't have a return type.
      return null;
    }

    private static UPnPError OnGetSearchCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      Logger.Debug("MediaServer - OnGetSearchCapabilities");
      // TODO: I don't know what upnp:class and res@size are but the UPnP spec had them in the example response
      outParams = new List<object>(1) { "dc:title,dc:creator,upnp:class,res@size" };
      return null;
    }

    private static UPnPError OnGetSortCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      Logger.Debug("MediaServer - OnGetSortCapabilities");
      // TODO: I don't know what res@size is but the UPnP spec had them in the example response
      outParams = new List<object>(1) { "dc:title,dc:creator,res@size" };
      return null;
    }

    private static UPnPError OnGetSystemUpdateID(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      //TODO: Old DNLA clients use this ID to determine if the any content was changed/added since last request
      outParams = new List<object> { 0 };
      return null;
    }

    private static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      // In parameters
      var objectId = (string)inParams[0];
      var searchCriteria = inParams[1].ToString();
      var filter = inParams[2].ToString();
      var startingIndex = Convert.ToUInt32(inParams[3]);
      var requestedCount = Convert.ToUInt32(inParams[4]);
      var sortCriteria = (string)inParams[5];

      Logger.Debug(
        "MediaServer - entry OnSearch(objectId=\"{0}\",searchCriteria=\"{1}\",filter=\"{2}\",startingIndex=\"{3}\",requestedCount=\"{4}\",sortCriteria=\"{5}\")",
        objectId, searchCriteria, filter, startingIndex, requestedCount, sortCriteria);

      // Out parameters
      int numberReturned = 0;
      int totalMatches = 0;

      EndPointSettings deviceClient = ProfileManager.DetectProfile(context.Request.Headers);

      if (deviceClient == null || deviceClient.Profile == null)
      {
        outParams = null;
        return null;
      }

      GenericContentDirectoryFilter deviceFilter = GenericContentDirectoryFilter.GetContentFilter(deviceClient.Profile.DirectoryContentFilter);
      var newObjectId = deviceFilter.FilterObjectId(objectId, true);
      if (newObjectId == null)
      {
        Logger.Debug("MediaServer: Request for container ID {0} ignored", objectId);
        outParams = null;
        return null;
      }
      if (objectId != newObjectId)
      {
        Logger.Debug("MediaServer: Request for container ID {0} intercepted, changing it to {1}", objectId, newObjectId);
        objectId = newObjectId;
      }

      //TODO: DNLA clients use this ID to determine if the any content was changed/added to the container since last request
      int containterUpdateId = 0;

      SearchExp exp = SearchParser.Parse(searchCriteria);

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      IFilter searchFilter = SearchParser.Convert(exp, necessaryMIATypes);
      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      if (necessaryMIATypes.Contains(VideoAspect.ASPECT_ID) == false)
      {
        optionalMIATypes.Add(VideoAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoAudioAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoEmbeddedAspect.ASPECT_ID);
      }
      if (necessaryMIATypes.Contains(AudioAspect.ASPECT_ID) == false)
      {
        optionalMIATypes.Add(AudioAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemAudioAspect.ASPECT_ID);
      }
      if (necessaryMIATypes.Contains(ImageAspect.ASPECT_ID) == false)
      {
        optionalMIATypes.Add(ImageAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemImageAspect.ASPECT_ID);
      }
      optionalMIATypes.Add(DirectoryAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);
      optionalMIATypes.Add(SeasonAspect.ASPECT_ID);

      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter);
      searchQuery.Offset = startingIndex;
      searchQuery.Limit = requestedCount;

      Logger.Debug("MediaServer - OnSearch query {0}", searchQuery);
      IList<MediaItem> items = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, true);

      var msgBuilder = new GenericDidlMessageBuilder();
      var o = deviceClient.RootContainer.FindObject(objectId);
      if (o == null)
      {
        // We failed to find the container requested
        // throw error!
        throw new ArgumentException("ObjectID not found");
      }
      IEnumerable<IDirectoryObject> objects = items.Select(item => MediaLibraryHelper.InstansiateMediaLibraryObject(item, (BasicContainer)o));
      msgBuilder.BuildAll(filter, objects);

      numberReturned = items.Count;
      totalMatches = items.Count;

      var xml = msgBuilder.ToString();
      outParams = new List<object>(4) { xml, numberReturned, totalMatches, containterUpdateId };

      Logger.Debug(
          "MediaServer - exit OnSearch(objectId=\"{0}\"...) = (numberReturned=\"{1}\",totalMatches=\"{2}\",containerUpdateId=\"{3}\") {4}",
          objectId, numberReturned, totalMatches, containterUpdateId, xml);

      // This upnp action doesn't have a return type.
      return null;
    }

    private static UPnPError OnX_GetFeatureList(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      //Samsung feature response
      outParams = new List<object> { "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Features xmlns=\"urn:schemas-upnp-org:av:avs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\" urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd\"><Feature name=\"samsung.com_BASICVIEW\" version=\"1\"><container id=\"I\" type=\"object.item.imageItem\"/><container id=\"A\" type=\"object.item.audioItem\"/><container id=\"V\" type=\"object.item.videoItem\"/></Feature></Features>" };
      return null;
    }

    private static UPnPError OnX_SetBookmark(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      //Samsung position bookmarks
      var posSecond = Convert.ToInt32(inParams[3]);
      int position = posSecond >= 10 ? posSecond - 10 : 0;

      outParams = new List<object> { 0 };
      return null;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
