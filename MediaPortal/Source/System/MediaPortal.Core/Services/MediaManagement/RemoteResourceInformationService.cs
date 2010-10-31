#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Xml.XPath;
using MediaPortal.Core.Exceptions;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Core.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnP.Infrastructure.CP.SSDP;

namespace MediaPortal.Core.Services.MediaManagement
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
      XPathNavigator deviceElementNav = rootDescriptor.FindFirstDeviceElement(
          UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION) ??
          rootDescriptor.FindFirstDeviceElement(
              UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE_VERSION);
      if (deviceElementNav == null)
        return null;
      string deviceUuid = RootDescriptor.GetDeviceUUID(deviceElementNav);
      try
      {
        connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error connecting to UPnP MP 2 device '{0}'", e, deviceUuid);
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
        ServiceRegistration.Get<ILogger>().Warn("Error connecting to services of UPnP MP 2 device '{0}'", e, deviceUuid);
        _controlPoint.Disconnect(deviceUuid);
        return null;
      }
    }

    protected bool IsDescriptorOfSystem(RootDescriptor rootDescriptor, SystemName system)
    {
      RootEntry rootEntry = rootDescriptor.SSDPRootEntry;
      lock (rootEntry.SyncObj)
        foreach (LinkData link in rootEntry.AllLinks)
        {
          string location = link.DescriptionLocation;
          try
          {
            string hostName = new Uri(location).Host;
            if (system == new SystemName(hostName))
              return true;
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("RemoteResourceInformationService: Error checking host of URI '{0}'", e, location);
          }
        }
      return false;
    }

    /// <summary>
    /// Checks if there is a resource information UPnP service available on the given <paramref name="system"/>.
    /// </summary>
    /// <param name="system">System to check.</param>
    /// <returns>Proxy object to access the resource information service at the given <paramref name="system"/> or
    /// <c>null</c>, if the given <paramref name="system"/> neither provides an MP2 client nor an MP2 server.</returns>
    protected IResourceInformationService FindResourceInformationService(SystemName system)
    {
      lock (_networkTracker.SharedControlPointData.SyncObj)
        foreach (RootDescriptor rootDeviceDescriptor in _networkTracker.KnownRootDevices.Values)
          if (IsDescriptorOfSystem(rootDeviceDescriptor, system))
          {
            IResourceInformationService ris = TryGetResourceInformationService(rootDeviceDescriptor);
            if (ris != null)
              return ris;
          }
      return null;
    }

    protected IResourceInformationService GetResourceInformationService(SystemName system)
    {
      IResourceInformationService result = FindResourceInformationService(system);
      if (result == null)
        throw new NotConnectedException("The system '{0}' is not connected", system);
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

    public bool GetResourceInformation(SystemName nativeSystem, ResourcePath nativeResourcePath,
        out bool isFileSystemResource, out bool isFile, out string resourcePathName, out string resourceName,
        out DateTime lastChanged, out long size)
    {
      return GetResourceInformationService(nativeSystem).GetResourceInformation(nativeResourcePath, out isFileSystemResource,
          out isFile, out resourcePathName, out resourceName, out lastChanged, out size);
    }

    public bool ResourceExists(SystemName nativeSystem, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystem).DoesResourceExist(nativeResourcePath);
    }

    public ResourcePath ConcatenatePaths(SystemName nativeSystem, ResourcePath nativeResourcePath, string relativePath)
    {
      return GetResourceInformationService(nativeSystem).ConcatenatePaths(nativeResourcePath, relativePath);
    }

    public ICollection<ResourcePathMetadata> GetFiles(SystemName nativeSystem, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystem).GetFilesData(nativeResourcePath);
    }

    public ICollection<ResourcePathMetadata> GetChildDirectories(SystemName nativeSystem, ResourcePath nativeResourcePath)
    {
      return GetResourceInformationService(nativeSystem).GetChildDirectoriesData(nativeResourcePath);
    }

    public string GetFileHttpUrl(SystemName nativeSystem, ResourcePath nativeResourcePath)
    {
      return ResourceHttpAccessUrlUtils.GetResourceURL(GetResourceInformationService(nativeSystem).GetResourceServerBaseURL(), nativeResourcePath);
    }
  }
}