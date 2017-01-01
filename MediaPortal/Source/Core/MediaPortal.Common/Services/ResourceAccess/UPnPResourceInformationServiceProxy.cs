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
using System.Net;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP client's proxy for the ContentDirectory service.
  /// </summary>
  public class UPnPResourceInformationServiceProxy : UPnPServiceProxyBase, IResourceInformationService
  {
    public UPnPResourceInformationServiceProxy(CpService serviceStub) : base(serviceStub, "ResourceInformation") { }

    // We could also provide the asynchronous counterparts of the following methods... do we need them?

    public ICollection<MediaCategory> GetMediaCategoriesFromMetadataExtractors()
    {
      CpAction action = GetAction("GetMediaCategoriesFromMetadataExtractors");
      IList<object> outParameters = action.InvokeAction(null);
      return new List<MediaCategory>((IEnumerable<MediaCategory>) outParameters[0]);
    }

    public ICollection<ResourceProviderMetadata> GetAllBaseResourceProviderMetadata()
    {
      CpAction action = GetAction("GetAllBaseResourceProviderMetadata");
      IList<object> outParameters = action.InvokeAction(null);
      return (ICollection<ResourceProviderMetadata>) outParameters[0];
    }

    public ResourceProviderMetadata GetResourceProviderMetadata(Guid resourceProviderId)
    {
      CpAction action = GetAction("GetResourceProviderMetadata");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(resourceProviderId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ResourceProviderMetadata) outParameters[0];
    }

    public string GetResourcePathDisplayName(ResourcePath path)
    {
      CpAction action = GetAction("GetResourcePathDisplayName");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (string) outParameters[0];
    }

    public string GetResourceDisplayName(ResourcePath path)
    {
      CpAction action = GetAction("GetResourceDisplayName");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (string) outParameters[0];
    }

    public ICollection<ResourcePathMetadata> GetChildDirectoriesData(ResourcePath path)
    {
      CpAction action = GetAction("GetChildDirectoriesData");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<ResourcePathMetadata>) outParameters[0];
    }

    public ICollection<ResourcePathMetadata> GetFilesData(ResourcePath path)
    {
      CpAction action = GetAction("GetFilesData");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (ICollection<ResourcePathMetadata>) outParameters[0];
    }

    public bool DoesResourceExist(ResourcePath path)
    {
      CpAction action = GetAction("DoesResourceExist");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    public bool GetResourceInformation(ResourcePath path, out bool isFileSystemResource,
        out bool isFile, out string resourcePathName, out string resourceName,
        out DateTime lastChanged, out long size)
    {
      CpAction action = GetAction("GetResourceInformation");
      IList<object> inParameters = new List<object> {path.Serialize()};
      IList<object> outParameters = action.InvokeAction(inParameters);
      isFileSystemResource = (bool) outParameters[0];
      isFile = (bool) outParameters[1];
      resourcePathName = (string) outParameters[2];
      resourceName = (string) outParameters[3];
      lastChanged = (DateTime) outParameters[4];
      size = (Int64) outParameters[5];
      return (bool) outParameters[6];
    }

    public bool DoesResourceProviderSupportTreeListing(Guid resourceProviderId)
    {
      CpAction action = GetAction("DoesResourceProviderSupportTreeListing");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(resourceProviderId)};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return (bool) outParameters[0];
    }

    public ResourcePath ExpandResourcePathFromString(Guid resourceProviderId, string path)
    {
      CpAction action = GetAction("ExpandResourcePathFromString");
      IList<object> inParameters = new List<object> {MarshallingHelper.SerializeGuid(resourceProviderId), path};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return ResourcePath.Deserialize((string) outParameters[0]);
    }

    public ResourcePath ConcatenatePaths(ResourcePath basePath, string relativePath)
    {
      CpAction action = GetAction("ConcatenatePaths");
      IList<object> inParameters = new List<object> {basePath.Serialize(), relativePath};
      IList<object> outParameters = action.InvokeAction(inParameters);
      return ResourcePath.Deserialize((string) outParameters[0]);
    }

    public string GetResourceServerBaseURL(out IPAddress localIpAddress)
    {
      CpAction action = GetAction("GetResourceServerBaseURL");
      IList<object> outParameters = action.InvokeAction(null);
      DeviceConnection connection = _serviceStub.Connection;
      localIpAddress = connection == null ? null : connection.RootDescriptor.SSDPRootEntry.PreferredLink.Endpoint.EndPointIPAddress;
      return (string) outParameters[0];
    }

    // TODO: State variables, if present
  }
}