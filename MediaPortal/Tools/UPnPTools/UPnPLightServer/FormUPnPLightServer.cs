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
using System.Windows.Forms;

namespace UPnPLightServer
{
  public partial class FormUPnPLightServer : Form
  {
    protected UPnPLightServer _server = null;
    protected static readonly Guid SERVER_ID = new Guid("{8A382006-1ADA-4659-B565-E5833226EAED}");

    public FormUPnPLightServer()
    {
      InitializeComponent();
    }

    protected void UpdateServer()
    {
      if (cbActivateServer.Checked)
      {
        if (_server != null)
          return;
        _server = new UPnPLightServer(SERVER_ID.ToString("B"));
        _server.Start();
      }
      else
      {
        if (_server == null)
          return;
        _server.Dispose();
        _server = null;
      }
    }

    private void FormUPnPLightServer_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (_server != null)
        _server.Close();
    }

    private void FormUPnPLightServer_Shown(object sender, EventArgs e)
    {
      UpdateServer();
    }

    private void cbActivateServer_CheckedChanged(object sender, EventArgs e)
    {
      UpdateServer();
    }
  }
}
