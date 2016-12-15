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
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.Description;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Services.ResourceAccess
{
  public class RemoteResourceInformationService : IRemoteResourceInformationService
  {
    #region Consts

    protected const string KEY_RESOURCE_INFORMATION_SERVICE = "RemoteResourceInformationService: ResourceInformationService";

    #endregion

    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;

    #endregion

    public RemoteResourceInformationService()
    {
      _networkTracker = new UPnPNetworkTracker(new CPData());
      _controlPoint = new UPnPControlPoint(_networkTracker);
    }

    protected IResourceInformationService TryGetResourceInformationService(RootDescriptor rootDescriptor)
    {
      DeviceConnection connection;
      lock (_networkTracker.SharedControlPointData.SyncObj)
      {
        object service;
        if (rootDescriptor.SSDPRootEntry.ClientProperties.TryGetValue(KEY_RESOURCE_INFORMATION_SERVICE, out service))
          return service as IResourceInformationService;
      }
      DeviceDescriptor rootDevice = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);
      DeviceDescriptor frontendServerDevice = rootDevice.FindFirstDevice(
          UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION) ??
              rootDevice.FindFirstDevice(UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE_VERSION);
      if (frontendServerDevice == null)
        return null;
      string deviceUuid = frontendServerDevice.DeviceUUID;
      try
      {
        connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error connecting to UPnP MP2 device '{0}'", e, deviceUuid);
        return null;
      }
      try
      {
        CpService rasStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.RESOURCE_INFORMATION_SERVICE_ID);
        if (rasStub == null)
          throw new InvalidDataException("ResourceAccess service not found in device '{0}'", deviceUuid);
        IResourceInformationService ris = new UPnPResourceInformationServiceProxy(rasStub);
        lock (_networkTracker.SharedControlPointData.SyncObj)
          rootDescriptor.SSDPRootEntry.ClientProperties[KEY_RESOURCE_INFORMATION_SERVICE] = ris;
        return ris;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error connecting to services of UPnP MP2 device '{0}'", e, deviceUuid);
        _controlPoint.Disconnect(deviceUuid);
        return null;
      }
    }

    /// <summary>
    /// Checks if there is a resource information UPnP service available on the system with the
    /// given <paramref name="systemId"/>.
    /// </summary>
    /// <param name="systemId">Id of the system to check.</param>
    /// <returns>Proxy object to access the resource information service at the system with the given
    /// <paramref name="systemId"/> or <c>null</c>, if the system neither provides an MP2 client nor an MP2 server.</returns>
    protected IResourceInformationService FindResourceInformationService(string systemId)
    {
      lock (_networkTracker.SharedControlPointData.SyncObj)
        foreach (RootDescriptor rootDeviceDescriptor in _networkTracker.KnownRootDevices.Values)
          if (rootDeviceDescriptor.State == RootDescriptorState.Ready && rootDeviceDescriptor.SSDPRootEntry.RootDeviceUUID == systemId)
          {
            IResourceInformationService ris = TryGetResourceInformationService(rootDeviceDescriptor);
            if (ris != null)
              return ris;
          }
      return null;
    }

    protected IResourceInformationService GetResourceInformationService(string systemId)
    {
      IResourceInformationService result = FindResourceInformationService(systemId);
      if (result == null)
        throw new NotConnectedException("The system '{0}' is not connected", systemId);
      return result;
    }

    public void Startup()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts. See docs of Start() method.
      _networkTracker.Start();
    }

    public void Shutdown()
    {
      _networkTracker.Close();
      _controlPoint.Close(); // Close the control point after the network tracker was closed. See docs of Close() method.
    }

    public bool GetResourceInformation(string nativeSystemId, ResourcePath nativeResourcePath, out bool isFileSystemResource, out bool isFile, out string resourcePathName, out string resourceName, out DateTime lastChanged, out long size)
    {
      return GetResourceInformationService(nativeSystemId).GetResourceInformation(nativeResourcePath, out isFileSystemResource,
          out isFile, out resourcePathName, out resourceName, out lastChanged, out size);
    }

    public bool ResourceExists(string nativeSystemId, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystemId).DoesResourceExist(nativeResourcePath);
    }

    public ResourcePath ConcatenatePaths(string nativeSystemId, ResourcePath nativeResourcePath, string relativePath)
    {
      return GetResourceInformationService(nativeSystemId).ConcatenatePaths(nativeResourcePath, relativePath);
    }

    public ICollection<ResourcePathMetadata> GetFiles(string nativeSystemId, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystemId).GetFilesData(nativeResourcePath);
    }

    public ICollection<ResourcePathMetadata> GetChildDirectories(string nativeSystemId, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystemId).GetChildDirectoriesData(nativeResourcePath);
    }

    public bool GetFileHttpUrl(string nativeSystemId, ResourcePath nativeResourcePath, out string fileHttpUrl, out IPAddress localIpAddress)
    {
      fileHttpUrl = null;
      localIpAddress = null;
      IResourceInformationService ris = FindResourceInformationService(nativeSystemId);
      if (ris == null)
        return false;
      fileHttpUrl = ResourceHttpAccessUrlUtils.GetResourceURL(ris.GetResourceServerBaseURL(out localIpAddress), nativeResourcePath);
      return true;
    }
  }
}