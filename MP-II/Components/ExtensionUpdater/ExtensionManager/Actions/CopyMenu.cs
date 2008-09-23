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
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Components.ExtensionUpdater.ExtensionManager.Actions.Helpers;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Presentation.MenuManager;
using ICSharpCode.SharpZipLib.Zip;

namespace Components.ExtensionUpdater.ExtensionManager.Actions
{
  class CopyMenu : IExtensionFileAction
  {
    private Dictionary<string, string> _dirs = new Dictionary<string, string>();
    private IMenu menu1;
    private IMenu menu2;

    public CopyMenu()
    {
      _dirs.Add("%BaseDir%", AppDomain.CurrentDomain.BaseDirectory);
      _dirs.Add("%PluginsDir%", AppDomain.CurrentDomain.BaseDirectory+"\\Plugins\\");
      _dirs.Add("%ModelsDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Models\\");
      _dirs.Add("%ViewsDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Views\\");
      _dirs.Add("%SkinDir%", AppDomain.CurrentDomain.BaseDirectory + "\\Skin\\");
    }
    /// <summary>
    /// Return a zip entry for a FileItem.
    /// </summary>
    /// <param name="item">The file item.</param>
    /// <returns></returns>
    public string GetZipEntry(IExtensionFileItem item)
    {
      return "Menus\\"+Path.GetFileName(item.FileName);
    }

    /// <summary>
    /// Return the dir entry for fileitem.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns></returns>
    public string GetDirEntry(IExtensionFileItem item)
    {
      return "Menus\\" + Path.GetFileName(item.FileName);
    }

    public string Description()
    {
      return "Copy a menu file to all skin menu folder";
    }
    
    public List<string> Param1List()
    {
      return null;
    }
    
    public List<string> Param2List()
    {
      return null;//new List<string>();
    }

    public List<string> Param3List()
    {
      return null; //new List<string>();
    }

    public override string ToString()
    {
      return "CopyMenu";
    }

    /// <summary>
    /// Installs the specified fileitem from holder.
    /// </summary>
    /// <param name="holder">The holder strem.</param>
    /// <param name="fileItem">The file item.</param>
    /// <param name="pak">The package</param>
    /// <returns></returns>
    public bool Install(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      try
      {
        string tpf = Path.GetTempFileName();
        Directory.CreateDirectory(Path.GetDirectoryName(tpf));
        byte[] data = new byte[2048];
        int nb = data.Length;
        FileStream fs = new FileStream(tpf, FileMode.Create);
        while ((nb = ((ZipInputStream)holder).Read(data, 0, data.Length)) > 0)
        {
          fs.Write(data, 0, nb);
        }
        fs.Close();
        menu1 = LoadMenu(tpf);
        string[] skins = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + "\\skin");
        for (int i = 0; i < skins.Length; ++i)
        {
          string dir = string.Format("{0}\\menus\\", Path.GetFullPath(skins[i]));
          string filename = dir + Path.GetFileName(fileItem.FileName);
          if (Directory.Exists(dir))
          {
            if(File.Exists(filename))
            {
              menu2 = LoadMenu(filename);
              Combine(menu2.Items, menu1.Items,pak.ExtensionId);
              SaveMenu(filename, menu2);
            }
            else
            {
              File.Copy(tpf, filename);
            }
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        System.Windows.Forms.MessageBox.Show(ex.Message+ex.StackTrace);
        return false;
      }
    }

    public bool UnInstall(object holder, IExtensionFileItem fileItem, IExtensionPackage pak)
    {
      try
      {
        string[] skins = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + "\\skin");
        for (int i = 0; i < skins.Length; ++i)
        {
          string dir = string.Format("{0}\\menus\\", Path.GetFullPath(skins[i]));
          string filename = dir + Path.GetFileName(fileItem.FileName);
          if (File.Exists(filename))
          {
            menu1 = LoadMenu(filename);
            RemoveItem(menu1.Items, pak.ExtensionId);
            if (menu1.Items.Count > 0)
            {
              SaveMenu(filename, menu1);
            }
            else
            {
              File.Delete(filename);
            }
          }
        }
        return true;
      }
      catch (Exception)
      {
      }
      return true;
    }
    #region privates

    public void RemoveItem(List<IMenuItem> items, string packageID)
    {
      for (int i = 0; i < items.Count; i++)
      {
        string strPackage = ((ExtMenuItem)items[i]).Packages;
        if (strPackage.Contains(packageID))
        {
          if (strPackage.Length > packageID.Length + 1)
          {
            strPackage = strPackage.Replace("+" + packageID, string.Empty);
          }
          else
          {
            strPackage = strPackage.Replace(packageID, string.Empty);
          }
          if (strPackage.Length == 0 && items[i].Items.Count == 0)
          {
            items.RemoveAt(i);
          }
          else
          {
            ((ExtMenuItem)items[i]).Packages = strPackage;
            RemoveItem(items[i].Items, packageID);
          }
        }
        else
        {
          RemoveItem(items[i].Items, packageID);
        }
      }
    }

    public IMenu LoadMenu(string menuFile)
    {
      MediaPortal.Services.MenuManager.Menu menu = new MediaPortal.Services.MenuManager.Menu(menuFile);
      //load menu configurarion
      //for now we simply load it from an .xml file since 
      //i dont know yet how the pluginmanager could do this.
      XmlDocument doc = new XmlDocument();
      doc.Load(menuFile);
      XmlNodeList nodesItems = doc.SelectNodes("/menu/items/item");
      foreach (XmlNode nodeItem in nodesItems)
      {
        menu.Items.Add(GetMenuItem(nodeItem));
      }
      return menu;
    }

    IMenuItem GetMenuItem(XmlNode node)
    {
      string label = node.Attributes["text"].Value;
      string image = node.Attributes["image"].Value;
      string command = node.Attributes["command"].Value;
      string parameter = node.Attributes["parameter"].Value;
      string packages = string.Empty;
      if (node.Attributes["packages"] != null)
        packages = node.Attributes["packages"].Value;
      ExtMenuItem item = new ExtMenuItem(label, image, command, parameter,packages);
      XmlNodeList nodesItems = node.SelectNodes("items/item");
      foreach (XmlNode nodeItem in nodesItems)
      {
        item.Items.Add(GetMenuItem(nodeItem));
      }
      return item;
    }

    public void SaveMenu(string menuFile, IMenu menu)
    {
      Stream myStream;
      if ((myStream = File.Open(menuFile, FileMode.Create, FileAccess.Write, FileShare.None)) != null)
      {
        XmlWriter writer = null;
        try
        {
          // Create an XmlWriterSettings object with the correct options. 
          XmlWriterSettings settings = new XmlWriterSettings();
          string st = string.Empty;
          settings.Indent = true;
          settings.IndentChars = ("    ");
          settings.OmitXmlDeclaration = true;
          // Create the XmlWriter object and write some content.
          writer = XmlWriter.Create(myStream, settings);
          writer.WriteStartElement("menu");
          WriteMenuItems(writer, menu.Items);
          writer.WriteEndElement();

        }
        catch (Exception ex)
        {
          System.Windows.Forms.MessageBox.Show(ex.Message);
        }
        finally
        {
          if (writer != null)
            writer.Close();
        }
        myStream.Close();
      }
    }

    private void WriteMenuItems(XmlWriter writer, List<IMenuItem> items)
    {
      if (items == null || items.Count == 0)
        return;
      writer.WriteStartElement("items");
      foreach (ExtMenuItem item in items)
      {
        writer.WriteStartElement("item");
        writer.WriteAttributeString("text", item.LiteralText);
        writer.WriteAttributeString("image", item.ImagePath);
        writer.WriteAttributeString("command", item.Command);
        writer.WriteAttributeString("parameter", item.CommandParameter);
        if(!string.IsNullOrEmpty(item.Packages))
          writer.WriteAttributeString("packages", item.Packages);
        WriteMenuItems(writer, item.Items);
        writer.WriteEndElement();
      }
      writer.WriteEndElement();
    }

    private void Combine(List<IMenuItem> source1, List<IMenuItem> source2, string packageId)
    {
      foreach(ExtMenuItem item in source2)
      {
        AddMenu(source1, item, packageId);
      }
    }

    private void AddMenu(List<IMenuItem> source, ExtMenuItem item, string packageId)
    {
      ExtMenuItem menuitem = (ExtMenuItem)FindMenu(source, item);
      if (menuitem == null)
      {
        item.Packages = packageId;
        source.Add(item);
      }
      else
      {
        if (!menuitem.Packages.Contains(packageId))
        {
          if (!string.IsNullOrEmpty(menuitem.Packages))
            menuitem.Packages += "+" + packageId;
        }
        Combine(menuitem.Items, item.Items,packageId);
      }
    }
    
    private IMenuItem FindMenu(List<IMenuItem> source,IMenuItem item)
    {
      foreach (IMenuItem it in source)
      {
        if (it.Command == item.Command && it.CommandParameter==item.CommandParameter)
          return it;
      }
      return null;
    }
    #endregion

  }
}
