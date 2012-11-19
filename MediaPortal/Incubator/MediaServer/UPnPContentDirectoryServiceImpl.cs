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
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.MediaServer.DIDL;
//using MediaPortal.Extensions.MediaServer.Parser;
using MediaPortal.Utilities.UPnP;
//using Peg.Base;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Extensions.MediaServer
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
      DvStateVariable A_ARG_TYPE_BrowseFlag = new DvStateVariable("A_ARG_TYPE_BrowseFlag",
                                                                  new DvStandardDataType(
                                                                    UPnPStandardDataType.String))
                                                {
                                                  SendEvents = false,
                                                  AllowedValueList =
                                                    new List<string>() {"BrowseMetadata", "BrowseDirectChildren"}
                                                };
      AddStateVariable(A_ARG_TYPE_BrowseFlag);

      DvStateVariable A_ARG_TYPE_Count = new DvStateVariable("A_ARG_TYPE_Count",
                                                             new DvStandardDataType(UPnPStandardDataType.Ui4))
                                           {
                                             SendEvents = false
                                           };
      AddStateVariable(A_ARG_TYPE_Count);

      DvStateVariable A_ARG_TYPE_Filter = new DvStateVariable("A_ARG_TYPE_Filter",
                                                              new DvStandardDataType(UPnPStandardDataType.String))
                                            {
                                              SendEvents = false
                                            };
      AddStateVariable(A_ARG_TYPE_Filter);

      DvStateVariable A_ARG_TYPE_Index = new DvStateVariable("A_ARG_TYPE_Index",
                                                             new DvStandardDataType(UPnPStandardDataType.Ui4))
                                           {
                                             SendEvents = false
                                           };
      AddStateVariable(A_ARG_TYPE_Index);

      DvStateVariable A_ARG_TYPE_ObjectID = new DvStateVariable("A_ARG_TYPE_ObjectID",
                                                                new DvStandardDataType(UPnPStandardDataType.String))
                                              {
                                                SendEvents = false
                                              };
      AddStateVariable(A_ARG_TYPE_ObjectID);

      DvStateVariable A_ARG_TYPE_Result = new DvStateVariable("A_ARG_TYPE_Result",
                                                              new DvStandardDataType(UPnPStandardDataType.String))
                                            {
                                              SendEvents = false
                                            };
      AddStateVariable(A_ARG_TYPE_Result);

      DvStateVariable A_ARG_TYPE_SearchCriteria = new DvStateVariable("A_ARG_TYPE_SearchCriteria",
                                                                      new DvStandardDataType(
                                                                        UPnPStandardDataType.String))
                                                    {
                                                      SendEvents = false
                                                    };
      AddStateVariable(A_ARG_TYPE_SearchCriteria);

      DvStateVariable A_ARG_TYPE_SortCriteria = new DvStateVariable("A_ARG_TYPE_SortCriteria",
                                                                    new DvStandardDataType(
                                                                      UPnPStandardDataType.String))
                                                  {
                                                    SendEvents = false
                                                  };
      AddStateVariable(A_ARG_TYPE_SortCriteria);

      DvStateVariable A_ARG_TYPE_UpdateID = new DvStateVariable("A_ARG_TYPE_UpdateID",
                                                                new DvStandardDataType(UPnPStandardDataType.Ui4))
                                              {
                                                SendEvents = false
                                              };
      AddStateVariable(A_ARG_TYPE_UpdateID);

      DvStateVariable SearchCapabilities = new DvStateVariable("SearchCapabilities",
                                                               new DvStandardDataType(UPnPStandardDataType.String))
                                             {
                                               SendEvents = false
                                             };
      AddStateVariable(SearchCapabilities);

      DvStateVariable SortCapabilities = new DvStateVariable("SortCapabilities",
                                                             new DvStandardDataType(UPnPStandardDataType.String))
                                           {
                                             SendEvents = false
                                           };
      AddStateVariable(SortCapabilities);

      DvStateVariable SystemUpdateID = new DvStateVariable("SystemUpdateID",
                                                           new DvStandardDataType(UPnPStandardDataType.Ui4))
                                         {
                                           SendEvents = true
                                         };
      AddStateVariable(SystemUpdateID);

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
    }

    private static UPnPError OnBrowse(DvAction action, IList<object> inParams, out IList<object> outParams,
                                      CallContext context)
    {
      // In parameters
      var objectId = (string) inParams[0];
      var browseFlag = inParams[1].ToString();
      var filter = inParams[2].ToString();
      var startingIndex = Convert.ToInt32(inParams[3]);
      var requestedCount = Convert.ToInt32(inParams[4]);
      var sortCriteria = (string) inParams[5];

      // Out parameters
      int numberReturned = 0;
      int totalMatches = 0;
      int containterUpdateId;

      Logger.Debug(
        "MediaServer - OnBrowse(objectId=\"{0}\",browseFlag=\"{1}\",filter=\"{2}\",startingIndex=\"{3}\",requestedCount=\"{4}\",sortCriteria=\"{5}\")",
        objectId, browseFlag, filter, startingIndex, requestedCount, sortCriteria);

      // Find the container object requested
      //var parentDirectoryId = objectId == "0" ? Guid.Empty : MarshallingHelper.DeserializeGuid(objectId);
      var o = MediaServerPlugin.RootContainer.FindObject(objectId);

      if (o == null)
      {
        // We failed to find the container requested
        // throw error!
        throw new ArgumentException("ObjectID not found");
      }

      var msgBuilder = new GenericDidlMessageBuilder();


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
          // Create a new ContainerList based on search criteria
          var resultList = o.Search(filter, sortCriteria);
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
      outParams = new List<object>(4) {xml, numberReturned, totalMatches, containterUpdateId};

      Logger.Debug(
        "MediaServer - OnBrowse(objectId=\"{0}\"...) = (numberReturned=\"{1}\",totalMatches=\"{2}\",containerUpdateId=\"{3}\") {4}",
        objectId, numberReturned, totalMatches, containterUpdateId, xml);

      // This upnp action doesn't have a return type.
      return null;
    }

    private static UPnPError OnGetSearchCapabilities(DvAction action, IList<object> inParams,
                                                     out IList<object> outParams, CallContext context)
    {
      // Current implementation doesn't support searching
      outParams = new List<object> {""};
      return null;
    }

    private static UPnPError OnGetSortCapabilities(DvAction action, IList<object> inParams, out IList<object> outParams,
                                                   CallContext context)
    {
      // Current implementation doesn't support sorting
      outParams = new List<object> {""};
      return null;
    }

    private static UPnPError OnGetSystemUpdateID(DvAction action, IList<object> inParams, out IList<object> outParams,
                                                 CallContext context)
    {
      outParams = new List<object> {0};
      return null;
    }

    private static UPnPError OnSearch(DvAction action, IList<object> inParams, out IList<object> outParams,
                                      CallContext context)
    {
      // In parameters
      var containerId = (string) inParams[0];
      var searchCriteria = inParams[1].ToString();
      var filter = inParams[2].ToString();
      var startingIndex = Convert.ToInt32(inParams[3]);
      var requestedCount = Convert.ToInt32(inParams[4]);
      var sortCriteria = (string) inParams[5];

      // Out parameters
      int numberReturned = 0;
      int totalMatches = 0;
      int containterUpdateId = 0;
      /*
            UPnPContentDirectorySearch query = new UPnPContentDirectorySearch();
            StringWriter sw = new StringWriter();
            query.Construct(searchCriteria, sw);
            query.searchCrit();
            PegNode pn = query.GetRoot();

            string xml = ParserHelper.PegNodeToXml(pn, searchCriteria);
            Logger.Debug("MediaServer - Parsed: \"{0}\" to make \"{1}\"", searchCriteria, xml);
            
            var parentDirectoryId = containerId == "0" ? Guid.Empty : MarshallingHelper.DeserializeGuid(containerId);
            var necessaryMIATypes = new List<Guid> {DirectoryAspect.ASPECT_ID};
            
            var searchQuery = new MediaItemQuery(necessaryMIATypes, null);
            //searchQuery.Filter

            var searchItems = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, true);
            /*
            foreach (var item in browseItems)
            {

            }
            */
      outParams = new List<object>(3) {numberReturned, totalMatches, containterUpdateId};
      return null;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}