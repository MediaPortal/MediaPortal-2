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
using System.Data;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.ClientCommunication
{
  public class ClientManager : IClientManager
  {
    protected UPnPServerControlPoint _controlPoint = null;
    protected object _syncObj = new object();
    protected IDictionary<string, MPClientMetadata> _attachedClients;

    public ClientManager()
    {
      _controlPoint = new UPnPServerControlPoint();
      _controlPoint.ClientConnected += OnClientConnected;
      _controlPoint.ClientDisconnected += OnClientDisconnected;
    }

    void OnClientConnected(ClientDescriptor client)
    {
      UpdateClientSetOnline(client.MPFrontendServerUUID, client.System);
      // This method is called as a result of our control point's attempt to connect to the (allegedly attached) client;
      // But maybe the client isn't attached any more to this server (it could have detached while the server wasn't online).
      // So we will validate if the client is still attached.
      ServiceRegistration.Get<IThreadPool>().Add(() => CompleteClientConnection(client));
    }

    void OnClientDisconnected(ClientDescriptor client)
    {
      UpdateClientSetOffline(client.MPFrontendServerUUID);
      ClientManagerMessaging.SendConnectionStateChangedMessage(ClientManagerMessaging.MessageType.ClientOffline, client);
    }

    protected void CompleteClientConnection(ClientDescriptor client)
    {
      if (!ValidateAttachmentState(client))
        return;
      UpdateClientSystem(client.MPFrontendServerUUID, client.System, client.ClientName);
      ClientManagerMessaging.SendConnectionStateChangedMessage(ClientManagerMessaging.MessageType.ClientOnline, client);
    }

    protected bool ValidateAttachmentState(ClientDescriptor client)
    {
      string clientSystemId = client.MPFrontendServerUUID;
      ServiceRegistration.Get<ILogger>().Info("ClientManager: Validating attachment state of client '{0}' (system ID '{1}')",
          client.ClientName, clientSystemId);
      ClientConnection connection = _controlPoint.GetClientConnection(clientSystemId);
      if (connection != null)
      {
        string homeServerSystemId = connection.ClientController.GetHomeServerSystemId();
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        if (homeServerSystemId != systemResolver.LocalSystemId)
        {
          ServiceRegistration.Get<ILogger>().Info(
              "ClientManager: Client '{0}' is no longer attached to this server, cleaning up client data", clientSystemId);
          DetachClientAndRemoveShares(clientSystemId);
          return false;
        }
      }
      return true;
    }

    protected void UpdateClientSetOnline(string clientSystemId, SystemName currentSytemName)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      mediaLibrary.NotifySystemOnline(clientSystemId, currentSytemName);
    }

    protected void UpdateClientSetOffline(string clientSystemId)
    {
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      mediaLibrary.NotifySystemOffline(clientSystemId);
    }

    /// <summary>
    /// Returns a dictionary which maps the system ids of all attached clients to their last hostname.
    /// </summary>
    /// <returns>Dictionary with system ids mapped to host names.</returns>
    protected IDictionary<string, MPClientMetadata> ReadAttachedClientsFromDB()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        int systemIdIndex;
        int lastHostNameIndex;
        int lastClientNameIndex;
        IDictionary<string, MPClientMetadata> result = new Dictionary<string, MPClientMetadata>();
        using (IDbCommand command = ClientManager_SubSchema.SelectAttachedClientsCommand(transaction, out systemIdIndex,
            out lastHostNameIndex, out lastClientNameIndex))
        using (IDataReader reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            string clientSystemId = database.ReadDBValue<string>(reader, systemIdIndex);
            string lastClientHostName = database.ReadDBValue<string>(reader, lastHostNameIndex);
            SystemName lastHostName = lastClientHostName == null ? null : new SystemName(lastClientHostName);
            string lastClientName = database.ReadDBValue<string>(reader, lastClientNameIndex);
            result.Add(clientSystemId, new MPClientMetadata(clientSystemId, lastHostName, lastClientName));
          }
        }
        return result;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    protected void UpdateClientSystem(string clientSystemId, SystemName system, string clientName)
    {
      ServiceRegistration.Get<ILogger>().Info("ClientManager: Updating host name of client '{0}' to '{1}'", clientSystemId, system.HostName);
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = ClientManager_SubSchema.UpdateAttachedClientDataCommand(transaction, clientSystemId, system, clientName))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientManager: Error updating host name '{0}' of client '{1}'", e, system.HostName, clientSystemId);
        transaction.Rollback();
        throw;
      }
      IDictionary<string, MPClientMetadata> attachedClients = ReadAttachedClientsFromDB();
      lock (_syncObj)
        _attachedClients = attachedClients;
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
      IDictionary<string, MPClientMetadata> attachedClients = ReadAttachedClientsFromDB();
      lock (_syncObj)
        _attachedClients = attachedClients;
      _controlPoint.AttachedClientSystemIds = attachedClients.Keys;
      _controlPoint.Start();
    }

    public void Shutdown()
    {
      _controlPoint.Stop();
    }

    public object SyncObj
    {
      get { return _syncObj; }
    }

    public ICollection<ClientConnection> ConnectedClients
    {
      get
      {
        lock (_syncObj)
          return new List<ClientConnection>(_controlPoint.ClientConnections.Values);
      }
    }

    public IDictionary<string, MPClientMetadata> AttachedClients
    {
      get
      {
        lock (_syncObj)
          return new Dictionary<string, MPClientMetadata>(_attachedClients);
      }
    }

    public void AttachClient(string clientSystemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = ClientManager_SubSchema.InsertAttachedClientCommand(transaction, clientSystemId, null, null))
          command.ExecuteNonQuery();
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientManager: Error attaching client '{0}'", e, clientSystemId);
        transaction.Rollback();
        throw;
      }
      ServiceRegistration.Get<ILogger>().Info("ClientManager: Client with system ID '{0}' attached", clientSystemId);
      // Establish the UPnP connection to the client, if available in the network
      IDictionary<string, MPClientMetadata> attachedClients = ReadAttachedClientsFromDB();
      lock (_syncObj)
        _attachedClients = attachedClients;
      _controlPoint.AddAttachedClient(clientSystemId);
      ClientManagerMessaging.SendClientAttachmentChangeMessage(ClientManagerMessaging.MessageType.ClientAttached, clientSystemId);
    }

    public void DetachClientAndRemoveShares(string clientSystemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = ClientManager_SubSchema.DeleteAttachedClientCommand(transaction, clientSystemId))
          command.ExecuteNonQuery();
        // this commit was placed here to avoid nested write transactions.
        // ToDo: Ideally this transaction together with the two calls to mediaLibrary below should be in one transaction.
        transaction.Commit();
        transaction = null;

        IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
        mediaLibrary.DeleteMediaItemOrPath(clientSystemId, null, true);
        mediaLibrary.RemoveSharesOfSystem(clientSystemId);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientManager: Error detaching client '{0}'", e, clientSystemId);
        if(transaction != null)
          transaction.Rollback();
        throw;
      }
      ServiceRegistration.Get<ILogger>().Info("ClientManager: Client with system ID '{0}' detached", clientSystemId);
      // Last action: Remove the client from the collection of attached clients and disconnect the client connection, if connected
      _attachedClients = ReadAttachedClientsFromDB();
      _controlPoint.RemoveAttachedClient(clientSystemId);
      ClientManagerMessaging.SendClientAttachmentChangeMessage(ClientManagerMessaging.MessageType.ClientDetached, clientSystemId);
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
