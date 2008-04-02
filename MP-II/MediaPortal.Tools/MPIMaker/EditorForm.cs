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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using MediaPortal.Services.ExtensionManager;
using MediaPortal.Core;
using MediaPortal.Services.PathManager;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.ExtensionManager;
using ICSharpCode.SharpZipLib.Zip;


namespace ExtensionMaker
{
  public partial class EditorForm : Form
  {
    private ExtensionInstaller Installer = new ExtensionInstaller();
    private ExtensionPackage Package = new ExtensionPackage();
    private string FileName = string.Empty;
    private bool loading = false;
    private const string REORDER = "Reorder";
    private bool allowRowReorder = true;

    public EditorForm()
    {
      ServiceScope.Add<IPathManager>(new PathManager());
      InitializeComponent();
      foreach(KeyValuePair<string, IExtensionFileAction> kpv in Installer.GetAllFileActions())
      {
        comboBoxAction.Items.Add(kpv.Key);
      }
      foreach (ExtensionEnumeratorObject obj in Installer.GetAllKnowExtensions())
      {
        comboBoxDependenciExt.Items.Add(obj);
      }
    }

    public bool AllowRowReorder
    {
      get
      {
        return this.allowRowReorder;
      }
      set
      {
        this.allowRowReorder = value;
        base.AllowDrop = value;
      }
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void filesToolStripMenuItem_Click(object sender, EventArgs e)
    {
      saveFileDialog1.Filter = "All files|*.*|xml file (*.xml)|*.xml|Jpeg files (*.jpg)|*.jpg|PNG files (*.png)|*.png";
      openFileDialog1.Multiselect = true;
      if (openFileDialog1.ShowDialog() == DialogResult.OK)
      {
        foreach (string s in openFileDialog1.FileNames)
          AddFile(s);
      }
    }

    private void AddFile(string filename)
    {
      ExtensionFileItem fil = new ExtensionFileItem(filename,"","","","");
      ListViewItem item1 = new ListViewItem(filename);
      item1.SubItems.Add("");
      item1.SubItems.Add("");
      item1.SubItems.Add("");
      item1.SubItems.Add("");
      FileItemToListItem(fil,item1);
      item1.Tag = fil;
      listView1.Items.AddRange(new ListViewItem[] { item1 });
      Package.Items.Add(fil);
    }

    private void comboBoxAction_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (!loading)
      {
        labelDescription.Text = Installer.GetFileAction(comboBoxAction.Text).Description();
        InitCombo(comboBoxParam1, Installer.GetFileAction(comboBoxAction.Text).Param1List());
        InitCombo(comboBoxParam2, Installer.GetFileAction(comboBoxAction.Text).Param2List());
        InitCombo(comboBoxParam3, Installer.GetFileAction(comboBoxAction.Text).Param3List());

        comboBoxParam1_TextChanged(null, null);
      }
    }

    private void InitCombo(ComboBox cb, List<string> ls)
    {
      cb.Items.Clear();
      cb.Visible = true;
      if (ls != null)
        cb.Items.AddRange(ls.ToArray());
      else
        cb.Visible = false;
    }

    private void button1_Click(object sender, EventArgs e)
    {
      textBoxId.Text = Guid.NewGuid().ToString();
    }

    private void listView1_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (listView1.SelectedItems.Count > 0)
      {
        loading = true;
        string it1 = listView1.SelectedItems[0].SubItems[1].Text;
        string it2 = listView1.SelectedItems[0].SubItems[2].Text;
        string it3 = listView1.SelectedItems[0].SubItems[3].Text;
        string it4 = listView1.SelectedItems[0].SubItems[4].Text;
        foreach (ListViewItem item in listView1.SelectedItems)
        {
          if (it1 != item.SubItems[1].Text)
            it1 = string.Empty;
          if (it2 != item.SubItems[2].Text)
            it2 = string.Empty;
          if (it3 != item.SubItems[3].Text)
            it3 = string.Empty;
          if (it4 != item.SubItems[4].Text)
            it4 = string.Empty;
        }
        GetProperty(it1, it2, it3, it4);
        loading = false;
        comboBoxAction.Enabled = true;
        comboBoxParam1.Enabled = true;
        comboBoxParam2.Enabled = true;
        comboBoxParam3.Enabled = true;
        removeToolStripMenuItem.Enabled = true;
      }
      else
      {
        removeToolStripMenuItem.Enabled = false;
        comboBoxAction.Enabled = false;
        comboBoxParam1.Enabled = false;
        comboBoxParam2.Enabled = false;
        comboBoxParam3.Enabled = false;
      }
    }

    private void GetProperty(ListViewItem it)
    {
      comboBoxAction.Text = it.SubItems[1].Text;
      comboBoxParam1.Text = it.SubItems[2].Text;
      comboBoxParam2.Text = it.SubItems[3].Text;
      comboBoxParam3.Text = it.SubItems[4].Text;
    }

    private void GetProperty(string it1, string it2, string it3, string it4)
    {
      comboBoxAction.Text = it1;
      comboBoxParam1.Text = it2;
      comboBoxParam2.Text = it3;
      comboBoxParam3.Text = it4;
    }

    private void comboBoxParam1_TextChanged(object sender, EventArgs e)
    {
      if (listView1.SelectedItems.Count > 0 && !loading)
      {
        foreach (ListViewItem it in listView1.SelectedItems)
        {
          SetProperty(it);
        }
      }
    }

    private void SetProperty(ListViewItem it)
    {
      ((ExtensionFileItem)it.Tag).Action = comboBoxAction.Text;
      ((ExtensionFileItem)it.Tag).Param1 = comboBoxParam1.Text;
      ((ExtensionFileItem)it.Tag).Param2 = comboBoxParam2.Text;
      ((ExtensionFileItem)it.Tag).Param3 = comboBoxParam3.Text;
      FileItemToListItem((ExtensionFileItem)it.Tag, it);
    }

    private void FileItemToListItem(ExtensionFileItem fi, ListViewItem it)
    {
      it.Text = fi.FileName;
      it.SubItems[1].Text = fi.Action;
      it.SubItems[2].Text = fi.Param1;
      it.SubItems[3].Text = fi.Param2;
      it.SubItems[4].Text = fi.Param3;
    }

    private void textBoxName_TextChanged(object sender, EventArgs e)
    {
      GetPackageProperty();
    }

    private void GetPackageProperty()
    {
      if (!loading)
      {
        Package.Name = textBoxName.Text;
        Package.PackageId = textBoxId.Text;
        Package.ExtensionId = textBoxExtensionId.Text;
        Package.Version = textBoxVersion.Text;
        Package.VersionType = comboBoxVersionType.Text;
        Package.Description = textBoxDescription.Text;
        Package.Author = textBoxAuthor.Text;
        Package.ExtensionType = comboBoxType.Text;
      }
    }

    private void saveToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(FileName))
      {
        saveFileDialog1.Filter = "Extension project file (*.xmp)|*.xmp|All files|*.*";
        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
        {
          FileName = saveFileDialog1.FileName;
          SaveFile(FileName);
        }
      }
      else
      {
        SaveFile(FileName);
      }
    }

    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      openFileDialog1.Multiselect = false;
      saveFileDialog1.Filter = "Extension project file (*.xmp)|*.xmp|All files|*.*";
      if (saveFileDialog1.ShowDialog()==DialogResult.OK)
      {
        FileName = saveFileDialog1.FileName;
        SaveFile(FileName);
      }
    }
    
    private void SaveFile(string filename)
    {
      Package.Items.Clear();
      foreach(ListViewItem item in listView1.Items)
      {
        Package.Items.Add((ExtensionFileItem)item.Tag);
      }
      XmlSerializer serializer = new XmlSerializer(typeof(ExtensionPackage));
      TextWriter writer = new StreamWriter(filename);
      serializer.Serialize(writer, Package);
      writer.Close();
    }

    public void LoadFromPackage()
    {
      if (Package != null)
      {
        listView1.Items.Clear();
        foreach (ExtensionFileItem fi in Package.Items)
        {
          ListViewItem item1 = new ListViewItem(fi.FileName);
          item1.SubItems.Add("");
          item1.SubItems.Add("");
          item1.SubItems.Add("");
          item1.SubItems.Add("");
          FileItemToListItem(fi, item1);
          item1.Tag = fi;
          listView1.Items.AddRange(new ListViewItem[] { item1 });
        }
        foreach (ExtensionDependency dep in Package.Dependencies)
        {
          ListViewItem item1 = new ListViewItem(dep.ExtensionId);
          item1.SubItems.Add(dep.Operator);
          item1.SubItems.Add(dep.Version);
          item1.Tag = dep;
          listView2.Items.AddRange(new ListViewItem[] { item1 });
        }
        loading = true;
        textBoxName.Text = Package.Name;
        textBoxId.Text = Package.PackageId;
        textBoxExtensionId.Text = Package.ExtensionId;
        textBoxVersion.Text = Package.Version;
        comboBoxVersionType.Text = Package.VersionType;
        textBoxAuthor.Text = Package.Author;
        textBoxDescription.Text = Package.Description;
        comboBoxType.Text = Package.ExtensionType;
        loading = false;
      }
    }

    private void LoadFile(string filename)
    {
      FileName = filename;
      if (File.Exists(filename))
      {
        Package = (ExtensionPackage)Installer.LoadPackageFromXML(filename);
        LoadFromPackage();
      }
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
      openFileDialog1.Filter = "Extension project file (*.xmp)|*.xmp|All files|*.*";
      if (openFileDialog1.ShowDialog() == DialogResult.OK)
      {
        LoadFile(openFileDialog1.FileName);
      }
    }

    private void buttonSaveBuildFile_Click(object sender, EventArgs e)
    {
      saveFileDialog1.Filter = "Extension file (*.mpi)|*.mpi|All files|*.*";
      if (saveFileDialog1.ShowDialog() == DialogResult.OK)
      {
        textBoxBuildFile.Text = saveFileDialog1.FileName;
      }
    }

    private void buttonBuild_Click(object sender, EventArgs e)
    {
      listBoxBuild.Items.Clear();
      try
      {
        string tempfile = Path.GetTempFileName();
        SaveFile(tempfile);
        listBoxBuild.Items.Add("Adding files:");
        using (ZipOutputStream s = new ZipOutputStream(File.Create(textBoxBuildFile.Text)))
        {
          //s.SetLevel(9); // 0 - store only to 9 - means best compression
          AddFileToZip(s, tempfile, "installer.xmp");
          listBoxBuild.Items.Add(tempfile);
          foreach (ExtensionFileItem fileitem in Package.Items)
          {
            AddFileToZip(s, fileitem.FileName, Installer.GetFileAction(fileitem.Action).GetZipEntry(fileitem));
            listBoxBuild.Items.Add(fileitem.FileName);
            listBoxBuild.Update();
          }
        }
        listBoxBuild.Items.Add("Done");
      }
      catch (Exception ex)
      {
        listBoxBuild.Items.Add("Error: "+ex.Message);
      }

    }

    /// <summary>
    /// Adds the file to zip.
    /// </summary>
    /// <param name="s">The s.</param>
    /// <param name="filename">The filename.</param>
    /// <param name="entryname">The entryname.</param>
    private void AddFileToZip(ZipOutputStream s, string filename,string entryname)
    {
      byte[] buffer = new byte[4096];
      ZipEntry entry = new ZipEntry(entryname);

      // Setup the entry data as required.

      // Crc and size are handled by the library for seakable streams
      // so no need to do them here.

      // Could also use the last write time or similar for the file.
      FileInfo fl = new FileInfo(filename);
      entry.DateTime = DateTime.Now;
      entry.Size = fl.Length;
      s.PutNextEntry(entry);

      using (FileStream fs = File.OpenRead(filename))
      {

        // Using a fixed size buffer here makes no noticeable difference for output
        // but keeps a lid on memory usage.
        int sourceBytes;
        do
        {
          sourceBytes = fs.Read(buffer, 0, buffer.Length);
          s.Write(buffer, 0, sourceBytes);
        } while (sourceBytes > 0);
      }


    }

    private void buttonAddDependencie_Click(object sender, EventArgs e)
    {
      ListViewItem item1 = new ListViewItem(((ExtensionEnumeratorObject)comboBoxDependenciExt.SelectedItem).ExtensionId);
      item1.SubItems.Add(comboBoxDependecieOp.Text);
      item1.SubItems.Add(textBoxDependencieVersion.Text);
      ExtensionDependency dep = new ExtensionDependency(((ExtensionEnumeratorObject)comboBoxDependenciExt.SelectedItem).ExtensionId, comboBoxDependecieOp.Text, textBoxDependencieVersion.Text);
      item1.Tag = dep;
      Package.Dependencies.Add(dep);
      listView2.Items.AddRange(new ListViewItem[] { item1 });
    }

    private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      if (listView2.SelectedItems.Count > 0)
      {
        foreach (ExtensionEnumeratorObject obj in comboBoxDependenciExt.Items)
        {
          if (obj.ExtensionId == listView2.SelectedItems[0].Text)
            comboBoxDependenciExt.SelectedItem = obj; 
        }
        //comboBoxDependenciExt.Text = listView2.SelectedItems[0].Text;
        comboBoxDependecieOp.Text = listView2.SelectedItems[0].SubItems[1].Text;
        textBoxDependencieVersion.Text = listView2.SelectedItems[0].SubItems[2].Text;
        Package.Dependencies.Remove((ExtensionDependency)listView2.SelectedItems[0].Tag);
        listView2.Items.Remove(listView2.SelectedItems[0]);
      }
    }

    private void removeToolStripMenuItem_Click(object sender, EventArgs e)
    {
      foreach (ListViewItem item in listView1.SelectedItems)
      {
        listView1.Items.Remove(item);
        Package.Items.Remove((ExtensionFileItem)item.Tag);
      }
    }

    #region drag/drop in listview1
    private void listView1_DragDrop(object sender, DragEventArgs e)
    {
      //listView1.OnDragDrop(e);
      if (!this.AllowRowReorder)
      {
        return;
      }
      if (listView1.SelectedItems.Count == 0)
      {
        return;
      }
      Point cp = listView1.PointToClient(new Point(e.X, e.Y));
      ListViewItem dragToItem = listView1.GetItemAt(cp.X, cp.Y);
      if (dragToItem == null)
      {
        return;
      }
      int dropIndex = dragToItem.Index;
      if (dropIndex > listView1.SelectedItems[0].Index)
      {
        dropIndex++;
      }
      ArrayList insertItems =
        new ArrayList(listView1.SelectedItems.Count);
      foreach (ListViewItem item in listView1.SelectedItems)
      {
        insertItems.Add(item.Clone());
      }
      for (int i = insertItems.Count - 1; i >= 0; i--)
      {
        ListViewItem insertItem =
          (ListViewItem)insertItems[i];
        listView1.Items.Insert(dropIndex, insertItem);
      }
      foreach (ListViewItem removeItem in listView1.SelectedItems)
      {
        listView1.Items.Remove(removeItem);
      }
    }

    private void listView1_DragOver(object sender, DragEventArgs e)
    {
      if (!this.AllowRowReorder)
      {
        e.Effect = DragDropEffects.None;
        return;
      }
      if (!e.Data.GetDataPresent(DataFormats.Text))
      {
        e.Effect = DragDropEffects.None;
        return;
      }
      Point cp = listView1.PointToClient(new Point(e.X, e.Y));
      ListViewItem hoverItem = listView1.GetItemAt(cp.X, cp.Y);
      if (hoverItem == null)
      {
        e.Effect = DragDropEffects.None;
        return;
      }
      foreach (ListViewItem moveItem in listView1.SelectedItems)
      {
        if (moveItem.Index == hoverItem.Index)
        {
          e.Effect = DragDropEffects.None;
          hoverItem.EnsureVisible();
          return;
        }
      }
      //listView1.OnDragOver(e);
      String text = (String)e.Data.GetData(REORDER.GetType());
      if (text.CompareTo(REORDER) == 0)
      {
        e.Effect = DragDropEffects.Move;
        hoverItem.EnsureVisible();
      }
      else
      {
        e.Effect = DragDropEffects.None;
      }

    }

    private void listView1_DragEnter(object sender, DragEventArgs e)
    {
      //listView1.OnDragEnter(e);
      if (!this.AllowRowReorder)
      {
        e.Effect = DragDropEffects.None;
        return;
      }
      if (!e.Data.GetDataPresent(DataFormats.Text))
      {
        e.Effect = DragDropEffects.None;
        return;
      }
      //listView1.OnDragEnter(e);
      String text = (String)e.Data.GetData(REORDER.GetType());
      if (text.CompareTo(REORDER) == 0)
      {
        e.Effect = DragDropEffects.Move;
      }
      else
      {
        e.Effect = DragDropEffects.None;
      }

    }

    private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
    {
      //listView1.ItemDrag(e);
      listView1.DoDragDrop(REORDER, DragDropEffects.Move);
    }

    #endregion

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
      Package = new ExtensionPackage();
      LoadFromPackage();
    }

    private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
      {
        DirectoryInfo di = new DirectoryInfo(folderBrowserDialog1.SelectedPath);
        FileInfo[] fileList = di.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (FileInfo f in fileList)
        {
          if (!f.DirectoryName.Contains(".svn"))
            AddFile(f.FullName);
        }
      }

    }

  }
}
