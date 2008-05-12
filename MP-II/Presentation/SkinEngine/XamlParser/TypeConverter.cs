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
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Presentation.SkinEngine.XamlParser
{
  /// <summary>
  /// Describes the signature a type converter has to provide.
  /// </summary>
  /// <param name="val">The object to be converted.</param>
  /// <param name="targetType">The type to which the object should be converted.</param>
  /// <param name="result">Object of the target type or of a descending type, if the return
  /// value is <c>true</c>.</param>
  /// <returns>The method returns <c>true</c>, if the conversion was successful. In this case,
  /// the returned <paramref name="result"/> has the specified <paramref name="targetType"/>.
  /// It returns <c>false</c>, if the converter was not able to convert the specified
  /// value. In this case, result has an undefined value.</returns>
  public delegate bool ConvertTypeDlgt(object val, Type targetType,
      out object result);

  /// <summary>
  /// Static registration class for a type converter.
  /// </summary>
  public class TypeConverter
  {
    protected static readonly NumberFormatInfo NUMBERFORMATINFO = CultureInfo.InvariantCulture.NumberFormat;

    protected static ConvertTypeDlgt _customTypeConverter;

    public static ConvertTypeDlgt CustomTypeConverter
    {
      get { return _customTypeConverter; }
      set { _customTypeConverter = value; }
    }

    /// <summary>
    /// Converts the specified <paramref name="obj"/> to a collection.
    /// If the object supports the interface <see cref="ICollection"/> itself,
    /// it will simply be casted. If it is an <see cref="IEnumerable"/>, its contents
    /// will be copied into a new object implementing <see cref="ICollection"/>.
    /// Else, a new <see cref="ICollection"/> will be created with <paramref name="obj"/>
    /// as single contents.
    /// </summary>
    /// <param name="obj">The object to convert to a collection.</param>
    /// <returns>Collection containing the contents of obj or obj itself.</returns>
    protected static ICollection ToCollection(object obj)
    {
      if (obj is IInclude)
        obj = ((IInclude)obj).Content;
      if (obj is ICollection)
        return (ICollection)obj;
      else if (obj is IEnumerable)
      {
        IList<object> result = new List<object>();
        foreach (object o in (IEnumerable)obj)
          result.Add(o);
        return (ICollection)result;
      }
      else
      {
        IList<object> result = new List<object>();
        result.Add(obj);
        return (ICollection)result;
      }
    }

    public static bool Convert(object val, Type targetType, out object result)
    {
      result = val;
      // Trivial cases
      if (val == null)
        return true;
      if (targetType.IsAssignableFrom(val.GetType()))
        return true;

      // Custom type conversions

      if (_customTypeConverter != null)
      {
        if (_customTypeConverter(val, targetType, out result))
          return true;
      }

      if (targetType == typeof(double)) // == typeof(Double)
      {
        double resultDouble;
        if (double.TryParse(val.ToString(), NumberStyles.Any, NUMBERFORMATINFO, out resultDouble))
        {
          result = resultDouble;
          return true;
        }
        else
          return false;
      }
      if (targetType == typeof(float))
      {
        float resultFloat;
        if (float.TryParse(val.ToString(), NumberStyles.Any, NUMBERFORMATINFO, out resultFloat))
        {
          result = resultFloat;
          return true;
        }
        else
          return false;
      }

      // Built-in type conversions

      if (typeof(string).IsAssignableFrom(val.GetType()) && targetType == typeof(Type))
      { // string -> Type
        result = Type.GetType(val.ToString());
        return true;
      }

      // Enumerations
      if (typeof(string).IsAssignableFrom(val.GetType()) && targetType.IsEnum)
      { // string -> Enum
        FieldInfo fi = targetType.GetField(val.ToString(), BindingFlags.Public | BindingFlags.Static);
        result = fi.GetValue(null);
        return true;
      }

      // Collection types

      if (typeof(ICollection).IsAssignableFrom(targetType)) // Targets IList & ICollection
      {
        IList<object> resultList = new List<object>();
        foreach (object entry in ToCollection(val))
          resultList.Add(entry);
        result = resultList;
        return true;
      }
      if (typeof(IAddChild).IsAssignableFrom(targetType)) // Target IAddChild
      {
        IAddChild resultAC = (IAddChild)Activator.CreateInstance(targetType);
        foreach (object entry in ToCollection(val))
          resultAC.AddChild(entry);
        result = resultAC;
        return true;
      }
      if (val is ICollection && ((ICollection)val).Count == 1) // From collection to non-collection target
      { // 
        ICollection sourceCol = (ICollection)val;
        IEnumerator enumerator = sourceCol.GetEnumerator();
        enumerator.MoveNext();
        return Convert(enumerator.Current, targetType, out result);
      }

      // Simple type conversions

      System.ComponentModel.TypeConverter tc = TypeDescriptor.GetConverter(targetType);
      if (tc != null && tc.CanConvertFrom(val.GetType()))
      {
        result = tc.ConvertFrom(val);
        return true;
      }

      tc = TypeDescriptor.GetConverter(val);
      if (tc != null && tc.CanConvertTo(targetType))
      {
        result = tc.ConvertTo(val, targetType);
        return true;
      }

      if (targetType.IsAssignableFrom(typeof(string)))
      { // * -> string
        result = val.ToString();
        return true;
      }

      return false;
    }
  }
}
