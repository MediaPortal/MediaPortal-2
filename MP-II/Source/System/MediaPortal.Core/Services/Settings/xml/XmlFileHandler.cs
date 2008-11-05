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

namespace MediaPortal.Core.Services.Settings
{
  public class XmlFileHandler
  {

    #region Variables

    /// <summary>
    /// Document to write to.
    /// </summary>
    private XmlDocument _document;
    /// <summary>
    /// Keeps track of modifications.
    /// </summary>
    private bool _modified;
    /// <summary>
    /// Physical location of the xml file.
    /// </summary>
    private string _filename;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the Xml filename.
    /// </summary>
    public string FileName
    {
      get { return _filename; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of XmlFileHandler.
    /// </summary>
    /// <param name="xmlfilename">The xml file to read from and/or write into.</param>
    public XmlFileHandler(string xmlfilename)
    {
      _filename = xmlfilename;
      _document = new XmlDocument();
      _modified = false;
      try
      {
        if (File.Exists(_filename))               // try to get the file
        {
          using (XmlTextReader reader = new XmlTextReader(_filename))
          {
            _document.Load(reader);
            if (_document.DocumentElement == null)
              _document = null;
          }
        }
        else if (File.Exists(_filename + ".bak")) // try to get the backup
        {
          using (XmlTextReader reader = new XmlTextReader(_filename + ".bak"))
          {
            _document.Load(reader);
            if (_document.DocumentElement == null)
              _document = null;
          }
        }
      }
      catch
      {
        // should we log?
      }
      finally
      {
        if (_document == null)
          _document = new XmlDocument();
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads the value of a given entry in a given section
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="entry">The entry name.</param>
    /// <returns></returns>
    public string GetValue(string section, string entry)
    {
      if (_document == null) return null;
      XmlElement root = _document.DocumentElement;
      if (root == null) return null;
      XmlNode entryNode = root.SelectSingleNode(GetSectionPath(section) + "/" + GetEntryPath(entry));
      if (entryNode == null) return null;
      return entryNode.InnerText;
    }

    /// <summary>
    /// Sets the value of a given entry in a given section.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="entry">The entry name.</param>
    /// <param name="value">Value to set.</param>
    public void SetValue(string section, string entry, string value)
    {
      // If the value is null, remove the entry
      if (value == null)
      {
        RemoveEntry(section, entry);
        return;
      }
      // Make sure the root of the XML document tree is set.
      if (_document.DocumentElement == null)
      {
        XmlElement node = _document.CreateElement("Configuration");
        _document.AppendChild(node);
      }
      XmlElement root = _document.DocumentElement;
      // Get the section element and add it if it's not there
      XmlNode sectionNode = root.SelectSingleNode("Section[@name=\"" + section + "\"]");
      if (sectionNode == null)
      {
        XmlElement element = _document.CreateElement("Section");
        XmlAttribute attribute = _document.CreateAttribute("name");
        attribute.Value = section;
        element.Attributes.Append(attribute);
        sectionNode = root.AppendChild(element);
      }
      // Get the section element and add it if it's not there
      XmlNode entryNode;
      entryNode = sectionNode.SelectSingleNode("Setting[@name=\"" + entry + "\"]");

      if (entryNode == null)
      {
        XmlElement element = _document.CreateElement("Setting");
        XmlAttribute attribute = _document.CreateAttribute("name");
        attribute.Value = entry;
        element.Attributes.Append(attribute);
        entryNode = sectionNode.AppendChild(element);
      }
      entryNode.InnerText = value;
      _modified = true;
    }

    /// <summary>
    /// Saves any changes done in the xml document
    /// </summary>
    public void Save()
    {
      if (!_modified
        || _document == null
        || _document.DocumentElement == null
        || _document.ChildNodes.Count == 0
        || _document.DocumentElement.ChildNodes == null)
      {
        return;
      }
      // Create needed directories if they don't exist
      FileInfo configFile = new FileInfo(_filename);
      if (!configFile.Directory.Exists)
        configFile.Directory.Create();
      // Try to take a backup
      TakeBackup(_filename);
      // Write the file
      using (StreamWriter stream = new StreamWriter(_filename, false))
      {
        _document.Save(stream);
        stream.Flush();
      }
      _modified = false;
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

    private void TakeBackup(string fileName)
    {
      try
      {
        if (File.Exists(fileName + ".bak"))
        {
          try
          {
            File.Delete(fileName + ".bak");
          }
          catch (Exception) { }
        }
        if (File.Exists(fileName))
        {
          try
          {
            File.Move(fileName, fileName + ".bak");
          }
          catch (Exception) { }
        }
      }
      catch (Exception) { }
    }

    #endregion

  }
}
