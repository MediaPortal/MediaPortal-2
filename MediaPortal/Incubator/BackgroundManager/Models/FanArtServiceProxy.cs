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
using MediaPortal.Common;
using MediaPortal.Common.UPnP;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces.UPnP;
using MediaPortal.Extensions.UserServices.FanArtService.UPnP;
using MediaPortal.UI.ServerCommunication;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;
using UPnPExtendedDataTypes = MediaPortal.Common.UPnP.UPnPExtendedDataTypes;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class FanArtServiceProxy : UPnPServiceProxyBase, IFanArtService
  {
    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected static List<FanArtImage> EMPTY_LIST = new List<FanArtImage>();

    #endregion

    public FanArtServiceProxy()
    {
      UPnPExtendedDataTypes.AddDataType(UPnPDtImageCollection.Instance);
    }

    protected bool TryInit()
    {
      if (_serviceStub != null)
        return true;

      DeviceConnection connection = ServiceRegistration.Get<IServerConnectionManager>().ControlPoint.Connection;
      if (connection == null)
        return false;

      CpService fanArtStub = connection.Device.FindServiceByServiceId(Consts.FANART_SERVICE_ID);
      //if (fanArtStub == null)
      //  throw new InvalidDataException("ContentDirectory service not found in device '{0}' of type '{1}:{2}'",
      //      connection.Device.UUID, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION);
      if (fanArtStub != null)
        Init(fanArtStub, "FanArt");

      return _serviceStub != null;
    }

    public IList<FanArtImage> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, bool singleRandom)
    {
      try
      {
        if (!TryInit())
          return null; // allow to retry

        CpAction action = GetAction("GetFanArt");
        IList<object> inParameters = new List<object>
                                     {
                                       mediaType.ToString(),
                                       fanArtType.ToString(),
                                       name,
                                       singleRandom
                                     };
        IList<object> outParameters = action.InvokeAction(inParameters);
        return (IList<FanArtImage>)outParameters[0];
      }
      catch (Exception)
      {
        return EMPTY_LIST;
      }
    }
  }
}
