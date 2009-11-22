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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  /// <summary>
  /// Helper class for reflecting values.
  /// </summary>
  public class ReflectionHelper
  {
    /// <summary>
    /// Given the <paramref name="methodInfos"/>, this method tries to choose one of them which matches best
    /// for the given parameters, and returns the method and the converted parameters.
    /// </summary>
    /// <param name="methodInfos">Enumeration of methods to be checked.</param>
    /// <param name="parameters">Parameters to be used.</param>
    /// <param name="methodBase">Method which can be used with the <paramref name="convertedParameters"/>.</param>
    /// <param name="convertedParameters">Parameters which have been converted from the given <paramref name="parameters"/>.
    /// </param>
    /// <returns><c>true</c>, if a member matched, else <c>false</c>.</returns>
    public static bool FindBestMember(IEnumerable<MethodBase> methodInfos, object[] parameters,
        out MethodBase methodBase, out object[] convertedParameters)
    {
      MethodBase bestMatch = null;
      bool ambiguousVagueMember = false;
      foreach (MethodBase mb in methodInfos)
      {
        ParameterInfo[] formalParameters = mb.GetParameters();
        if (formalParameters.Length == parameters.Length)
        {
          bool missmatch = false;
          for (int i=0; i<formalParameters.Length; i++)
          {
            Type formalParameter = formalParameters[i].ParameterType;
            Type actualParameter = parameters[i].GetType();
            if (actualParameter != formalParameter)
            {
              missmatch = true;
              break;
            }
          }
          if (!missmatch)
          {
            // We found a member info with exactly the given parameters
            methodBase = mb;
            convertedParameters = parameters;
            return true;
          }
          if (bestMatch == null)
            bestMatch = mb;
          else
            ambiguousVagueMember = true;
        }
      }
      if (ambiguousVagueMember)
        throw new XamlParserException("Trying to access an ambinguously defined member");
      if (bestMatch != null && ConsumeParameters(parameters, bestMatch.GetParameters(), true, out convertedParameters))
      {
        methodBase = bestMatch;
        return true;
      }
      convertedParameters = null;
      methodBase = null;
      return false;
    }

    /// <summary>
    /// Tries to convert the specified <paramref name="parameters"/> objects to match the
    /// specified <paramref name="parameterInfos"/> for a method call or property index expression.
    /// the converted parameters will be returned in the parameter <paramref name="convertedParameters"/>.
    /// </summary>
    /// <param name="parameters">Input parameter objects to be converted.</param>
    /// <param name="parameterInfos">Parameter specification to convert the <paramref name="parameters"/>
    /// to.</param>
    /// <param name="mustMatchSignature">If set to <c>true</c>, this method raises an exception if
    /// the parameters do not match the specified signature or if they cannot be converted. If this
    /// parameter is set to <c>false</c> and the parameters do not match the signature, this method
    /// only returns a value of <c>false</c>. This parameter could also be named
    /// "throwExceptionIfNotMatch".</param>
    /// <param name="convertedParameters">Returns the converted parameters, if this method
    /// returns a value of <c>true</c>.</param>
    /// <returns><c>true</c>, if the parameter conversion could be done successfully, else
    /// <c>false</c>.</returns>
    public static bool ConsumeParameters(IEnumerable<object> parameters,
        ParameterInfo[] parameterInfos, bool mustMatchSignature,
        out object[] convertedParameters)
    {
      Type[] indexTypes = new Type[parameterInfos.Length];
      int ti = 0;
      int numMandatory = 0;
      foreach (ParameterInfo parameter in parameterInfos)
      {
        indexTypes[ti++] = parameter.ParameterType;
        if (!parameter.IsOptional)
          numMandatory++;
      }
      bool result = ConvertTypes(parameters, indexTypes, out convertedParameters);
      if (result && convertedParameters.Length <= indexTypes.Length &&
          convertedParameters.Length >= numMandatory)
        return true;
      else if (mustMatchSignature)
        if (result)
          throw new XamlBindingException("Wrong count of parameter for index (expected: {0}, got: {1})",
              parameterInfos.Length, convertedParameters.Length);
        else
          throw new XamlBindingException("Could not convert parameters");
      else
        return false;
    }

    /// <summary>
    /// Convertes all objects in the specified <paramref name="objects"/> array to the specified
    /// <paramref name="types"/>. The number of types may be greater than the number of objects;
    /// this supports type conversion for both mandatory and optional parameters.
    /// </summary>
    /// <param name="objects">The array of objects to be type-converted.</param>
    /// <param name="types">Desired types the objects should be converted to.
    /// Indices in the <paramref name="types"/> array correspond to indices
    /// of the <paramref name="objects"/> array. The <paramref name="types"/>
    /// array may contain more elements than the <paramref name="objects"/> array.</param>
    /// <param name="convertedIndices">Returns the array of converted objects.
    /// The size of this returned array is the same as the size of the
    /// <paramref name="objects"/> array.</param>
    /// <returns><c>true</c>, if the conversion was successful for all objects
    /// in the input array, else <c>false</c>.</returns>
    /// <exception cref="XamlBindingException">If the number of objects given is greater than
    /// the number of types given.</exception>
    public static bool ConvertTypes(IEnumerable<object> objects, Type[] types,
        out object[] convertedIndices)
    {
      // Convert objects to index types
      convertedIndices = new object[types.Length];
      int current = 0;
      foreach (object obj in objects)
      {
        if (current >= types.Length)
          return false;
        if (!TypeConverter.Convert(obj, types[current], out convertedIndices[current]))
          return false;
        current++;
      }
      return true;
    }

    /// <summary>
    /// Given the instance <paramref name="obj"/> and the <paramref name="memberName"/>,
    /// this method searches the best matching member on the instance. It first searches
    /// a property with name [PropertyName]Property, casts it to
    /// <see cref="Property"/> and returns a <see cref="DependencyPropertyDataDescriptor"/>
    /// for it in the parameter <paramref name="dd"/>. If there is no such property, this method
    /// searches a simple property with the given name, returning a property descriptor for it.
    /// Then, the method will search for a field with the specified name, returning a
    /// <see cref="FieldDataDescriptor"/> for it.
    /// If there is no member found with the given name, this method returns false and a
    /// <c>null</c> value in <paramref name="dd"/>.
    /// </summary>
    /// <param name="obj">The object where to search the member with the
    /// specified <paramref name="memberName"/>.</param>
    /// <param name="memberName">The name of the member to be searched.</param>
    /// <param name="dd">Data descriptor which will be returned for the property or member,
    /// if it was found, else a <c>null</c> value will be returned.</param>
    /// <returns><c>true</c>, if a member with the specified name was found, else <c>false</c>.</returns>
    public static bool FindMemberDescriptor(object obj, string memberName, out IDataDescriptor dd)
    {
      if (obj == null)
        throw new NullReferenceException("Property target object 'null' is not supported");
      DependencyPropertyDataDescriptor dpdd;
      if (DependencyPropertyDataDescriptor.CreateDependencyPropertyDataDescriptor(
            obj, memberName, out dpdd))
      {
        dd = dpdd;
        return true;
      }
      SimplePropertyDataDescriptor spdd;
      if (SimplePropertyDataDescriptor.CreateSimplePropertyDataDescriptor(
          obj, memberName, out spdd))
      {
        dd = spdd;
        return true;
      }
      FieldDataDescriptor fdd;
      if (FieldDataDescriptor.CreateFieldDataDescriptor(obj, memberName, out fdd))
      {
        dd = fdd;
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
    /// <see cref="IList{T}"/>, <see cref="ICollection{T}"/> or <see cref="IEnumerable{T}"/>.
    /// The returned data descriptor will allow to read the value, and for an
    /// <see cref="IList{T}"/> it will also be writeable.</param>
    /// <param name="index">Index to access the collection-like instance.</param>
    /// <param name="result">Returns the data descriptor for the access to the
    /// <paramref name="index"/>th entry in <paramref name="maybeCollection"/>.</param>
    /// <returns><c>true</c>, if <paramref name="maybeCollection"/> is a collection-like
    /// instance and could be accessed by the specified <paramref name="index"/>, else
    /// <c>false</c>.</returns>
    public static bool GetEnumerationEntryByIndex(object maybeCollection, int index,
        out IDataDescriptor result)
    {
      if (maybeCollection is IList)
      {
        result = new IndexerDataDescriptor(maybeCollection, new object[] { index });
        return true;
      }
      if (maybeCollection is IEnumerable)
      {
        int i = 0;
        foreach (object o in (IEnumerable)maybeCollection)
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
    /// Tries to find an implemented <see cref="IEnumerable{T}"/> or <see cref="IEnumerable"/>
    /// interface and returns it. If the resulting enumerable is a generic type,
    /// the entry type (type parameter T) of this enumerable will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="enumerableType">Returns the enumerable type found. If an implemented
    /// generic enumerable type was found, this type will be returned. Else, if the standard
    /// enumerable type is implemented, this type will be returned. If no enumerable type is
    /// implemented, this value will be <c>null</c>.</param>
    /// <param name="entryType">Returns the entry type (type parameter T) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedEnumerableType(Type type, out Type enumerableType, out Type entryType)
    {
      FindImplementedEnumerableType(type, typeof(IEnumerable), typeof(IEnumerable<>), out enumerableType, out entryType);
    }

    /// <summary>
    /// Tries to find an implemented <see cref="ICollection{T}"/> or <see cref="ICollection"/>
    /// interface and returns it. If the resulting collection is a generic type,
    /// the entry type (type parameter T) of this collection will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="collectionType">Returns the collection type found. If an implemented
    /// generic collection type was found, this type will be returned. Else, if the standard
    /// collection type is implemented, this type will be returned. If no collection type is
    /// implemented, this value will be <c>null</c>.</param>
    /// <param name="entryType">Returns the entry type (type parameter T) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedCollectionType(Type type, out Type collectionType, out Type entryType)
    {
      FindImplementedEnumerableType(type, typeof(ICollection), typeof(ICollection<>), out collectionType, out entryType);
    }

    /// <summary>
    /// Tries to find an implemented <see cref="IList{T}"/> or <see cref="IList{T}"/>
    /// interface and returns it. If the resulting list is a generic type,
    /// the entry type (type parameter T) of this list will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="listType">Returns the list type found. If an implemented
    /// generic list type was found, this type will be returned. Else, if the standard
    /// list type is implemented, this type will be returned. If no list type is
    /// implemented, this value will be <c>null</c>.</param>
    /// <param name="entryType">Returns the entry type (type parameter T) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedListType(Type type, out Type listType, out Type entryType)
    {
      FindImplementedEnumerableType(type, typeof(IList), typeof(IList<>), out listType, out entryType);
    }

    protected static void FindImplementedEnumerableType(Type type,
      Type nonGenericType, Type genericType, out Type resultEnumerableType, out Type resultEntryType)
    {
      resultEnumerableType = null;
      resultEntryType = null;
      // First check generic interfaces
      IDictionary<Type, Type> foundGeneric = new Dictionary<Type, Type>();
      foreach (Type interfaceType in type.GetInterfaces())
      {
        Type[] genericArguments;
        if (interfaceType.IsGenericType && (genericArguments = interfaceType.GetGenericArguments()).Length == 1)
        {
          Type entryType = genericArguments[0];
          Type collectionType = genericType.MakeGenericType(entryType);
          if (collectionType.IsAssignableFrom(type))
            if (!foundGeneric.ContainsKey(collectionType))
              foundGeneric.Add(collectionType, entryType);
        }
      }
      IEnumerator<KeyValuePair<Type, Type>> ge = foundGeneric.GetEnumerator();
      if (ge.MoveNext())
      {
        resultEnumerableType = ge.Current.Key;
        resultEntryType = ge.Current.Value;
        return;
      }
      // Fallback: Check non-generic type
      if (nonGenericType.IsAssignableFrom(type))
      {
        resultEnumerableType = nonGenericType;
        resultEntryType = null;
        return;
      }
    }

    /// <summary>
    /// Tries to find an implemented <see cref="IDictionary{TKey,TValue}"/> or <see cref="IDictionary{TKey,TValue}"/>
    /// interface and returns it. If the resulting dictionary is a generic type,
    /// the key type (type parameter TKey) and the value type (type parameter TValue) of this dictionary
    /// will be returned too.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="resultDictionaryType">Returns the dictionary type found. If an implemented
    /// generic dictionary type was found, this type will be returned. Else, if the standard
    /// dictionary type is implemented, this type will be returned. If no dictionary type is
    /// implemented, this value will be <c>null</c>.</param>
    /// <param name="resultKeyType">Returns the key type (type parameter TKey) of the implemented
    /// generic type, if any.</param>
    /// <param name="resultValueType">Returns the value type (type parameter TValue) of the implemented
    /// generic type, if any.</param>
    public static void FindImplementedDictionaryType(Type type,
      out Type resultDictionaryType, out Type resultKeyType, out Type resultValueType)
    {
      resultDictionaryType = null;
      resultKeyType = null;
      resultValueType = null;
      // First check generic interfaces
      IDictionary<Type, KeyValuePair<Type, Type>> foundGeneric = new Dictionary<Type, KeyValuePair<Type, Type>>();
      foreach (Type interfaceType in type.GetInterfaces())
      {
        Type[] genericArguments;
        if (interfaceType.IsGenericType && (genericArguments = interfaceType.GetGenericArguments()).Length == 2)
        {
          Type keyType = genericArguments[0];
          Type valueType = genericArguments[1];
          Type collectionType = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
          if (collectionType.IsAssignableFrom(type))
            if (!foundGeneric.ContainsKey(collectionType))
              foundGeneric.Add(collectionType, new KeyValuePair<Type, Type>(keyType, valueType));
        }
      }
      IEnumerator<KeyValuePair<Type, KeyValuePair<Type, Type>>> ge = foundGeneric.GetEnumerator();
      if (ge.MoveNext())
      {
        resultDictionaryType = ge.Current.Key;
        resultKeyType = ge.Current.Value.Key;
        resultValueType = ge.Current.Value.Value;
        return;
      }
      // Fallback: Check non-generic type
      if (typeof(IDictionary).IsAssignableFrom(type))
      {
        resultDictionaryType = typeof(IDictionary);
        resultKeyType = null;
        resultValueType = null;
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
            method = type.GetMethod("AddChild", new Type[] { et });
            entryType = et;
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if the specified <paramref name="maybeCollectionTarget"/> parameter
    /// is an object which is not null and which supports any collection element adding
    /// facility. The specified <paramref name="value"/> will then be assigned to the
    /// collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will be needed in two cases:
    /// <list type="number">
    /// <item>A property should be assigned, which has a collection-like type. These properties
    /// won't be assigned directly but they are expected to have a not-null collection value.
    /// Adding children to them will cause calling some "Add" method on the existing instance.</item>
    /// <item>Child elements should be assigned to an object which doesn't support
    /// the <see cref="IContentEnabled"/> interface. So if there is no content property,
    /// the parser tries to add children directly to the object, if it is supported.</item>
    /// </list>
    /// In both cases, there is already an existing (collection-like) instance, to that the
    /// specified <paramref name="value"/> should be assigned, so this method has no
    /// need to create the collection itself.
    /// </para>
    /// <para>
    /// If this method cannot handle the specified target object
    /// <paramref name="maybeCollectionTarget"/>, or if this parameter is <c>null</c>,
    /// it will return <c>false</c> to signal that it couldn't do the assignment.
    /// </para>
    /// </remarks>
    /// <param name="maybeCollectionTarget">Parameter holding an instance which may be
    /// a collection type to add the <paramref name="value"/> to. May also be <c>null</c>,
    /// which will result in a return value of <c>false</c>.</param>
    /// <param name="value">The value to assign to the target object. If the value has
    /// a collection type compatible with the <paramref name="maybeCollectionTarget"/> target,
    /// the contents will be transferred to the target object, else this parameter value
    /// will be added to the target itself.</param>
    /// <returns><c>true</c>, if the method could handle the assignment, else <c>false</c>.</returns>
    public static bool CheckHandleCollectionAssignment(object maybeCollectionTarget, object value)
    {
      if (maybeCollectionTarget == null || value == null)
        return false;
      Type targetType = maybeCollectionTarget.GetType();
      // Check for List
      Type resultType;
      Type entryType;
      MethodInfo method;
      FindImplementedListType(targetType, out resultType, out entryType);
      if (resultType != null)
      {
        method = entryType == null ? targetType.GetMethod("Add") : targetType.GetMethod("Add", new Type[] { entryType });
        // Have to cast to ICollection, because the type converter cannot cope with the situation corretcly if we cast to IEnumerable
        ICollection col = (ICollection)TypeConverter.Convert(value, typeof(ICollection));
        if (col == null)
          // The type converter converts null to null rather than to an empty collection, so we have to handle this case explicitly
          method.Invoke(maybeCollectionTarget, new object[] { null });
        else
          foreach (object child in col)
            method.Invoke(maybeCollectionTarget, new object[] { child });
        return true;
      }
      // Check for Dictionary
      Type keyType;
      Type valueType;
      FindImplementedDictionaryType(targetType, out resultType, out keyType, out valueType);
      Type sourceDictType;
      Type sourceKeyType;
      Type sourceValueType;
      FindImplementedDictionaryType(value.GetType(), out sourceDictType, out sourceKeyType, out sourceValueType);
      if (resultType != null && sourceDictType != null)
      {
        PropertyInfo targetItemProperty = keyType == null ? targetType.GetProperty("Item") : targetType.GetProperty("Item", new Type[] { keyType });
        MethodInfo targetItemSetter = targetItemProperty.GetSetMethod();
        foreach (KeyValuePair<object, object> kvp in (IEnumerable)value)
          targetItemSetter.Invoke(maybeCollectionTarget, new object[] { kvp.Key, kvp.Value });
        return true;
      }
      // Check for IAddChild
      if (IsIAddChild(maybeCollectionTarget.GetType(), out method, out entryType))
      {
        foreach (object child in (ICollection)TypeConverter.Convert(value, typeof(ICollection)))
          method.Invoke(maybeCollectionTarget, new object[] { TypeConverter.Convert(child, entryType) });
        return true;
      }
      else
        return false;
    }
  }
}