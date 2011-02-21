#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Windows.Forms;
using MediaPortal.Core.MPIManager;
using MediaPortal.Services.MPIManager;

namespace ExtensionInstaller
{
  public partial class Form1 : Form
  {
    private MPInstaller Installer = new MPInstaller();
    private MPIPackage Package = new MPIPackage();
    private MPIQueue Queue = new MPIQueue();
    public Form1()
    {
      InitializeComponent();
      Installer.LoadQueue();
      Queue = (MPIQueue)Installer.GetQueue();
      LoadQueueToListview();
      LoadToListview2();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      if ((Package = (MPIPackage)Installer.LoadPackageFromMPI(textBox1.Text)) != null)
      {
        List<MPIEnumeratorObject> lst = Installer.GetUnsolvedDependencies(Package);
        if (lst.Count>0)
        {
          foreach (MPIEnumeratorObject obj in lst)
          {
            MessageBox.Show("Dependency required \n" +obj.ExtensionId+"\n"+obj.Name);
          }
          return;
        }
        MessageBox.Show(Package.Name + "\n" + Package.Items.Count.ToString() + "\n" + Package.FileName + "\n" + "Added to queue");
        Installer.AddToQueue(Package, "Install");
        Queue = (MPIQueue)Installer.GetQueue();
        LoadQueueToListview();
        Installer.SaveQueue();
      }
      else
      {
        MessageBox.Show("Error loading package !!");
      }
    }

    private void button2_Click(object sender, EventArgs e)
    {
      if (openFileDialog1.ShowDialog() == DialogResult.OK)
      {
        textBox1.Text = openFileDialog1.FileName;
      }
    }

    private void LoadQueueToListview()
    {
      listView1.Items.Clear();
      foreach (MPIQueueObject ob in Queue.Items)
      {
        ListViewItem item = new ListViewItem(ob.PackageId);
        item.SubItems.Add(ob.Action);
        item.SubItems.Add(ob.PackageName);
        listView1.Items.AddRange(new ListViewItem[] { item });
      }
    }

    private void LoadToListview2()
    {
      treeView1.Nodes.Clear();
      foreach (KeyValuePair<string,List<MPIEnumeratorObject>> key in Installer.Enumerator.Items)
      {
        TreeNode node = new TreeNode(key.Key);
        foreach (MPIEnumeratorObject ob in key.Value)
        {
          node.Nodes.Add(ob.PackageId + " - " + ob.State.ToString()+" - " + ob.Name+" Deps:"+ob.Dependencies.Count.ToString()+" Files:"+ob.Items.Count.ToString());
        }
        treeView1.Nodes.Add(node);
      }
    }

    private void button3_Click(object sender, EventArgs e)
    {
      //MessageBox.Show("Not workin YET!");
      Installer.ExecuteQueue(false);
      Queue = (MPIQueue)Installer.GetQueue();
      Installer.SaveQueue();
      LoadQueueToListview();
    }

    private void listView1_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (listView1.SelectedItems.Count > 0)
        button4.Enabled = true;
      else
        button4.Enabled = false;
    }

    private void button4_Click(object sender, EventArgs e)
    {
      if (listView1.SelectedItems.Count > 0)
      {
        Installer.RemoveFromQueue(listView1.SelectedItems[0].Text);
        LoadQueueToListview();
        Installer.SaveQueue();
      }
    }
  }
}
