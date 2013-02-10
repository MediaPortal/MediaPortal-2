#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  public class FanArtServiceProxy : UPnPServiceProxyBase, IFanArtService, IDisposable
  {
    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected readonly object _syncObj = new object();
    protected static List<FanArtImage> EMPTY_LIST = new List<FanArtImage>();

    #endregion

    public FanArtServiceProxy(CpService serviceStub) : base(serviceStub, "FanArt")
    {
      ServiceRegistration.Set<IFanArtService>(this);
    }

    public IList<FanArtImage> GetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom)
    {
      try
      {
        CpAction action = GetAction("GetFanArt");
        IList<object> inParameters = new List<object>
            {
              mediaType.ToString(),
              fanArtType.ToString(),
              name,
              maxWidth,
              maxHeight,
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

    public void Dispose()
    {
      ServiceRegistration.Remove<IFanArtService>();
    }
  }
}
