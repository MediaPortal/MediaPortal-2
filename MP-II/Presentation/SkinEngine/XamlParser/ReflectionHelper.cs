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
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using MediaPortal.Presentation.Properties;

namespace Presentation.SkinEngine.XamlParser
{
  using System.Collections;

  /// <summary>
  /// Helper class for reflecting values.
  /// </summary>
  public class ReflectionHelper
  {
    protected static IDictionary<string, Assembly> _loadedAssemblies =
      new Dictionary<string, Assembly>();

    /// <summary>
    /// Returns the implicit key for the specified object. The method will try to cast
    /// <paramref name="o"/> to <see cref="IImplicitKey"/>. If the object doesn't implement
    /// this interface, an exception will be raised.
    /// </summary>
    /// <param name="o">Object whose implicit key should be evaluated.</param>
    /// <returns>Implicit key for <paramref name="o"/>.</returns>
    /// <exception cref="XamlBindingException">If <paramref name="o"/> doesn't implement
    /// <see cref="IImplicitKey"/>.</exception>
    public static object GetImplicitKey(object o)
    {
      if (o is IImplicitKey)
        return ((IImplicitKey)o).GetImplicitKey();
      else
        throw new XamlBindingException("Object '{0}' doesn't expose an implicit key", o);
    }

    /// <summary>
    /// Finds the assembly with the specified <paramref name="name"/>. This method
    /// searches all assemblies currently loaded in the current
    /// <see cref="AppDomain.CurrentDomain">application domain</see>. If the specified
    /// assembly was not loaded yet, this method tries to find the assembly dll in the
    /// directory of the mscorlib assembly.
    /// </summary>
    /// <param name="name">Short name of the assembly to load.</param>
    /// <returns>Assembly with the specified sohrt name.</returns>
    /// <exception cref="XamlLoadException">If the assembly with the specified name
    /// was not found.</exception>
    public static Assembly LoadAssembly(string name)
    {
      AssemblyName assemblyName = new AssemblyName(name);
      string assemblyShortName = assemblyName.Name;
      assemblyShortName = assemblyShortName.ToUpper(CultureInfo.InvariantCulture);

      if (_loadedAssemblies.ContainsKey(assemblyShortName))
        return _loadedAssemblies[assemblyShortName];

      Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly ass in assemblies)
      {
        if (String.Compare(ass.GetName().Name, assemblyShortName, StringComparison.OrdinalIgnoreCase) == 0)
        {
          _loadedAssemblies[assemblyShortName] = ass;
          return ass;
        }
      }

      string fullpath = new FileInfo(typeof(string).Assembly.Location).DirectoryName + "\\" + name + ".dll";
      if (File.Exists(fullpath))
      {
        Assembly ass = Assembly.LoadFile(fullpath);
        _loadedAssemblies[assemblyShortName] = ass;
        return ass;
      }

      throw new XamlLoadException("Assembly '{0}' not found", name);
    }

    /// <summary>
    /// Given the instance <paramref name="obj"/> and the <paramref name="propertyName"/>,
    /// this method searches the best matching property on the instance. It first searches
    /// a property with name [PropertyName]Property, casts it as
    /// <see cref="Property"/> and returns a <see cref="IPropertyDescriptor">property descriptor</see>
    /// for it in the parameter <paramref name="dd"/>. If there is no such property, this method
    /// searches a property with the given name, returning a property descriptor for it.
    /// If there is no property found with the given name, this method returns false and a
    /// <c>null</c> value in <paramref name="dd"/>.
    /// </summary>
    /// <param name="obj">The object where to search the property with the
    /// specified <paramref name="propertyName"/>.</param>
    /// <param name="propertyName">The name of the property to be searched.</param>
    /// <param name="dd">Property descriptor which will be returned for the property,
    /// if it was found, else a <c>null</c> value will be returned.</param>
    /// <returns>true, if a property with the specified name was found, else false.</returns>
    public static bool FindPropertyDescriptor(object obj, string propertyName, out IDataDescriptor dd)
    {
      if (obj == null)
        throw new NullReferenceException("Property target object 'null' is not supported");
      DependencyPropertyDataDescriptor dpdd;
      if (DependencyPropertyDataDescriptor.CreateDependencyPropertyDataDescriptor(
            obj, propertyName, out dpdd))
      {
        dd = dpdd;
        return true;
      }
      SimplePropertyDataDescriptor spdd;
      if (SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          obj, propertyName, out spdd))
      {
        dd = spdd;
        return true;
      }
      dd = null;
      return false;
    }

    /// <summary>
    /// Returns a data descriptor for the access to a collection-like instance
    /// <paramref name="maybeCollection"/> with an <paramref name="index"/>.
    /// </summary>
    /// <param name="maybeCollection">Instance which may be collection-like, like an
    /// <see cref="IList"/>, <see cref="ICollection"/> or <see cref="IEnumerable"/>.
    /// The returned data descriptor will allow to read the value, and for an
    /// <see cref="IList"/> it will also be writeable.</param>
    /// <param name="index">Index to access the collection-like instance.</param>
    /// <param name="result">Returns the data descriptor for the access to the
    /// <paramref name="index"/>th entry in <paramref name="maybeCollection"/>.</param>
    /// <returns><c>true</c>, if <paramref name="maybeCollection"/> is a collection-like
    /// instance and could be accessed by the specified <paramref name="index"/>, else
    /// <c>false</c>.</returns>
    public static bool GetEnumerationEntryByIndex(object maybeCollection, int index,
        out IDataDescriptor result)
    {
      object value = maybeCollection;
      if (!(value is IEnumerable))
      {
        // Value cannot be indexed, try to get content property
        IDataDescriptor dd;
        if (value is IContentEnabled &&
            ((IContentEnabled) value).FindContentProperty(out dd))
        {
          result = new ValueDataDescriptor(dd.Value);
          return true;
        }
      }
      if (value is IList)
      {
        result = new ListIndexerDataDescriptor((IList) value, index);
        return true;
      }
      if (value is IEnumerable)
      {
        int i = 0;
        foreach (object o in (IEnumerable) value)
        {
          if (i++ == index)
          {
            result = new ValueDataDescriptor(o);
            return true;
          }
        }
        throw new XamlBindingException("Index '{0}' is out of range (# elements={1})", index, i);
      }
      result = null;
      return false;
    }

    /// <summary>
    /// Tries to find an implemented <see cref="ICollection"/> or <see cref="ICollection{T}"/>
    /// interface and returns it. If the resulting collection is a generic type,
    /// the entry type (type parameter T) of this collection will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="collectionType">Returns the collection type found. If an implemented
    /// generic collection type was found, this type will be returned. Else, if the standard
    /// collection type is implemented, this type will be returned.</param>
    /// <param name="entryType">Returns the entry type (type parameter T) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedCollectionType(Type type, out Type collectionType, out Type entryType)
    {
      FindImplementedCollectionOrListType(type, typeof(ICollection), typeof(ICollection<>), out collectionType, out entryType);
    }

    /// <summary>
    /// Tries to find an implemented <see cref="IList"/> or <see cref="IList{T}"/>
    /// interface and returns it. If the resulting list is a generic type,
    /// the entry type (type parameter T) of this list will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="listType">Returns the list type found. If an implemented
    /// generic list type was found, this type will be returned. Else, if the standard
    /// list type is implemented, this type will be returned.</param>
    /// <param name="entryType">Returns the entry type (type parameter T) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedListType(Type type, out Type listType, out Type entryType)
    {
      FindImplementedCollectionOrListType(type, typeof(IList), typeof(IList<>), out listType, out entryType);
    }

    protected static void FindImplementedCollectionOrListType(Type type,
      Type nonGenericType, Type genericType, out Type resultCollectionType, out Type resultEntryType)
    {
      resultCollectionType = null;
      resultEntryType = null;
      IDictionary<Type, Type> foundGeneric = new Dictionary<Type, Type>();
      IList<Type> foundNonGeneric = new List<Type>();
      foreach (Type interfaceType in type.GetInterfaces())
      {
        Type collectionType = nonGenericType;
        if (interfaceType.IsGenericType)
        {
          Type entryType = interfaceType.GetGenericArguments()[0];
          collectionType = genericType;
          collectionType = collectionType.MakeGenericType(entryType);
          if (collectionType.IsAssignableFrom(type))
            if (!foundGeneric.ContainsKey(collectionType))
              foundGeneric.Add(collectionType, entryType);
        }
        else if (collectionType.IsAssignableFrom(type))
          foundNonGeneric.Add(collectionType);
      }
      IEnumerator<KeyValuePair<Type, Type>> ge = foundGeneric.GetEnumerator();
      if (ge.MoveNext())
      {
        resultCollectionType = ge.Current.Key;
        resultEntryType = ge.Current.Value;
        return;
      }
      IEnumerator<Type> nge = foundNonGeneric.GetEnumerator();
      if (nge.MoveNext())
      {
        resultCollectionType = nge.Current;
        resultEntryType = null;
        return;
      }
    }

    /// <summary>
    /// Finds the first implemented <see cref="IAddChild{T}"/> interface type of the specified
    /// <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Type to examine.</param>
    /// <param name="method">If the specified <paramref name="type"/> implements the
    /// <see cref="IAddChild{T}"/> interface, this parameter returns the
    /// <see cref="IAddChild{T}.AddChild"/> method.</param>
    /// <param name="entryType">If the specified <paramref name="type"/> implements the
    /// <see cref="IAddChild{T}"/> interface, this parameter returns the entry type
    /// (type parameter T) of the implemented <see cref="IAddChild{T}"/> interface type.
    /// <returns><c>true</c>, if the specified type implements <see cref="IAddChild{T}"/>,
    /// else <c>false</c>.</returns>
    public static bool IsIAddChild(Type type, out MethodInfo method, out Type entryType)
    {
      method = null;
      entryType = null;
      foreach (Type interfaceType in type.GetInterfaces())
      {
        if (interfaceType.IsGenericType)
        {
          Type iact = typeof(IAddChild<>);
          Type et = interfaceType.GetGenericArguments()[0];
          iact = iact.MakeGenericType(et);
          if (iact.IsAssignableFrom(type))
          {
            method = type.GetMethod("AddChild", new Type[] {et});
            entryType = et;
            return true;
          }
        }
      }
      return false;
    }
  }
}
