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
using System.Net.Sockets;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// Provides the UPnP service for providing low-level file meta information to local media files.
  /// </summary>
  public class UPnPResourceInformationServiceImpl : DvService
  {
    public UPnPResourceInformationServiceImpl() : base(
        UPnPTypesAndIds.RESOURCE_INFORMATION_SERVICE_TYPE, UPnPTypesAndIds.RESOURCE_INFORMATION_SERVICE_TYPE_VERSION,
        UPnPTypesAndIds.RESOURCE_INFORMATION_SERVICE_ID)
    {
      // Used for any single GUID value
      DvStateVariable A_ARG_TYPE_Uuid = new DvStateVariable("A_ARG_TYPE_Id", new DvStandardDataType(UPnPStandardDataType.Uuid))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_Uuid);

      // Simple boolean value
      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_Bool);

      // Simple DateTime value (with time)
      DvStateVariable A_ARG_TYPE_DateTime = new DvStateVariable("A_ARG_TYPE_DateTime", new DvStandardDataType(UPnPStandardDataType.DateTime))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_DateTime);

      DvStateVariable A_ARG_TYPE_FileSize = new DvStateVariable("A_ARG_TYPE_FileSize", new DvStandardDataType(UPnPStandardDataType.I8))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_FileSize);

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

      // Used to transport a short resource path string which can be evaluated by a resource provider
      DvStateVariable A_ARG_TYPE_ResourcePathString = new DvStateVariable("A_ARG_TYPE_ResourcePathString", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_ResourcePathString);

      DvStateVariable A_ARG_TYPE_URL_String = new DvStateVariable("A_ARG_TYPE_URL_String", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false,
        };
      AddStateVariable(A_ARG_TYPE_URL_String);

      // CSV of media category strings
      DvStateVariable A_ARG_TYPE_MediaCategoryEnumeration = new DvStateVariable("A_ARG_TYPE_MediaCategoryEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtMediaCategoryEnumeration))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_MediaCategoryEnumeration);

      // Used to transport a resource provider metadata structure
      DvStateVariable A_ARG_TYPE_ResourceProviderMetadata = new DvStateVariable("A_ARG_TYPE_ResourceProviderMetadata", new DvExtendedDataType(UPnPExtendedDataTypes.DtResourceProviderMetadata))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_ResourceProviderMetadata);

      // Used to transport an enumeration of resource provider metadata structures
      DvStateVariable A_ARG_TYPE_ResourceProviderMetadataEnumeration = new DvStateVariable("A_ARG_TYPE_ResourceProviderMetadataEnumeration", new DvExtendedDataType(UPnPExtendedDataTypes.DtResourceProviderMetadataEnumeration))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_ResourceProviderMetadataEnumeration);

      // Used to transport a display name for a resource or a resource path
      DvStateVariable A_ARG_TYPE_ResourceDisplayName = new DvStateVariable("A_ARG_TYPE_ResourceDisplayName", new DvStandardDataType(UPnPStandardDataType.String))
        {
            SendEvents = false
        };
      AddStateVariable(A_ARG_TYPE_ResourceDisplayName);

      // More state variables go here

      DvAction getMediaCategoriesFromMetadataExtractorsAction = new DvAction("GetMediaCategoriesFromMetadataExtractors", OnGetMediaCategoriesFromMetadataExtractors,
          new DvArgument[] {
          },
          new DvArgument[] {
              new DvArgument("MediaCategories", A_ARG_TYPE_MediaCategoryEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(getMediaCategoriesFromMetadataExtractorsAction);

      DvAction getAllBaseResourceProviderMetadataAction = new DvAction("GetAllBaseResourceProviderMetadata", OnGetAllBaseResourceProviderMetadata,
          new DvArgument[] {
          },
          new DvArgument[] {
              new DvArgument("ResourceProviderMetadata", A_ARG_TYPE_ResourceProviderMetadataEnumeration, ArgumentDirection.Out, true),
          });
      AddAction(getAllBaseResourceProviderMetadataAction);

      DvAction getResourceProviderMetadataAction = new DvAction("GetResourceProviderMetadata", OnGetResourceProviderMetadata,
          new DvArgument[] {
              new DvArgument("ResourceProviderId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourceProviderMetadata", A_ARG_TYPE_ResourceProviderMetadata, ArgumentDirection.Out, true),
          });
      AddAction(getResourceProviderMetadataAction);

      DvAction getResourcePathDisplayNameAction = new DvAction("GetResourcePathDisplayName", OnGetResourcePathDisplayName,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourcePathDisplayName", A_ARG_TYPE_ResourceDisplayName, ArgumentDirection.Out, true),
          });
      AddAction(getResourcePathDisplayNameAction);

      DvAction getResourceDisplayNameAction = new DvAction("GetResourceDisplayName", OnGetResourceDisplayName,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourcePathDisplayName", A_ARG_TYPE_ResourceDisplayName, ArgumentDirection.Out, true),
          });
      AddAction(getResourceDisplayNameAction);

      DvAction getChildDirectoriesDataAction = new DvAction("GetChildDirectoriesData", OnGetChildDirectoriesData,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ChildDirectoriesData", A_ARG_TYPE_ResourcePaths, ArgumentDirection.Out, true),
          });
      AddAction(getChildDirectoriesDataAction);

      DvAction getFilesDataAction = new DvAction("GetFilesData", OnGetFilesData,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("FilesData", A_ARG_TYPE_ResourcePaths, ArgumentDirection.Out, true),
          });
      AddAction(getFilesDataAction);

      DvAction doesResourceExistAction = new DvAction("DoesResourceExist", OnDoesResourceExist,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourceExists", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
          });
      AddAction(doesResourceExistAction);

      DvAction getResourceInformationAction = new DvAction("GetResourceInformation", OnGetResourceInformation,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("IsFileSystemResource", A_ARG_TYPE_Bool, ArgumentDirection.Out, false),
              new DvArgument("IsFile", A_ARG_TYPE_Bool, ArgumentDirection.Out, false),
              new DvArgument("ResourcePathDisplayName", A_ARG_TYPE_ResourcePathString, ArgumentDirection.Out, false),
              new DvArgument("ResourceDisplayName", A_ARG_TYPE_ResourcePathString, ArgumentDirection.Out, false),
              new DvArgument("LastChanged", A_ARG_TYPE_DateTime, ArgumentDirection.Out, false),
              new DvArgument("Size", A_ARG_TYPE_FileSize, ArgumentDirection.Out, false),
              new DvArgument("ResourceExists", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
          });
      AddAction(getResourceInformationAction);

      DvAction doesResourceProviderSupportTreeListingAction = new DvAction("DoesResourceProviderSupportTreeListing", OnDoesResourceProviderSupportTreeListing,
          new DvArgument[] {
              new DvArgument("ResourceProviderId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("SupportsTreeListing", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
          });
      AddAction(doesResourceProviderSupportTreeListingAction);

      DvAction expandResourcePathFromStringAction = new DvAction("ExpandResourcePathFromString", OnExpandResourcePathFromString,
          new DvArgument[] {
              new DvArgument("ResourceProviderId", A_ARG_TYPE_Uuid, ArgumentDirection.In),
              new DvArgument("ResourcePathStr", A_ARG_TYPE_ResourcePathString, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.Out, true),
          });
      AddAction(expandResourcePathFromStringAction);

      DvAction concatenatePathsAction = new DvAction("ConcatenatePaths", OnConcatenatePaths,
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.In),
              new DvArgument("RelativePath", A_ARG_TYPE_ResourcePathString, ArgumentDirection.In),
          },
          new DvArgument[] {
              new DvArgument("ResourcePath", A_ARG_TYPE_ResourcePath, ArgumentDirection.Out, true),
          });
      AddAction(concatenatePathsAction);

      DvAction getResourceServerBaseURLAction = new DvAction("GetResourceServerBaseURL", OnGetResourceServerBaseURL,
          new DvArgument[] {
          },
          new DvArgument[] {
              new DvArgument("BaseURL", A_ARG_TYPE_URL_String, ArgumentDirection.Out, true),
          });
      AddAction(getResourceServerBaseURLAction);

      // More actions go here
    }

    protected static bool IsAllowedToAccess(ResourcePath resourcePath)
    {
      // TODO: How to check safety? We don't have access to our shares store here... See also method IsAllowedToAccess
      // in ResourceAccessModule
      return true;
    }

    static UPnPError OnGetMediaCategoriesFromMetadataExtractors(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      outParams = new List<object> {mediaAccessor.MediaCategories.Values};
      return null;
    }

    static UPnPError OnGetAllBaseResourceProviderMetadata(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IEnumerable<ResourceProviderMetadata> metadata = mediaAccessor.LocalBaseResourceProviders.Select(resourceProvider => resourceProvider.Metadata);
      outParams = new List<object> {metadata};
      return null;
    }

    static UPnPError OnGetResourceProviderMetadata(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid resourceProviderId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IResourceProvider rp;
      outParams = null;
      if (!mediaAccessor.LocalResourceProviders.TryGetValue(resourceProviderId, out rp))
        return new UPnPError(600, string.Format("No resource provider of id '{0}' present in system", resourceProviderId));
      outParams = new List<object> {rp.Metadata};
      return null;
    }

    static UPnPError OnGetResourcePathDisplayName(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (path == null)
        return new UPnPError(600, "Invalid resource path");
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      IResourceAccessor ra;
      if (!path.TryCreateLocalResourceAccessor(out ra))
        return new UPnPError(600, "The given path is not accessible");
      using (ra)
        outParams = new List<object> {ra.ResourcePathName};
      return null;
    }

    static UPnPError OnGetResourceDisplayName(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (path == null)
        return new UPnPError(600, "Invalid resource path");
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      IResourceAccessor ra;
      if (!path.TryCreateLocalResourceAccessor(out ra))
        return new UPnPError(600, "The given path is not accessible");
      using (ra)
        outParams = new List<object> {ra.ResourceName};
      return null;
    }

    static UPnPError OnGetChildDirectoriesData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (path == null)
        return new UPnPError(600, "Invalid resource path");
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      IResourceAccessor ra;
      if (!path.TryCreateLocalResourceAccessor(out ra))
        return new UPnPError(600, "The given path is not accessible");
      using (ra)
      {
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          return new UPnPError(600, "The given path is not a file system resource");
        ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetChildDirectories(fsra, false);
        IList<ResourcePathMetadata> result = null;
        if (res != null)
        {
          result = new List<ResourcePathMetadata>();
          foreach (IFileSystemResourceAccessor childAccessor in res)
            using (childAccessor)
              result.Add(new ResourcePathMetadata
                {
                    ResourceName = childAccessor.ResourceName,
                    HumanReadablePath = childAccessor.ResourcePathName,
                    ResourcePath = childAccessor.CanonicalLocalResourcePath
                });
        }
        outParams = new List<object> {result};
      }
      return null;
    }

    static UPnPError OnGetFilesData(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (path == null)
        return new UPnPError(600, "Invalid resource path");
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      IResourceAccessor ra;
      if (!path.TryCreateLocalResourceAccessor(out ra))
        return new UPnPError(600, "The given path is not accessible");
      using (ra)
      {
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          return new UPnPError(600, "The given path is not a file system resource");
        ICollection<IFileSystemResourceAccessor> res = FileSystemResourceNavigator.GetFiles(fsra, false);
        IList<ResourcePathMetadata> result = null;
        if (res != null)
        {
          result = new List<ResourcePathMetadata>();
          foreach (IFileSystemResourceAccessor fileAccessor in res)
            using (fileAccessor)
              result.Add(new ResourcePathMetadata
                {
                    ResourceName = fileAccessor.ResourceName,
                    HumanReadablePath = fileAccessor.ResourcePathName,
                    ResourcePath = fileAccessor.CanonicalLocalResourcePath
                });
        }
        outParams = new List<object> {result};
      }
      return null;
    }

    static UPnPError OnDoesResourceExist(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      IResourceAccessor ra;
      bool result = path.TryCreateLocalResourceAccessor(out ra);
      if (result)
        ra.Dispose();
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnGetResourceInformation(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      if (!IsAllowedToAccess(path))
        return new UPnPError(600, "Access is not allowed to this resource path");
      bool isFileSystemResource = false;
      bool isFile = false;
      string resourcePathDisplayName = string.Empty;
      string resourceDisplayName = string.Empty;
      DateTime lastChanged = DateTime.MinValue;
      Int64 size = 0;
      IResourceAccessor ra;
      bool result = path.TryCreateLocalResourceAccessor(out ra);
      if (result)
        using (ra)
        {
          IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
          isFileSystemResource = fsra != null;
          isFile = fsra.IsFile;
          resourcePathDisplayName = ra.ResourcePathName;
          resourceDisplayName = ra.ResourceName;
          lastChanged = fsra.LastChanged;
          size = fsra.IsFile ? fsra.Size : -1;
        }
      outParams = new List<object> {isFileSystemResource, isFile, resourcePathDisplayName, resourceDisplayName, lastChanged, size, result};
      return null;
    }

    static UPnPError OnDoesResourceProviderSupportTreeListing(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid resourceProviderId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      IResourceProvider rp;
      bool result = false;
      if (mediaAccessor.LocalResourceProviders.TryGetValue(resourceProviderId, out rp) && rp is IBaseResourceProvider)
      {
        IResourceAccessor rootAccessor;
        if (((IBaseResourceProvider) rp).TryCreateResourceAccessor("/", out rootAccessor))
        {
          if (rootAccessor is IFileSystemResourceAccessor)
            result = true;
          rootAccessor.Dispose();
        }
      }
      outParams = new List<object> {result};
      return null;
    }

    static UPnPError OnExpandResourcePathFromString(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      Guid resourceProviderId = MarshallingHelper.DeserializeGuid((string) inParams[0]);
      string pathStr = (string) inParams[1];
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      ResourcePath result = null;
      IResourceProvider rp;
      if (mediaAccessor.LocalResourceProviders.TryGetValue(resourceProviderId, out rp) && rp is IBaseResourceProvider)
        result = ((IBaseResourceProvider) rp).ExpandResourcePathFromString(pathStr);
      outParams = new List<object> {result == null ? null : result.Serialize()};
      return null;
    }

    static UPnPError OnConcatenatePaths(DvAction action, IList<object> inParams, out IList<object> outParams,
        CallContext context)
    {
      outParams = null;
      ResourcePath path = ResourcePath.Deserialize((string) inParams[0]);
      string relativePathStr = (string) inParams[1];
      IResourceAccessor ra;
      if (!path.TryCreateLocalResourceAccessor(out ra))
        return new UPnPError(600, "The given path is not accessible");
      string serializedPath = null;
      using (ra)
      {
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra != null)
        {
          IFileSystemResourceAccessor ra2 = fsra.GetResource(relativePathStr);
          if (ra2 != null)
            using (ra2)
              serializedPath = ra2.CanonicalLocalResourcePath.Serialize();
        }
        else
          return new UPnPError(600, "The given paths cannot be concatenated");
      }
      outParams = new List<object> {serializedPath};
      return null;
    }

    static UPnPError OnGetResourceServerBaseURL(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      IResourceServer resourceServer = ServiceRegistration.Get<IResourceServer>();
      string baseURL = "http://" + NetworkHelper.IPEndPointToString(context.Endpoint.EndPointIPAddress, resourceServer.GetPortForIP(context.Endpoint.EndPointIPAddress)) +
        ResourceHttpAccessUrlUtils.RESOURCE_ACCESS_PATH;
      outParams = new List<object> {baseURL};
      return null;
    }
  }
}
