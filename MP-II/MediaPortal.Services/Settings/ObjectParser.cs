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

#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Settings;

namespace MediaPortal.Services.Settings
{
  /// <summary>
  /// Static Class used to store or retrieve settings classes
  /// </summary>
  internal class ObjectParser
  {
    private const string ERRORMESSAGE = "Error deserializing settings for property {0} object {1}";

    #region Publics methods

    /// <summary>
    /// Serialize public properties of a Settings object to a given xml file
    /// </summary>
    /// <param name="obj">Settings Object to serialize</param>
    /// <param name="saveGlobalSettings">if set to false, global settings won't be saved</param>
    /// <param name="saveUserSettings">if set to false, user settings won't be saved</param>
    public static void Serialize(object obj, bool saveGlobalSettings, bool saveUserSettings)
    {
      string fileName = getFilename(obj);
      //
      Dictionary<string, string> globalSettingsList = new Dictionary<string, string>();
      Dictionary<string, string> userSettingsList = new Dictionary<string, string>();
      //
      string fullUserFileName = getFullUserFilename(fileName);
      string fullFileName = getFullGlobalFilename(fileName);
      //
      XmlFileHandler xmlWriter = new XmlFileHandler(fullFileName);
      XmlFileHandler xmlUserWriter = new XmlFileHandler(fullUserFileName);
      bool isFirstSave = (!File.Exists(fullFileName));
      bool isUserFirstSave = (!File.Exists(fullUserFileName));
      foreach (PropertyInfo property in obj.GetType().GetProperties())
      {
        Type thisType = property.PropertyType;
        string defaultval="";

        #region CLR Typed property

        if (isCLRType(thisType))
        {
          object[] attributes = property.GetCustomAttributes(typeof(SettingAttribute), false);
          SettingScope scope;
          if (attributes.Length != 0)
          {
            SettingAttribute attribute = (SettingAttribute)attributes[0];
            scope = attribute.SettingScope;
            if (attribute.DefaultValue != null)
              defaultval = attribute.DefaultValue.ToString();
          }
          else
          {
            scope = SettingScope.Global;
            defaultval = "";
          }
          string value = defaultval;

          if ((scope == SettingScope.Global && !isFirstSave) || (scope == SettingScope.User && !isUserFirstSave))
          //else default value will be used if it exists
          {
            if (obj.GetType().GetProperty(property.Name).GetValue(obj, null) != null)
            {
              value = obj.GetType().GetProperty(property.Name).GetValue(obj, null).ToString();
            }
            if (scope == SettingScope.User)
            {
              userSettingsList.Add(property.Name, value);
            }
            else
            {
              globalSettingsList.Add(property.Name, value);
            }
          }
          else
          {
            if (scope == SettingScope.Global)
            {
              globalSettingsList.Add(property.Name, value);
            }
            if (scope == SettingScope.User)
            {
              userSettingsList.Add(property.Name, value);
            }
          }
        }
        #endregion

        #region not CLR Typed property

        else
        {
          XmlSerializer xmlSerial = new XmlSerializer(thisType);
          StringBuilder sb = new StringBuilder();
          StringWriter strWriter = new StringWriter(sb);
          XmlTextWriter writer = new XmlNoNamespaceWriter(strWriter);
          writer.Formatting = Formatting.Indented;
          object propertyValue = obj.GetType().GetProperty(property.Name).GetValue(obj, null);
          xmlSerial.Serialize(writer, propertyValue);
          strWriter.Close();
          strWriter.Dispose();
          // remove unneeded encoding tag
          sb.Remove(0, 41);
          object[] attributes = property.GetCustomAttributes(typeof(SettingAttribute), false);
          SettingScope scope;
          if (attributes.Length != 0)
          {
            SettingAttribute attribute = (SettingAttribute)attributes[0];
            scope = attribute.SettingScope;
            if (attribute.DefaultValue == null)
            {
              defaultval = null;
            }
            else
            {
              defaultval = attribute.DefaultValue.ToString();
            }
          }
          else
          {
            scope = SettingScope.Global;
            defaultval = "";
          }
          string value = defaultval;
          /// a changer
          if ((scope == SettingScope.Global && !isFirstSave) || (scope == SettingScope.User && !isUserFirstSave))
          {
            value = sb.ToString();
          }
          if (scope == SettingScope.User)
          {
            userSettingsList.Add(property.Name, value);
          }
          else
          {
            globalSettingsList.Add(property.Name, value);
          }
        }

        #endregion
      }

      #region write Settings

      // write settings to xml
      if (saveGlobalSettings) saveSettings(obj, globalSettingsList, xmlWriter);
      if (saveUserSettings) saveSettings(obj, userSettingsList, xmlUserWriter);

      #endregion
    }

    /// <summary>
    /// De-serialize public properties of a Settings object from a given xml file
    /// </summary>
    /// <param name="obj">Setting Object to retrieve</param>
    public static void Deserialize(object obj)
    {
      ILogger log = ServiceScope.Get<ILogger>();
      string fileName;
      INamedSettings namedSettings = obj as INamedSettings;
      if (namedSettings != null)
      {
        fileName = obj + "." + namedSettings.Name + ".xml";
      }
      else
      {
        fileName = obj + ".xml";
      }
      //log.Debug("Deserialize({0},{1})", obj.ToString(), fileName);
      // if xml file doesn't exist yet then create it
      string fullFileName = getFullGlobalFilename(fileName);
      string fullUserFileName = getFullUserFilename(fileName);
      XmlFileHandler xmlReader = new XmlFileHandler(fullFileName);
      XmlFileHandler xmlUserReader = new XmlFileHandler(fullUserFileName);
      if (!File.Exists(fullFileName) || !File.Exists(fullUserFileName))
      {
        Serialize(obj, !File.Exists(fullFileName), !File.Exists(fullUserFileName));
      }
      foreach (PropertyInfo property in obj.GetType().GetProperties())
      {
        Type thisType = property.PropertyType;

        #region get scope

        SettingScope scope;
        object[] attributes = property.GetCustomAttributes(typeof(SettingAttribute), false);
        object defaultval;
        if (attributes.Length != 0)
        {
          SettingAttribute attribute = (SettingAttribute)attributes[0];
          scope = attribute.SettingScope;
          if (attribute.DefaultValue == null)
          {
            defaultval = null;
          }
          else
          {
            defaultval = attribute.DefaultValue;
          }
        }
        else
        {
          scope = SettingScope.Global;
          defaultval = null;
        }

        #endregion

        if (isCLRType(thisType))

        #region CLR Typed property

        {
          try
          {
            string readValue;
            if (scope == SettingScope.Global)
            {
              readValue = xmlReader.GetValue(obj.ToString(), property.Name);
            }
            else
            {
              readValue = xmlUserReader.GetValue(obj.ToString(), property.Name);
            }
            object value;
            if (String.IsNullOrEmpty(readValue))
            {
              value = defaultval;
            }
            else
            {
              value = readValue;
            }
            //if value is null and the type is a value type
            //or the type of the value is not the same as the type of the property
            //we ask the type of the property for a converter that can convert our value to the correct type
            if ((value == null && thisType.IsValueType) || (value != null && !thisType.Equals(value.GetType())))
            {
              TypeConverter conv = TypeDescriptor.GetConverter(thisType);
              if (value == null || conv.CanConvertFrom(value.GetType()))
              {
                value = conv.ConvertFrom(value);
              }
            }
            property.SetValue(obj, value, null);
          }
          catch (Exception ex)
          {
            log.Error(ERRORMESSAGE, ex, property.Name, namedSettings == null ? "?" : namedSettings.Name);
          }
        }
        #endregion

        else
        #region not CLR Typed property

        {
          XmlSerializer xmlSerial = new XmlSerializer(thisType);
          string value;
          if (scope == SettingScope.Global)
          {
            value = xmlReader.GetValue(obj.ToString(), property.Name);
          }
          else
          {
            value = xmlUserReader.GetValue(obj.ToString(), property.Name);
          }
          if (value != null && value !="")
          {
            TextReader reader = new StringReader(value);
            try
            {
              property.SetValue(obj, xmlSerial.Deserialize(reader), null);
            }
            catch (Exception ex)
            {
              log.Error(ERRORMESSAGE, ex, property.Name, namedSettings == null ? "?" : namedSettings.Name);
            }
          }
        }

        #endregion
      }
    }

    /// <summary>
    /// Detects if the current property type is or not a CLR type
    /// </summary>
    /// <param name="aType">property type</param>
    /// <returns>true: CLR Type , false: guess what</returns>
    public static bool isCLRType(Type aType)
    {
      if ((aType == typeof(int))
          || (aType == typeof(string))
          || (aType == typeof(bool))
          || (aType == typeof(float))
          || (aType == typeof(double))
          || (aType == typeof(UInt32))
          || (aType == typeof(UInt64))
          || (aType == typeof(UInt16))
          || (aType == typeof(DateTime))
        //|| (aType == typeof(string?))
          || (aType == typeof(bool?))
          || (aType == typeof(float?))
          || (aType == typeof(double?))
          || (aType == typeof(UInt32?))
          || (aType == typeof(UInt64?))
          || (aType == typeof(UInt16?))
          || (aType == typeof(Int32?))
          || (aType == typeof(Int64?))
          || (aType == typeof(Int16?)))
      {
        return true;
      }
      return false;
    }

    #endregion

    #region Privates methods

    /// <summary>
    /// returns full filename including config path and user subdir
    /// </summary>
    /// <param name="fileName">file's name</param>
    /// <returns></returns>
    private static string getFullUserFilename(string fileName)
    {
      string fullUserFileName = 
        String.Format(@"<CONFIG>\{0}\{1}", Environment.UserName, fileName);     
      return ServiceScope.Get<IPathManager>().GetPath(fullUserFileName);
    }

    /// <summary>
    /// returns full filename including config path 
    /// </summary>
    /// <param name="fileName">file's name</param>
    /// <returns></returns>
    private static string getFullGlobalFilename(string fileName)
    {
      string fullFileName = String.Format(@"<CONFIG>\{0}", fileName);
      return ServiceScope.Get<IPathManager>().GetPath(fullFileName);
    }

    /// <summary>
    /// returns a filename based on an setting class name
    /// additionnaly appends an extension to the filename
    /// if the settings class implements INamesSettings interface
    /// see doc for more infos on INamedSettings
    /// </summary>
    /// <param name="obj">settings instance to name</param>
    /// <returns></returns>
    private static string getFilename(object obj)
    {
      string fileName;
      INamedSettings namedSettings = obj as INamedSettings;
      if (namedSettings != null)
      {
        fileName = obj + "." + namedSettings.Name + ".xml";
      }
      else
      {
        fileName = obj + ".xml";
      }
      return fileName;
    }

    /// <summary>
    /// Writes and saves a collection of settings keys/values to xml
    /// </summary>
    /// <param name="obj">Settings class instance</param>
    /// <param name="SettingsList">a Dictionary<string, string> list containing the keys/values to write</param>
    /// <param name="xmlWriter">XmlFileHandler instance</param>
    private static void saveSettings(object obj, IEnumerable<KeyValuePair<string, string>> SettingsList,
                                     XmlFileHandler xmlWriter)
    {
      foreach (KeyValuePair<string, string> pair in SettingsList)
      {
        xmlWriter.SetValue(obj.ToString(), pair.Key, pair.Value);
      }
      xmlWriter.Save();
    }

    #endregion
  }
}
