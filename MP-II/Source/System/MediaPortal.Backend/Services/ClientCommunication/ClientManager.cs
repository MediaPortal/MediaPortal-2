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
using System.Collections.Generic;
using System.Data;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Core.Threading;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  public class ClientManager : IClientManager
  {
    protected UPnPServerControlPoint _controlPoint = null;
    protected object _syncObj = new object();

    public ClientManager()
    {
      _controlPoint = new UPnPServerControlPoint();
      _controlPoint.ClientConnected += OnClientConnected;
      _controlPoint.ClientDisconnected += OnClientDisconnected;
    }

    void OnClientConnected(ClientDescriptor client)
    {
      SetClientConnectionState(client.MPFrontendServerUUID, client.System, true);
      ClientManagerMessaging.SendConnectionStateChangedMessage(ClientManagerMessaging.MessageType.ClientOnline, client);
      // This method is called as a result of our control point's attempt to connect to the (allegedly attached) client;
      // But maybe the client isn't attached any more to this server (it could have detached while the server wasn't online).
      // So we will validate if the client is still attached.
      ClientManagerMessaging.SendConnectionStateChangedMessage(
          ClientManagerMessaging.MessageType.ValidateAttachmentState, client);
      ServiceScope.Get<IThreadPool>().Add(() => CompleteClientConnection(client));
    }

    void OnClientDisconnected(ClientDescriptor client)
    {
      SetClientConnectionState(client.MPFrontendServerUUID, client.System, false);
      ClientManagerMessaging.SendConnectionStateChangedMessage(ClientManagerMessaging.MessageType.ClientOffline, client);
    }

    /// <summary>
    /// Returns a dictionary which maps the system ids of all attached clients to their last hostname.
    /// </summary>
    /// <returns>Dictionary with system ids mapped to host names.</returns>
    protected IDictionary<string, string> ReadAttachedClients()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int attachedClientsIndex;
        int lastHostNameIndex;
        IDbCommand command = ClientManager_SubSchema.SelectAttachedClientsCommand(transaction, out attachedClientsIndex,
            out lastHostNameIndex);
        IDataReader reader = command.ExecuteReader();
        IDictionary<string, string> result = new Dictionary<string, string>();
        try
        {
          while (reader.Read())
            result.Add(DBUtils.ReadDBValue<string>(reader, attachedClientsIndex),
                DBUtils.ReadDBValue<string>(reader, lastHostNameIndex));
        }
        finally
        {
          reader.Close();
        }
        return result;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    protected void CompleteClientConnection(ClientDescriptor client)
    {
      if (ValidateAttachmentState(client))
        UpdateClientSystem(client.MPFrontendServerUUID, client.System);
    }

    protected bool ValidateAttachmentState(ClientDescriptor client)
    {
      string clientSystemId = client.MPFrontendServerUUID;
      ServiceScope.Get<ILogger>().Info("ClientManager: Validating attachment state of client '{0}' (system ID '{1}')",
          client.ClientName, clientSystemId);
      ClientConnection connection = _controlPoint.GetClientConnection(clientSystemId);
      if (connection != null)
      {
        string homeServer = connection.ClientController.GetHomeServer();
        ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
        if (homeServer != systemResolver.LocalSystemId)
        {
          ServiceScope.Get<ILogger>().Info(
              "ClientManager: Client '{0}' is no longer attached to this server, cleaning up client data", clientSystemId);
          DetachClientAndRemoveShares(clientSystemId);
          return false;
        }
      }
      return true;
    }

    protected void SetClientConnectionState(string clientSystemId, SystemName currentSytemName, bool isOnline)
    {
      IMediaLibrary mediaLibrary = ServiceScope.Get<IMediaLibrary>();
      if (isOnline)
      {
        mediaLibrary.NotifySystemOnline(clientSystemId, currentSytemName);
        UpdateClientSystem(clientSystemId, currentSytemName);
      }
      else
        mediaLibrary.NotifySystemOffline(clientSystemId);
    }

    protected void UpdateClientSystem(string clientSystemId)
    {
      SystemName systemName = null;
      foreach (ClientDescriptor client in _controlPoint.AvailableClients)
        if (client.MPFrontendServerUUID == clientSystemId)
          systemName = client.System;
      if (systemName != null)
        UpdateClientSystem(clientSystemId, systemName);
    }

    protected void UpdateClientSystem(string clientSystemId, SystemName system)
    {
      ServiceScope.Get<ILogger>().Info("ClientManager: Updating host name of client '{0}' to '{1}'", clientSystemId, system.HostName);
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = ClientManager_SubSchema.UpdateAttachedClientSystemCommand(transaction, clientSystemId, system);
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("ClientManager: Error updating host name '{0}' of client '{1}'", e, system.HostName, clientSystemId);
        transaction.Rollback();
        throw;
      }
    }

    #region IClientManager implementation

    public void Startup()
    {
      DatabaseSubSchemaManager updater = new DatabaseSubSchemaManager(ClientManager_SubSchema.SUBSCHEMA_NAME);
      updater.AddDirectory(ClientManager_SubSchema.SubSchemaScriptDirectory);
      int curVersionMajor;
      int curVersionMinor;
      if (!updater.UpdateSubSchema(out curVersionMajor, out curVersionMinor) ||
          curVersionMajor != ClientManager_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR ||
          curVersionMinor != ClientManager_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR)
        throw new IllegalCallException(string.Format(
            "Unable to update the ClientManager's subschema version to expected version {0}.{1}",
            ClientManager_SubSchema.EXPECTED_SCHEMA_VERSION_MAJOR, ClientManager_SubSchema.EXPECTED_SCHEMA_VERSION_MINOR));
      _controlPoint.AttachedClientSystemIds = ReadAttachedClients().Keys;
      _controlPoint.Start();
    }

    public void Shutdown()
    {
      _controlPoint.Stop();
    }

    public ICollection<ClientConnection> ConnectedClients
    {
      get { return _controlPoint.ClientConnections.Values; }
    }

    public ICollection<string> AttachedClientsSystemIds
    {
      get { return _controlPoint.AttachedClientSystemIds; }
    }

    public void AttachClient(string clientSystemId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = ClientManager_SubSchema.InsertAttachedClientCommand(transaction, clientSystemId, null);
        command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("ClientManager: Error attaching client '{0}'", e, clientSystemId);
        transaction.Rollback();
        throw;
      }
      ServiceScope.Get<ILogger>().Info("ClientManager: Client with system ID '{0}' attached", clientSystemId);
      // Establish the UPnP connection to the client, if available in the network
      _controlPoint.AddAttachedClient(clientSystemId);
    }

    public void DetachClientAndRemoveShares(string clientSystemId)
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = ClientManager_SubSchema.DeleteAttachedClientCommand(transaction, clientSystemId);
        command.ExecuteNonQuery();

        IMediaLibrary mediaLibrary = ServiceScope.Get<IMediaLibrary>();
        mediaLibrary.DeleteMediaItemOrPath(clientSystemId, null);
        mediaLibrary.RemoveSharesOfSystem(clientSystemId);

        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("ClientManager: Error detaching client '{0}'", e, clientSystemId);
        transaction.Rollback();
        throw;
      }
      ServiceScope.Get<ILogger>().Info("ClientManager: Client with system ID '{0}' detached", clientSystemId);
      // Last action: Remove the client from the collection of attached clients and disconnect the client connection, if connected
      _controlPoint.RemoveAttachedClient(clientSystemId);
    }

    public SystemName GetSystemNameForSystemId(string systemId)
    {
      ClientConnection connection = _controlPoint.GetClientConnection(systemId);
      if (connection == null)
        return null;
      return connection.Descriptor.System;
    }

    #endregion
  }
}
