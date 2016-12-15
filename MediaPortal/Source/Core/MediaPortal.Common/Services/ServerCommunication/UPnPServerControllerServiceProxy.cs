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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities.UPnP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Common.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP client's proxy for the ServerController service.
  /// </summary>
  public class UPnPServerControllerServiceProxy : UPnPServiceProxyBase, IServerController
  {
    protected const string SV_ATTACHED_CLIENTS_CHANGE_COUNTER = "AttachedClientsChangeCounter";
    protected const string SV_CONNECTED_CLIENTS_CHANGE_COUNTER = "ConnectedClientsChangeCounter";

    public UPnPServerControllerServiceProxy(CpService serviceStub) : base(serviceStub, "ServerController")
    {
      serviceStub.StateVariableChanged += OnStateVariableChanged;
      serviceStub.SubscribeStateVariables();
    }

    private void OnStateVariableChanged(CpStateVariable statevariable, object newValue)
    {
      if (statevariable.Name == SV_ATTACHED_CLIENTS_CHANGE_COUNTER)
        FireAttachedClientsChanged();
      else if (statevariable.Name == SV_CONNECTED_CLIENTS_CHANGE_COUNTER)
        FireConnectedClientsChanged();
    }

    protected void FireAttachedClientsChanged()
    {
      ParameterlessMethod dlgt = AttachedClientsChanged;
      if (dlgt != null)
        dlgt();
    }

    protected void FireConnectedClientsChanged()
    {
      ParameterlessMethod dlgt = ConnectedClientsChanged;
      if (dlgt != null)
        dlgt();
    }

    #region State variables

    // We don't make those events available via the public interface because .net event registrations are not allowed between MP2 modules.
    // It is the job of the class which instanciates this class to publicize those events.

    public event ParameterlessMethod AttachedClientsChanged;
    public event ParameterlessMethod ConnectedClientsChanged;

    #endregion

    public void AttachClient(string clientSystemId)
    {
      CpAction action = GetAction("AttachClient");
      action.InvokeAction(new List<object> {clientSystemId});
    }

    public void DetachClient(string clientSystemId)
    {
      CpAction action = GetAction("DetachClient");
      action.InvokeAction(new List<object> {clientSystemId});
    }

    public ICollection<MPClientMetadata> GetAttachedClients()
    {
      CpAction action = GetAction("GetAttachedClients");
      IList<object> outParams = action.InvokeAction(null);
      return (ICollection<MPClientMetadata>) outParams[0];
    }

    public ICollection<string> GetConnectedClients()
    {
      CpAction action = GetAction("GetConnectedClients");
      IList<object> outParams = action.InvokeAction(null);
      return MarshallingHelper.ParseCsvStringCollection((string) outParams[0]);
    }

    public void ScheduleImports(IEnumerable<Guid> shareIds, ImportJobType importJobType)
    {
      CpAction action = GetAction("ScheduleImports");
      IList<object> inParams = new List<object> {MarshallingHelper.SerializeGuidEnumerationToCsv(shareIds), importJobType == ImportJobType.Refresh ? "Refresh" : "Import"};
      action.InvokeAction(inParams);
    }

    public SystemName GetSystemNameForSystemId(string systemId)
    {
      CpAction action = GetAction("GetSystemNameForSystemId");
      IList<object> outParams = action.InvokeAction(new List<object> {systemId});
      string hostName = (string) outParams[0];
      if (string.IsNullOrEmpty(hostName))
        return null;
      return new SystemName(hostName);
    }

    // TODO: State variables, if present
  }
}
