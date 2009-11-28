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
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Core.UPnP;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Backend.ClientCommunication
{
  public delegate void ClientDeviceDisconnectedDlgt(ClientConnection clientConnection);

  public class ClientConnection
  {
    protected DeviceConnection _connection;
    protected ClientDescriptor _clientDescriptor;
    protected IClientController _clientController;

    public ClientConnection(DeviceConnection connection, ClientDescriptor clientDescriptor)
    {
      _connection = connection;
      _clientDescriptor = clientDescriptor;
      _connection.DeviceDisconnected += OnUPnPDeviceDisconnected;
      try
      {
        CpService ccsStub = connection.Device.FindServiceByServiceId(UPnPTypesAndIds.CLIENT_CONTROLLER_SERVICE_ID);
        if (ccsStub == null)
          throw new InvalidDataException("ClientController service not found in device '{0}' of type '{1}:{2}'",
              clientDescriptor.MPFrontendServerUUID,
              UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.FRONTEND_SERVER_DEVICE_TYPE_VERSION);
        lock (_connection.CPData.SyncObj)
          _clientController = new UPnPClientControllerServiceProxy(ccsStub);
        // TODO: other services
      }
      catch (Exception)
      {
        _connection.DeviceDisconnected -= OnUPnPDeviceDisconnected;
        throw;
      }
    }

    public event ClientDeviceDisconnectedDlgt ClientDeviceDisconnected;

    public ClientDescriptor Descriptor
    {
      get { return _clientDescriptor; }
    }

    public IClientController ClientController
    {
      get { return _clientController; }
    }

    public DeviceConnection UnderlayingConnection
    {
      get { return _connection; }
    }

    public void Disconnect()
    {
      _connection.Disconnect(true);
    }

    void OnUPnPDeviceDisconnected(DeviceConnection connection)
    {
      _clientController = null;
      InvokeClientDeviceDisconnected(this);
    }

    protected void InvokeClientDeviceDisconnected(ClientConnection connection)
    {
      ClientDeviceDisconnectedDlgt dlgt = ClientDeviceDisconnected;
      if (dlgt != null)
        dlgt(this);
    }
  }
}
