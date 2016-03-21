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
using System.Data;
using MediaPortal.Backend.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Backend.ClientCommunication;

namespace MediaPortal.Plugins.MediaServer.Database
{
  public class MediaServerClientManager
  {
    public void AttachClient(string clientSystemId, string clientHostName, string clientName)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IClientManager clientManager = ServiceRegistration.Get<IClientManager>(false);
        if (clientManager != null) 
        {
          clientManager.AttachClient(clientSystemId);
          using (IDbCommand command = MediaServer_SubSchema.UpdateAttachedClientDataCommand(transaction, clientSystemId, clientHostName, clientName))
            command.ExecuteNonQuery();
        }
        else
        {
          using (IDbCommand command = MediaServer_SubSchema.InsertAttachedClientCommand(transaction, clientSystemId, clientHostName, clientName))
            command.ExecuteNonQuery();
          ClientManagerMessaging.SendClientAttachmentChangeMessage(ClientManagerMessaging.MessageType.ClientAttached, clientSystemId);
        }
        transaction.Commit();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaServer: Error attaching client '{0}'", e, clientSystemId);
        transaction.Rollback();
      }
    }

    public void DetachClient(string clientSystemId)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IClientManager clientManager = ServiceRegistration.Get<IClientManager>(false);
        if (clientManager != null)
        {
          clientManager.DetachClientAndRemoveShares(clientSystemId);
        }
        else
        {
          using (IDbCommand command = MediaServer_SubSchema.DeleteAttachedClientCommand(transaction, clientSystemId))
            command.ExecuteNonQuery();
          transaction.Commit();
          ClientManagerMessaging.SendClientAttachmentChangeMessage(ClientManagerMessaging.MessageType.ClientDetached, clientSystemId);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaServer: Error detaching client '{0}'", e, clientSystemId);
        transaction.Rollback();
      }
    }
  }
}
