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

using System.Collections.Generic;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.MediaServer
{
    class UPnPMediaReceiverRegistrarServiceImpl : DvService
    {
        public UPnPMediaReceiverRegistrarServiceImpl()
            : base(
                UPnPMediaServerDevice.MEDIARECEIVER_REGISTRAR_SERVICE_TYPE, 
                UPnPMediaServerDevice.MEDIARECEIVER_REGISTRAR_SERVICE_TYPE_VERSION,
                UPnPMediaServerDevice.MEDIARECEIVER_REGISTRAR_SERVICE_ID)
        {
            // Used for a boolean value
            DvStateVariable A_ARG_TYPE_DeviceID = new DvStateVariable("A_ARG_TYPE_DeviceID",
                                                                        new DvStandardDataType(
                                                                            UPnPStandardDataType.String))
            {
                SendEvents = false
            };
            AddStateVariable(A_ARG_TYPE_DeviceID);

            DvStateVariable A_ARG_TYPE_Result = new DvStateVariable("A_ARG_TYPE_Result",
                                                                   new DvStandardDataType(UPnPStandardDataType.Int))
            {
                SendEvents = false
            };
            AddStateVariable(A_ARG_TYPE_Result);

            DvStateVariable A_ARG_TYPE_RegistrationReqMsg = new DvStateVariable("A_ARG_TYPE_RegistrationReqMsg",
                                                                    new DvStandardDataType(UPnPStandardDataType.BinBase64))
            {
                SendEvents = false
            };
            AddStateVariable(A_ARG_TYPE_RegistrationReqMsg);

            DvStateVariable A_ARG_TYPE_RegistrationRespMsg = new DvStateVariable("A_ARG_TYPE_RegistrationRespMsg",
                                                                   new DvStandardDataType(UPnPStandardDataType.BinBase64))
            {
                SendEvents = false
            };
            AddStateVariable(A_ARG_TYPE_RegistrationRespMsg);

            DvStateVariable AuthorizationGrantedUpdateID = new DvStateVariable("AuthorizationGrantedUpdateID",
                                                                      new DvStandardDataType(UPnPStandardDataType.Ui4))
            {
                SendEvents = true
            };
            AddStateVariable(AuthorizationGrantedUpdateID);

            DvStateVariable AuthorizationDeniedUpdateID = new DvStateVariable("AuthorizationDeniedUpdateID",
                                                                    new DvStandardDataType(UPnPStandardDataType.Ui4))
            {
                SendEvents = true
            };
            AddStateVariable(AuthorizationDeniedUpdateID);

            DvStateVariable ValidationSucceededUpdateID = new DvStateVariable("ValidationSucceededUpdateID",
                                                                            new DvStandardDataType(
                                                                                UPnPStandardDataType.Ui4))
            {
                SendEvents = true
            };
            AddStateVariable(ValidationSucceededUpdateID);

            DvStateVariable ValidationRevokedUpdateID = new DvStateVariable("ValidationRevokedUpdateID",
                                                                          new DvStandardDataType(
                                                                              UPnPStandardDataType.Ui4))
            {
                SendEvents = true
            };
            AddStateVariable(ValidationRevokedUpdateID);

            DvAction isAuthorizedAction = new DvAction("IsAuthorized", OnIsAuthorized,
                                                      new DvArgument[]
                                                          {
                                                              new DvArgument("DeviceID", A_ARG_TYPE_DeviceID,
                                                                             ArgumentDirection.In)
                                                          },
                                                      new DvArgument[]
                                                          {
                                                              new DvArgument("Result",
                                                                             A_ARG_TYPE_Result,
                                                                             ArgumentDirection.Out)
                                                          });
            AddAction(isAuthorizedAction);

            DvAction registerDeviceAction = new DvAction("RegisterDevice", OnRegisterDevice,
                                          new DvArgument[]
                                                          {
                                                              new DvArgument("RegistrationReqMsg", A_ARG_TYPE_RegistrationReqMsg,
                                                                             ArgumentDirection.In)
                                                          },
                                          new DvArgument[]
                                                          {
                                                              new DvArgument("RegistrationRespMsg",
                                                                             A_ARG_TYPE_RegistrationRespMsg,
                                                                             ArgumentDirection.Out),
                                                          });
            AddAction(registerDeviceAction);

            DvAction isValidatedAction = new DvAction("IsValidated", OnIsValidated,
                                         new DvArgument[]
                                                          {
                                                              new DvArgument("DeviceID", A_ARG_TYPE_DeviceID,
                                                                             ArgumentDirection.In)
                                                          },
                                                      new DvArgument[]
                                                          {
                                                              new DvArgument("Result",
                                                                             A_ARG_TYPE_Result,
                                                                             ArgumentDirection.Out)
                                                          });
            AddAction(isValidatedAction);
        }

        static UPnPError OnIsAuthorized(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
        {
            // MS authorization response
            outParams = new List<object> { 1 };
            return null;
        }

        static UPnPError OnRegisterDevice(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
        {
            // Register MS device
            outParams = new List<object> { };
            return null;
        }

        static UPnPError OnIsValidated(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
        {
            // MS validation response
            outParams = new List<object> { 1 };
            return null;
        }
    }
}
