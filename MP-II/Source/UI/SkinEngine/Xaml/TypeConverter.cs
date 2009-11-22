#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
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

    public static bool ConvertEntryType(ICollection col, Type entryType, out ICollection result)
    {
      if (entryType == null)
      {
        result = col;
        return true;
      }
      result = null;
      List<object> res = new List<object>();
      foreach (object o in col)
      {
        object obj = o;
        if (!entryType.IsAssignableFrom(obj.GetType()))
          if (!Convert(obj, entryType, out obj))
            return false;
        res.Add(obj);
      }
      result = res;
      return true;
    }

    /// <summary>
    /// Converts the specified <paramref name="obj"/> to a collection.
    /// If the object supports the interface <see cref="ICollection"/> itself,
    /// it will simply be casted. If it is an <see cref="IEnumerable"/>, its contents
    /// will be copied into a new object implementing <see cref="ICollection"/>.
    /// Else, a new <see cref="ICollection"/> will be created with <paramref name="obj"/>
    /// as single contents.
    /// A string won't be treated as a collection of characters but will be treated as if
    /// it was no collection.
    /// </summary>
    /// <param name="obj">The object to convert to a collection.</param>
    /// <param name="entryType">Type to convert the collection entries to or <c>null</c>.</param>
    /// <param name="result">Collection containing the contents of obj or obj itself.</param>
    /// <returns><c>true</c>, if the conversion was successful, else <c>false</c>.</returns>
    protected static bool ToCollection(object obj, Type entryType, out ICollection result)
    {
      if (obj is IInclude)
        obj = ((IInclude) obj).Content;
      if (obj.GetType() != typeof(string)) // Don't treat strings as a collection of characters
      {
        if (obj is ICollection)
          return ConvertEntryType((ICollection) obj, entryType, out result);
        else if (obj is IEnumerable)
        {
          List<object> col = new List<object>();
          foreach (object o1 in (IEnumerable) obj)
            col.Add(o1);
          return ConvertEntryType(col, entryType, out result);
        }
      }
      List<object> res = new List<object>();
      object o2;
      if (entryType == null)
        res.Add(obj);
      else if (Convert(obj, entryType, out o2))
        res.Add(o2);
      else
      {
        result = null;
        return false;
      }
      result = res;
      return true;
    }

    public static object Convert(object value, Type targetType)
    {
      object result;
      if (Convert(value, targetType, out result))
        return result;
      else
        throw new ConvertException("Could not convert object '{0}' to type '{1}'", value, targetType.Name);
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

      // TODO: typeof(Nullable<T>)

      // Built-in type conversions

      if (val.GetType() == typeof(string) && targetType == typeof(Type))
      { // string -> Type
        result = Type.GetType(val.ToString());
        return result != null;
      }

      // Enumerations
      if (val.GetType() == typeof(string) && targetType.IsEnum)
      { // string -> Enum
        result = Enum.Parse(targetType, (string) val);
        return true;
      }

      // Collection types

      Type enumerableType;
      Type entryType;
      ReflectionHelper.FindImplementedEnumerableType(targetType, out enumerableType, out entryType);

      if (enumerableType != null) // Targets IList, ICollection, IList<>, ICollection<>
      {
        IList<object> resultList = new List<object>();
        ICollection col;
        if (!ToCollection(val, entryType, out col))
          return false;
        foreach (object entry in col)
          resultList.Add(entry);
        result = resultList;
        return true;
      }
      if (val is ICollection && ((ICollection) val).Count == 1) // From collection to non-collection target
      {
        // 
        ICollection sourceCol = (ICollection) val;
        IEnumerator enumerator = sourceCol.GetEnumerator();
        enumerator.MoveNext();
        return Convert(enumerator.Current, targetType, out result);
      }

      // Simple type conversions

      System.ComponentModel.TypeConverter tc = TypeDescriptor.GetConverter(targetType);
      if (tc != null && tc.CanConvertFrom(val.GetType()))
      {
        try
        {
          result = tc.ConvertFrom(null, CultureInfo.InvariantCulture, val);
          return true;
        }
        catch { }
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
