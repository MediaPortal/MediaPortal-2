#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 *  Code modified from SharpDevelop AddIn code
 *  Thanks goes to: Mike Krüger
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace MediaPortal.Services.PluginManager.PluginDetails
{
  /// <summary>
  /// Description of PropertyGroup.
  /// </summary>
  internal class PluginProperties
  {
    #region Variables
    Dictionary<string, object> _properties = new Dictionary<string, object>();
    #endregion

    #region Constructors/Destructors
    public PluginProperties()
    {
    }
    #endregion

    #region Properties
    public string this[string property]
    {
      get
      {
        return Convert.ToString(Get(property));
      }
      set
      {
        Set(property, value);
      }
    }

    public string[] Elements
    {
      get
      {
        List<string> ret = new List<string>();
        foreach (KeyValuePair<string, object> property in _properties)
          ret.Add(property.Key);
        return ret.ToArray();
      }
    }

    public int Count
    {
      get
      {
        return _properties.Count;
      }
    }
    #endregion

    #region Public Methods
    public object Get(string property)
    {
      if (!_properties.ContainsKey(property))
      {
        return null;
      }
      return _properties[property];
    }

    public void Set<T>(string property, T value)
    {
      T oldValue = default(T);
      if (!_properties.ContainsKey(property))
      {
        _properties.Add(property, value);
      }
      else
      {
        oldValue = Get<T>(property, value);
        _properties[property] = value;
      }
      //OnPropertyChanged(new PropertyChangedEventArgs(this, property, oldValue, value));
    }

    public bool Contains(string property)
    {
      return _properties.ContainsKey(property);
    }

    public bool Remove(string property)
    {
      return _properties.Remove(property);
    }

    public void ReadProperties(XmlReader reader, string endElement)
    {
      if (reader.IsEmptyElement)
      {
        return;
      }
      while (reader.Read())
      {
        switch (reader.NodeType)
        {
          case XmlNodeType.EndElement:
            if (reader.LocalName == endElement)
            {
              return;
            }
            break;
          case XmlNodeType.Element:
            string propertyName = reader.LocalName;
            if (propertyName == "Properties")
            {
              propertyName = reader.GetAttribute(0);
              PluginProperties p = new PluginProperties();
              p.ReadProperties(reader, "Properties");
              _properties[propertyName] = p;
            }
            else if (propertyName == "Array")
            {
              propertyName = reader.GetAttribute(0);
              _properties[propertyName] = ReadArray(reader);
            }
            else
            {
              _properties[propertyName] = reader.HasAttributes ? reader.GetAttribute(0) : null;
            }
            break;
        }
      }
    }

    public void WriteProperties(XmlWriter writer)
    {
      foreach (KeyValuePair<string, object> entry in _properties)
      {
        object val = entry.Value;
        if (val is PluginProperties)
        {
          writer.WriteStartElement("Properties");
          writer.WriteAttributeString("name", entry.Key);
          ((PluginProperties)val).WriteProperties(writer);
          writer.WriteEndElement();
        }
        else if (val is Array || val is ArrayList)
        {
          writer.WriteStartElement("Array");
          writer.WriteAttributeString("name", entry.Key);
          foreach (object o in (IEnumerable)val)
          {
            writer.WriteStartElement("Element");
            WriteValue(writer, o);
            writer.WriteEndElement();
          }
          writer.WriteEndElement();
        }
        else
        {
          writer.WriteStartElement(entry.Key);
          WriteValue(writer, val);
          writer.WriteEndElement();
        }
      }
    }

    public void Save(string fileName)
    {
      using (XmlTextWriter writer = new XmlTextWriter(fileName, Encoding.UTF8))
      {
        writer.Formatting = Formatting.Indented;
        writer.WriteStartElement("Properties");
        WriteProperties(writer);
        writer.WriteEndElement();
      }
    }

    public T Get<T>(string property, T defaultValue)
    {
      if (!_properties.ContainsKey(property))
      {
        _properties.Add(property, defaultValue);
        return defaultValue;
      }
      object o = _properties[property];

      if (o is string && typeof(T) != typeof(string))
      {
        TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
        try
        {
          o = c.ConvertFromInvariantString(o.ToString());
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Warn("Error loading property '" + property + "': " + ex.Message);
          o = defaultValue;
        }
        _properties[property] = o; // store for future look up
      }
      else if (o is ArrayList && typeof(T).IsArray)
      {
        ArrayList list = (ArrayList)o;
        Type elementType = typeof(T).GetElementType();
        Array arr = System.Array.CreateInstance(elementType, list.Count);
        TypeConverter c = TypeDescriptor.GetConverter(elementType);
        try
        {
          for (int i = 0; i < arr.Length; ++i)
          {
            if (list[i] != null)
            {
              arr.SetValue(c.ConvertFromInvariantString(list[i].ToString()), i);
            }
          }
          o = arr;
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Warn("Error loading property '" + property + "': " + ex.Message);
          o = defaultValue;
        }
        _properties[property] = o; // store for future look up
      }
      else if (!(o is string) && typeof(T) == typeof(string))
      {
        TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
        if (c.CanConvertTo(typeof(string)))
        {
          o = c.ConvertToInvariantString(o);
        }
        else
        {
          o = o.ToString();
        }
      }
      try
      {
        return (T)o;
      }
      catch (NullReferenceException)
      {
        // can happen when configuration is invalid -> o is null and a value type is expected
        return defaultValue;
      }
    }

    //protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    //{
    //  if (PropertyChanged != null) {
    //    PropertyChanged(this, e);
    //  }
    //}

    //public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    #region Public static Methods
    public static PluginProperties Load(string fileName)
    {
      if (!File.Exists(fileName))
      {
        return null;
      }
      using (XmlTextReader reader = new XmlTextReader(fileName))
      {
        while (reader.Read())
        {
          if (reader.IsStartElement())
          {
            switch (reader.LocalName)
            {
              case "Properties":
                PluginProperties properties = new PluginProperties();
                properties.ReadProperties(reader, "Properties");
                return properties;
            }
          }
        }
      }
      return null;
    }

    public static PluginProperties ReadFromAttributes(XmlReader reader)
    {
      PluginProperties properties = new PluginProperties();
      if (reader.HasAttributes)
      {
        for (int i = 0; i < reader.AttributeCount; i++)
        {
          reader.MoveToAttribute(i);
          properties[reader.Name] = reader.Value;
        }
        reader.MoveToElement(); //Moves the reader back to the element node.
      }
      return properties;
    }
    #endregion

    #region Private Methods
    private ArrayList ReadArray(XmlReader reader)
    {
      if (reader.IsEmptyElement)
        return new ArrayList(0);
      ArrayList l = new ArrayList();
      while (reader.Read())
      {
        switch (reader.NodeType)
        {
          case XmlNodeType.EndElement:
            if (reader.LocalName == "Array")
            {
              return l;
            }
            break;
          case XmlNodeType.Element:
            l.Add(reader.HasAttributes ? reader.GetAttribute(0) : null);
            break;
        }
      }
      return l;
    }

    private void WriteValue(XmlWriter writer, object val)
    {
      if (val != null)
      {
        if (val is string)
        {
          writer.WriteAttributeString("value", val.ToString());
        }
        else
        {
          TypeConverter c = TypeDescriptor.GetConverter(val.GetType());
          writer.WriteAttributeString("value", c.ConvertToInvariantString(val));
        }
      }
    }
    #endregion

    #region <Base class> Overloads
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("[Properties:{");
      foreach (KeyValuePair<string, object> entry in _properties)
      {
        sb.Append(entry.Key);
        sb.Append("=");
        sb.Append(entry.Value);
        sb.Append(",");
      }
      sb.Append("}]");
      return sb.ToString();
    }
    #endregion
  }
}
