#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.IO;
using System.Xml;
using MediaPortal.Core.Settings;

namespace MediaPortal.Services.Settings
{
  public class XmlFileHandler
  {
    #region Private variables
    private XmlDocument document = null;
    private bool modified = false;
    private string filename = null;
    #endregion

    #region Ctor

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="xmlfilename">The xml file to write into</param>
    public XmlFileHandler(string xmlfilename)
    {
      filename = xmlfilename;
      document = new XmlDocument();
      if (File.Exists(filename))
      {
        XmlTextReader reader = new XmlTextReader(filename);
        document.Load(reader);
        if (document.DocumentElement == null)
        {
          document = null;
        }
        reader.Close();
      }
      else if (File.Exists(filename + ".bak"))
      {
        XmlTextReader reader = new XmlTextReader(filename + ".bak");
        document.Load(reader);
        if (document.DocumentElement == null)
        {
          document = null;
        }
        reader.Close();
      }
      if (document == null)
      {
        document = new XmlDocument();
      }
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Read only , gets the Xml filename
    /// </summary>
    public string FileName
    {
      get { return filename; }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads the value of a given entry in a given section
    /// </summary>
    /// <param name="section">the section name</param>
    /// <param name="entry">the entry name</param>
    /// <param name="scope">the setting's scope : User or Global</param>
    /// <returns></returns>
    public string GetValue(string section, string entry)
    {
      if (document == null)
      {
        return null;
      }

      XmlElement root = document.DocumentElement;
      if (root == null)
      {
        return null;
      }
      XmlNode entryNode;
      entryNode = root.SelectSingleNode(GetSectionPath(section) + "/" + GetEntryPath(entry));
      if (entryNode == null)
      {
        return null;
      }
      return entryNode.InnerText;
    }

    /// <summary>
    /// Sets the value of a given entry in a given section
    /// </summary>
    /// <param name="section">the section name</param>
    /// <param name="entry">the entry name</param>
    /// <param name="scope">the setting's scope : User or Global</param>
    /// <returns></returns>
    public void SetValue(string section, string entry, string value)
    {
      // If the value is null, remove the entry
      if (value == null)
      {
        RemoveEntry(section, entry);
        return;
      }

      string valueString = value.ToString();

      if (document.DocumentElement == null)
      {
        XmlElement node = document.CreateElement("Configuration");
        document.AppendChild(node);
      }
      XmlElement root = document.DocumentElement;
      // Get the section element and add it if it's not there
      XmlNode sectionNode = root.SelectSingleNode("Section[@name=\"" + section + "\"]");
      if (sectionNode == null)
      {
        XmlElement element = document.CreateElement("Section");
        XmlAttribute attribute = document.CreateAttribute("name");
        attribute.Value = section;
        element.Attributes.Append(attribute);
        sectionNode = root.AppendChild(element);
      }
      // Get the section element and add it if it's not there
      XmlNode entryNode;
      entryNode = sectionNode.SelectSingleNode("Setting[@name=\"" + entry + "\"]");

      if (entryNode == null)
      {
        XmlElement element = document.CreateElement("Setting");
        XmlAttribute attribute = document.CreateAttribute("name");
        attribute.Value = entry;
        element.Attributes.Append(attribute);
        entryNode = sectionNode.AppendChild(element);
      }
      entryNode.InnerText = valueString;
      modified = true;
    }

    /// <summary>
    /// Saves any changes done in the xml document
    /// </summary>
    public void Save()
    {
      //      ILogger log = ServiceScope.Get<ILogger>();
      //log.Debug("Saving({0},{1})", filename, modified.ToString());
      if (!modified)
      {
        return;
      }
      // creates needed dirs if they don't exist
      Directory.CreateDirectory( String.Format(@"{0}\MediaPortal\", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
      Directory.CreateDirectory( String.Format(@"{0}\MediaPortal\Config\", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
      Directory.CreateDirectory( String.Format(@"{0}\MediaPortal\Config\{1}\", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),Environment.UserName));
      //log.Debug("Saving {0}", this.FileName);
      if (document == null)
      {
        return;
      }
      if (document.DocumentElement == null)
      {
        return;
      }
      if (document.ChildNodes.Count == 0)
      {
        return;
      }
      if (document.DocumentElement.ChildNodes == null)
      {
        return;
      }
      try
      {
        if (File.Exists(this.FileName + ".bak"))
        {
          File.Delete(this.FileName + ".bak");
        }
        if (File.Exists(this.FileName))
        {
          File.Move(this.FileName, this.FileName + ".bak");
        }
      }

      catch (Exception) {}

      using (StreamWriter stream = new StreamWriter(this.FileName, false))
      {
        document.Save(stream);
        stream.Flush();
        stream.Close();
      }
      modified = false;
    }

    /// <summary>
    /// removes an entry in the xml file
    /// </summary>
    /// <param name="section">section name</param>
    /// <param name="entry">entry name</param>
    public void RemoveEntry(string section, string entry)
    {
      //todo
    }

    #endregion
    
    #region Private methods

    /// <summary>
    /// Gets the fullpath of a section given its name
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    private string GetSectionPath(string section)
    {
      return "Section[@name=\"" + section + "\"]";
    }

    /// <summary>
    /// Gets the fullpath of an entry given its name
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    private string GetEntryPath(string entry)
    {
      return "Setting[@name=\"" + entry + "\"]";
    }

    #endregion
  }
}