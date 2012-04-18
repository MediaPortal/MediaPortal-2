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
using System.Collections.ObjectModel;
using MediaPortal.Common;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Extensions.UserServices.FanArtService.UPnP
{
  public class FanArtServiceImpl : DvService
  {
    public const string FANART_SERVICE_TYPE = "schemas-team-mediaportal-com:service:FanArt";
    public const int FANART_SERVICE_TYPE_VERSION = 1;
    public const string FANART_SERVICE_ID = "urn:team-mediaportal-com:serviceId:FanArt";

    public FanArtServiceImpl()
      : base(FANART_SERVICE_TYPE, FANART_SERVICE_TYPE_VERSION, FANART_SERVICE_ID)
    {
      DvStateVariable mediaType = new DvStateVariable("MediaType", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(mediaType);

      DvStateVariable fanArtType = new DvStateVariable("FanArtType", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(fanArtType);

      DvStateVariable name = new DvStateVariable("Name", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(name);

      DvStateVariable fanArts = new DvStateVariable("FanArts", new DvExtendedDataType(UPnPExtendedDataTypes.DtImageCollection)) { SendEvents = false };
      AddStateVariable(fanArts);

      DvAction getFanArt = new DvAction("GetFanArt", OnGetFanArt,
                                   new[]
                                     {
                                       new DvArgument("MediaType", mediaType, ArgumentDirection.In),
                                       new DvArgument("FanArtType", fanArtType, ArgumentDirection.In),
                                       new DvArgument("Name", name, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("FanArts", fanArts, ArgumentDirection.Out, true)
                                     });
      AddAction(getFanArt);
    }

    private UPnPError OnGetFanArt(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IFanArtService fanArtService = ServiceRegistration.Get<IFanArtService>();
      if (fanArtService == null)
        return new UPnPError(500, "FanArt service not available");
      if (inParams.Count != 3)
        return new UPnPError(500, "Invalid arguments");

      FanArtConstants.FanArtMediaType fanArtMediaType = (FanArtConstants.FanArtMediaType) Enum.Parse(typeof (FanArtConstants.FanArtMediaType), inParams[0].ToString());
      FanArtConstants.FanArtType fanArtType = (FanArtConstants.FanArtType)Enum.Parse(typeof(FanArtConstants.FanArtType), inParams[1].ToString());
      string name = inParams[2].ToString();

      ICollection<FanArtImage> fanArtImages= new Collection<FanArtImage>();
      IList<string> fanArts = fanArtService.GetFanArt(fanArtMediaType, fanArtType, name, true);
      foreach (string fanArt in fanArts)
      {
        FanArtImage fanArtImage = FanArtImage.FromFile(fanArt);
        if (fanArtImage != null)
          fanArtImages.Add(fanArtImage);
      }
      outParams = new List<object> { fanArtImages };
      return null;
    }
  }
}
