#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Services.UPnP
{
  /// <summary>
  /// Encapsulates the MediaPortal-II UPnP client control point.
  /// </summary>
  public class UPnPClientControlPoint
  {
    public const string MEDIA_SERVER_DEVICE_TYPE = "schemas-team-mediaportal-com:device:MP-II-Server";
    public const int MEDIA_SERVER_DEVICE_TYPE_VERSION = 1;
    public const string CONTENT_DIRECTORY_SERVICE_ID = "urn:team-mediaportal-com:serviceId:ContentDirectory";

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected UPnPContentDirectoryService _contentDirectoryService;

    public UPnPClientControlPoint()
    {
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _controlPoint = new UPnPControlPoint(_networkTracker);
      _networkTracker.RootDeviceAdded += OnUPnPRootDeviceAdded;
    }

    public UPnPContentDirectoryService ContentDirectoryService
    {
      get { return _contentDirectoryService; }
    }

    public void Start()
    {
      _controlPoint.Start(); // Start the control point before the network tracker starts, to catch all device appearances
      _networkTracker.Start();
    }

    public void Stop()
    {
      _networkTracker.Close();
      _controlPoint.Close();
    }

    void OnUPnPRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      XmlElement mediaServerDeviceElement = rootDescriptor.FindFirstDeviceElement(MEDIA_SERVER_DEVICE_TYPE, MEDIA_SERVER_DEVICE_TYPE_VERSION);
      if (mediaServerDeviceElement == null)
        return;
      string deviceUuid = RootDescriptor.GetDeviceUUID(mediaServerDeviceElement);
      DeviceConnection connection = _controlPoint.Connect(rootDescriptor, deviceUuid, UPnPExtendedDataTypes.ResolveDataType);
      try
      {
        CpService cdsStub = connection.Device.FindServiceByServiceId(CONTENT_DIRECTORY_SERVICE_ID);
        if (cdsStub == null)
          throw new InvalidDataException("ContentDirectory service not found in device '{0}' of type '{1}:{2}'", deviceUuid, MEDIA_SERVER_DEVICE_TYPE, MEDIA_SERVER_DEVICE_TYPE_VERSION);
        _contentDirectoryService = new UPnPContentDirectoryService(cdsStub);
        // TODO: other services
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("Error attaching to UPnP MP-II content directory service", e);
        _controlPoint.Disconnect(deviceUuid);
      }
    }
  }
}
