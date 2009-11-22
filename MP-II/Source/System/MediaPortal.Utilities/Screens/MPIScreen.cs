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
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MediaPortal.Utilities.Screens
{
  public partial class MPIScreen : Form
  {

    public MPIScreen()
    {
      InitializeComponent();
      listBox1.Items.Clear();
    }

    public void SetAllCount(int i)
    {
      progressBar1.Maximum = i;
      this.Refresh();
      this.Update();
    }
   
    public void SetFileCount(int i)
    {
      progressBar2.Maximum = i;
      progressBar2.Value = progressBar2.Minimum;
      this.Refresh();
      this.Update();
    }

    public void NextFile()
    {
      if (progressBar2.Value < progressBar2.Maximum)
        progressBar2.Value++;
      this.Refresh();
      this.Update();
    }

    public void Next()
    {
      progressBar1.Value++;
      this.Refresh();
      this.Update();
    }

    public void AddText(string s)
    {
      listBox1.Items.Add(s);
      this.Refresh();
      this.Update();
    }
  }
}
