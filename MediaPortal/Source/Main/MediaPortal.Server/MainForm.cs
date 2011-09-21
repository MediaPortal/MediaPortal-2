#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Windows.Forms;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.ClientCommunication;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;

namespace MediaPortal.Server
{
  public partial class MainForm : Form
  {
    #region Consts

    public static string SERVER_TITLE_RES = "[MainForm.Title]";
    public static string ATTACHED_CLIENTS_RES = "[MainForm.AttachedClients]";
    public static string CLIENT_RES = "[MainForm.Client]";
    public static string SYSTEM_RES = "[MainForm.System]";
    public static string CONNECTION_STATE_RES = "[MainForm.ConnectionState]";
    public static string CONNECTED_RES = "[MainForm.Connected]";
    public static string DISCONNECTED_RES = "[MainForm.Disconnected]";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    public MainForm()
    {
      InitializeComponent();
      UpdateClientsList();
    }

    private void OnMainFormShown(object sender, System.EventArgs e)
    {
      InitializeLocalizedControls();
      _messageQueue = new AsynchronousMessageQueue("Server main form", new string[]
        {
            ClientManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected delegate void ParameterlessMethod();

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ClientManagerMessaging.CHANNEL)
      {
        ClientManagerMessaging.MessageType messageType =
            (ClientManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ClientManagerMessaging.MessageType.ClientAttached:
          case ClientManagerMessaging.MessageType.ClientDetached:
          case ClientManagerMessaging.MessageType.ClientOnline:
          case ClientManagerMessaging.MessageType.ClientOffline:
            ParameterlessMethod d = UpdateClientsList;
            Invoke(d);
            break;
        }
      }
    }

    protected void UpdateClientsList()
    {
      lvClients.BeginUpdate();
      try
      {
        lvClients.Items.Clear();
        IClientManager clientManager = ServiceRegistration.Get<IClientManager>();
        ICollection<ClientConnection> clients = clientManager.ConnectedClients;
        ICollection<string> connectedClientSystemIDs = new List<string>(clients.Count);
        foreach (ClientConnection clientConnection in clients)
          connectedClientSystemIDs.Add(clientConnection.Descriptor.MPFrontendServerUUID);
        foreach (MPClientMetadata attachedClientData in clientManager.AttachedClients.Values)
        {
          ListViewItem lvi = CreateClientItem(attachedClientData.LastClientName,
              attachedClientData.LastSystem == null ? null : attachedClientData.LastSystem.HostName,
              connectedClientSystemIDs.Contains(attachedClientData.SystemId));
          lvClients.Items.Add(lvi);
        }
      }
      finally
      {
        lvClients.EndUpdate();
      }
    }

    protected ListViewItem CreateClientItem(string clientName, string clientSystem, bool isConnected)
    {
      return new ListViewItem(new string[] {clientName, clientSystem, LocalizationHelper.Translate(isConnected ? CONNECTED_RES : DISCONNECTED_RES)});
    }

    protected void InitializeLocalizedControls()
    {
      Text = LocalizationHelper.Translate(SERVER_TITLE_RES);
      lbAttachedClients.Text = LocalizationHelper.Translate(ATTACHED_CLIENTS_RES);
      colClient.Text = LocalizationHelper.Translate(CLIENT_RES);
      colSystem.Text = LocalizationHelper.Translate(SYSTEM_RES);
      colConnectionState.Text = LocalizationHelper.Translate(CONNECTION_STATE_RES);
    }
  }
}