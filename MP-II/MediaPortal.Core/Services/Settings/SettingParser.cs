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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;


namespace MediaPortal.Core.Services.Settings
{
  /// <summary>
  /// Class used to store or retrieve settings classes.
  /// </summary>
  internal class SettingParser
  {

    #region Enums

    /// <summary>
    /// Specifies the category of a Type.
    /// </summary>
    private enum ObjectType
    {
      /// <summary>
      /// Defines the object has an unknown type,
      /// and will be serialized as a string (ToString()).
      /// </summary>
      Unknow,
      /// <summary>
      /// Defines the object is a CLR type.
      /// Examples: string, boolean, integer, double, DateTime, ...
      /// </summary>
      CLR,
      /// <summary>
      /// Defines the object is an Array.
      /// The object can be casted to Array.
      /// </summary>
      Array
    }

    #endregion

    #region Variables

    /// <summary>
    /// Location of the global xml file.
    /// </summary>
    private string _pathGlobalXml;
    /// <summary>
    /// Location of the user specific xml file.
    /// </summary>
    private string _pathUserXml;
    /// <summary>
    /// Content of the global xml file.
    /// </summary>
    private string _globalXml;
    /// <summary>
    /// Content of the user specific xml file.
    /// </summary>
    private string _userXml;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ObjectParser.
    /// </summary>
    /// <param name="pathGlobalXml">Path to the xml file containing all global settings.</param>
    /// <param name="pathUserXml">Path to the xml file containing all user settings.</param>
    public SettingParser(string pathGlobalXml, string pathUserXml)
    {
      _pathGlobalXml = pathGlobalXml;
      _pathUserXml = pathUserXml;
      // _userXml and _globalXml are set by Serialize() and Deserialize()
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Serializes the public properties marked with <see cref="SettingAttribute"/> of the given
    /// <paramref name="settingsObject"/>.
    /// </summary>
    public void Serialize(object settingsObject)
    {
      ILogger log = ServiceScope.Get<ILogger>();
      // Make sure the xml is updated
      _userXml = ReadFileToString(_pathUserXml);
      _globalXml = ReadFileToString(_pathGlobalXml);
      // Initialize dictionaries to hold the <PropertyInfo.Name, value> as strings
      Dictionary<string, string> globalSettingsList = new Dictionary<string, string>();
      Dictionary<string, string> userSettingsList = new Dictionary<string, string>();
      // Enumerate through the properties, get their value, and assign them to the correct dictionary
      foreach (PropertyInfo property in settingsObject.GetType().GetProperties())
      {
        SettingAttribute att = GetSettingAttribute(property);   // Get the attribute so we know the SettingScope and the default value
        if (att == null) continue;
        string value = ""; // The value to add to the dictionary (with property.Name as the key)
        switch (GetObjectType(property.PropertyType)) // Get the type-category of the current property
        {
          case ObjectType.CLR:
            value = GetPropertyValueAsString(settingsObject, att.SettingScope, property, att.DefaultValue);
            break;
          default:              // Try to serialize
            try
            {
              XmlSerializer xmlSerial = new XmlSerializer(property.PropertyType);
              StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
              using (TextWriter strWriter = new StringWriter(sb))
              {
                using (XmlWriter writer = new XmlNoNamespaceWriter(strWriter))
                  xmlSerial.Serialize(writer, property.GetValue(settingsObject, null));
              }
              // Remove <?xml version="1.0" encoding="utf-8"?>
              int index = sb.ToString().IndexOf("?>");
              if (index != -1)
                sb.Remove(0, index + 2);
              value = sb.ToString();
            }
            catch (Exception ex)
            {
              value = null; // Don't save the setting
              log.Error("Can't serialize setting: [{0}] {1}.", ex, property.PropertyType.ToString(), property.Name);
            }
            break;
        }
        if (att.SettingScope == SettingScope.Global)
          globalSettingsList.Add(property.Name, value);
        else if (att.SettingScope == SettingScope.User)
          userSettingsList.Add(property.Name, value);
      }
      // Write the data, don't create empty files
      if (globalSettingsList.Count != 0)
        SaveSettings(settingsObject, globalSettingsList, new XmlFileHandler(_pathGlobalXml));
      if (userSettingsList.Count != 0)
        SaveSettings(settingsObject, userSettingsList, new XmlFileHandler(_pathUserXml));
    }

    /// <summary>
    /// Deserializes public properties of the settings object specified by its <paramref name="settingsType"/>
    /// from either the global or the user setting file, if present.
    /// During deserialization the xml file will be created if it doesn't exist already,
    /// and will also possibly be updated with new settings.
    /// </summary>
    public object Deserialize(Type settingsType)
    {
      object result = Activator.CreateInstance(settingsType);
      ILogger log = ServiceScope.Get<ILogger>();
      // Make sure the xml is updated
      _userXml = ReadFileToString(_pathUserXml);
      _globalXml = ReadFileToString(_pathGlobalXml);
      XmlFileHandler xmlGlobalReader = new XmlFileHandler(_pathGlobalXml);
      XmlFileHandler xmlUserReader = new XmlFileHandler(_pathUserXml);
      foreach (PropertyInfo property in result.GetType().GetProperties())
      {
        SettingAttribute att = GetSettingAttribute(property);
        if (att == null) continue;
        XmlFileHandler reader = (att.SettingScope == SettingScope.Global ? xmlGlobalReader : xmlUserReader);
        object value;
        string strValue = reader.GetValue(result.ToString(), property.Name);  // holds the properties-value
        switch (GetObjectType(property.PropertyType))
        {
          case ObjectType.CLR:
            try
            {
              value = (String.IsNullOrEmpty(strValue) ? att.DefaultValue : strValue);
              // If value is null and the type is a value type.
              // Or the type of the value is not the same as the type of the property.
              // -> We ask the type of the property for a converter that can convert our value to the correct type.
              if ((value == null && property.PropertyType.IsValueType) || (value != null && !property.PropertyType.Equals(value.GetType())))
              {
                TypeConverter conv = TypeDescriptor.GetConverter(property.PropertyType);
                if (value == null || conv.CanConvertFrom(value.GetType()))
                  value = conv.ConvertFrom(value);
              }
              property.SetValue(result, value, null);
            }
            catch (Exception ex)
            {
              log.Error("Error deserializing settings for property '{0}', settings type '{1}'", ex, property.Name, settingsType.FullName);
            }
            break;
          default:
            XmlSerializer xmlSerial = new XmlSerializer(property.PropertyType);
            if (!string.IsNullOrEmpty(strValue))
            {
              TextReader strReader = new StringReader(strValue);
              try
              {
                property.SetValue(result, xmlSerial.Deserialize(strReader), null);
              }
              catch (Exception ex)
              {
                log.Error("Error deserializing settings for property '{0}', settings type '{1}'", ex, property.Name, settingsType.FullName);
              }
            }
            break;
        }
      }
      return result;
    }

    #endregion

    #region Privates methods

    /// <summary>
    /// Gets the Type as an ObjectType.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private ObjectType GetObjectType(Type type)
    {
      if (type == typeof(Array)
        || type == typeof(bool[])
        || type == typeof(string[])
        || type == typeof(byte[])
        || type == typeof(short[])
        || type == typeof(int[])
        || type == typeof(long[])
        || type == typeof(float[])
        || type == typeof(double[]))
        return ObjectType.Array;

      if ((type == typeof(int))
        || (type == typeof(string))
        || (type == typeof(bool))
        || (type == typeof(float))
        || (type == typeof(double))
        || (type == typeof(UInt32))
        || (type == typeof(UInt64))
        || (type == typeof(UInt16))
        || (type == typeof(DateTime))
        || (type == typeof(bool?))
        || (type == typeof(float?))
        || (type == typeof(double?))
        || (type == typeof(UInt32?))
        || (type == typeof(UInt64?))
        || (type == typeof(UInt16?))
        || (type == typeof(Int32?))
        || (type == typeof(Int64?))
        || (type == typeof(Int16?)))
        return ObjectType.CLR;

      return ObjectType.Unknow;
    }

    /// <summary>
    /// Returns the content of a file as one string.
    /// If the file can't be read "" will be returned.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string ReadFileToString(string path)
    {
      if (File.Exists(path))
      {
        try
        {
          return File.ReadAllText(path);
        }
        catch
        {
          return "";
        }
      }
      return "";
    }

    /// <summary>
    /// Gets the SettingAttribute of a property.
    /// Returns null if the attribute can't be found.
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    private SettingAttribute GetSettingAttribute(PropertyInfo property)
    {
      // Get the info stored in the SettingAttribute, if available
      object[] attributes = property.GetCustomAttributes(typeof(SettingAttribute), false);
      if (attributes.Length != 0)
        return (SettingAttribute)attributes[0];
      return null;
    }

    /// <summary>
    /// Gets the value of the given property as a string.
    /// </summary>
    /// <param name="obj">Object to load property from.</param>
    /// <param name="property">Property to load from object.</param>
    /// <param name="defaultValue">Default value of the property, will be returned if property can't be loaded.</param>
    /// <returns></returns>
    private string GetPropertyValueAsString(object obj, SettingScope scope, PropertyInfo property, object defaultValue)
    {
      // Check if we should try to return the property value. (the property must be an element of the old xml, else we must return the default)
      if ((scope == SettingScope.Global && !_globalXml.Contains("<" + property.Name))
        || (scope == SettingScope.User && !_userXml.Contains("<" + property.Name)))
      {
        object propValue = property.GetValue(obj, null);
        if (propValue != null)
          return propValue.ToString();
      }
      // Try to return default value
      if (defaultValue != null)
        return defaultValue.ToString();
      return "";
    }

    /// <summary>
    /// Writes and saves a collection of settings keys/values to xml.
    /// </summary>
    /// <param name="obj">Settings class instance</param>
    /// <param name="SettingsList">a Dictionary(string, string) list containing the keys/values to write</param>
    /// <param name="xmlWriter">XmlFileHandler instance</param>
    private void SaveSettings(object obj, IEnumerable<KeyValuePair<string, string>> SettingsList, XmlFileHandler xmlWriter)
    {
      XmlDocument doc = new XmlDocument();
      foreach (KeyValuePair<string, string> pair in SettingsList)
      {
        xmlWriter.SetValue(obj.ToString(), pair.Key, pair.Value);
      }
      xmlWriter.Save();
    }

    #endregion

  }
}
