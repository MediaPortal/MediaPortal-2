#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Net;

namespace MediaPortal.Utilities.Screens
{
  public partial class DownloadScreen : Form
  {
    string source = string.Empty;
    string dest = string.Empty;
    WebClient client = new WebClient();
    public int direction = 0;
    public string user = string.Empty;
    public string password = string.Empty;
    
    public DownloadScreen(string s, string d)
    {
      InitializeComponent();
      this.Text = "Resolving host ....";
      source = s;
      dest = d;
      client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
      client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadEnd);
      progressBar1.Minimum = 0;
      progressBar1.Maximum = 100;
      progressBar1.Value = 0;
    }

    private void download_form_Shown(object sender, EventArgs e)
    {
      this.Refresh();
      this.Update();
      if (!String.IsNullOrEmpty(source) && !String.IsNullOrEmpty(dest))
      {
        if (direction == 0)
        {
          string result = string.Empty; ;
          try
          {
            client.CachePolicy = new System.Net.Cache.RequestCachePolicy();
            client.Credentials = new NetworkCredential("test", "testmaid5");
            client.DownloadFileAsync(new System.Uri(source), dest);
          }
          catch (WebException ex)
          {
            MessageBox.Show("Error ocured : " + ex.Message);
            this.Close();
          }
        }
      }
      else
      {
        this.Close();
      }
    }

    private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
    {
      progressBar1.Value = e.ProgressPercentage;
      this.Text = string.Format(" Downloading {0} kb/{1} kb", e.BytesReceived / 1024, e.TotalBytesToReceive / 1024);
    }

    private void DownloadEnd(object sender, AsyncCompletedEventArgs e)
    {
      if (e.Error != null)
      {

        if (File.Exists(dest))
          if (!client.IsBusy)
            try
            {
              File.Delete(dest);
            }
            catch (Exception)
            {
            }
        MessageBox.Show(e.Error.Message + "\n" + e.Error.InnerException);
      }
      this.Close();
    }

  }
}
