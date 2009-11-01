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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Core.Services.Settings
{
  public class SettingsFileHandler
  {
    #region Protected fields

    protected static IDictionary<Type, XmlSerializer> _xmlSerializers = new Dictionary<Type, XmlSerializer>();
    /// <summary>
    /// XML document to write to.
    /// </summary>
    protected XmlDocument _document;

    /// <summary>
    /// Keeps track of modifications.
    /// </summary>
    protected bool _modified;

    /// <summary>
    /// File path of the physical XML file.
    /// </summary>
    protected string _filePath;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the path of the physical XML file.
    /// </summary>
    public string FilePath
    {
      get { return _filePath; }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new XML settings file handler instance.
    /// </summary>
    /// <param name="filePath">Path of the file to read from and/or write into.</param>
    public SettingsFileHandler(string filePath)
    {
      _filePath = filePath;
      Clear();
    }

    #endregion

    #region Protected methods

    protected void CreateRootElement()
    {
      XmlElement root = _document.CreateElement("Configuration");
      _document.AppendChild(root);
    }

    protected XmlElement GetPropertyElement(string entryName, bool createIfNotExists)
    {
      XmlElement root = _document.DocumentElement;
      if (root == null)
        if (!createIfNotExists)
          return null;
        else
          CreateRootElement();
      XmlElement entryElement = root.SelectSingleNode("Property[@Name=\"" + entryName + "\"]") as XmlElement;
      if (entryElement == null)
      {
        if (!createIfNotExists)
          return null;
        entryElement = _document.CreateElement("Property");
        XmlAttribute attribute = _document.CreateAttribute("Name");
        attribute.Value = entryName;
        entryElement.Attributes.Append(attribute);
        entryElement = root.AppendChild(entryElement) as XmlElement;
      }
      return entryElement;
    }

    protected static void TakeBackup(string filePath)
    {
      if (File.Exists(filePath + ".bak"))
        try
        {
          File.Delete(filePath + ".bak");
        }
        catch (Exception) { }
      if (File.Exists(filePath))
        try
        {
          File.Move(filePath, filePath + ".bak");
        }
        catch (Exception) { }
    }

    protected static XmlSerializer GetSerializer(Type type)
    {
      XmlSerializer result;
      if (_xmlSerializers.ContainsKey(type))
        result = _xmlSerializers[type];
      else
      {
        result = new XmlSerializer(type);
        _xmlSerializers[type] = result;
      }
      return result;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads the value of a given entry.
    /// </summary>
    /// <param name="entryName">The entry name.</param>
    /// <param name="type">Type of the value to retrieve.</param>
    /// <returns>Value of the specified entry.</returns>
    public object GetValue(string entryName, Type type)
    {
      XmlNode entryElement = GetPropertyElement(entryName, false);
      if (entryElement == null)
        return null;
      XmlSerializer xs = GetSerializer(type);
      using (StringReader reader = new StringReader(entryElement.InnerXml))
        return xs.Deserialize(reader);
    }

    /// <summary>
    /// Sets the value of a given entry.
    /// </summary>
    /// <param name="entryName">The entry name.</param>
    /// <param name="value">Value to set.</param>
    public void SetValue(string entryName, object value)
    {
      // If the value is null, remove the entry
      if (value == null)
      {
        RemoveEntry(entryName);
        _modified = true;
        return;
      }
      XmlNode entryElement = GetPropertyElement(entryName, true);
      XmlSerializer xs = GetSerializer(value.GetType());
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = new XmlInnerElementWriter(sb))
        xs.Serialize(writer, value);
      entryElement.InnerXml = sb.ToString();
      _modified = true;
    }

    /// <summary>
    /// Removes the value with the specified <paramref name="entryName"/>.
    /// </summary>
    /// <param name="entryName">Name of the entry to remove.</param>
    /// <returns><c>true</c> if the entry was removed, else <c>false</c>.</returns>
    public bool RemoveEntry(string entryName)
    {
      XmlNode entryElement = GetPropertyElement(entryName, false);
      if (entryElement == null)
        return false;
      entryElement.ParentNode.RemoveChild(entryElement);
      return true;
    }

    /// <summary>
    /// Loads the settings file.
    /// </summary>
    public void Load()
    {
      XmlTextReader reader = null;
      if (File.Exists(_filePath))
        // Try to get the file
        reader = new XmlTextReader(_filePath);
      else if (File.Exists(_filePath + ".bak"))
        // Try to get the backup
        reader = new XmlTextReader(_filePath + ".bak");
      if (reader != null)
        using (reader)
          _document.Load(reader);
    }

    public void Clear()
    {
      _document = new XmlDocument();
      CreateRootElement();
      _modified = false;
    }

    /// <summary>
    /// Saves any changes.
    /// </summary>
    public void Flush()
    {
      if (!_modified)
        return;
      // Create needed directories if they don't exist
      DirectoryInfo configDir = new DirectoryInfo(Path.GetDirectoryName(_filePath));
      if (!configDir.Exists)
        configDir.Create();
      // Try to take a backup
      TakeBackup(_filePath);
      // Write the file
      using (StreamWriter stream = new StreamWriter(_filePath, false))
      {
        _document.Save(stream);
        stream.Flush();
      }
      _modified = false;
    }

    public void Close()
    {
      Flush();
    }

    #endregion
  }
}
