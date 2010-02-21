#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections;
using System.Text;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  /// <summary>
  /// Path segment in a <see cref="PathExpression"/>. A path segment
  /// represents a query to an attribute (property/field), attached property
  /// or a method call on a starting object. Its methods allow it to
  /// <see cref="Evaluate"/> the attribute or to request the method
  /// (<see cref="GetMethod"/>).
  /// </summary>
  public interface IPathSegment
  {
    /// <summary>
    /// Evaluates this path segment based on the <paramref name="source"/> object, and
    /// returns the value in the returned <paramref name="result"/>.
    /// </summary>
    /// <param name="source">Source data for the attribute/property query.</param>
    /// <param name="result">Resulting data descriptor.</param>
    /// <returns><c>true</c>, if this path segment could be evaluated on object
    /// <paramref name="source"/>, <c>false</c> if it is not defined on the specified
    /// <paramref name="source"/>.</returns>
    bool Evaluate(IDataDescriptor source, out IDataDescriptor result);

    /// <summary>
    /// Returns the target object and method info of the method this path segment
    /// represents.
    /// </summary>
    /// <param name="source">Source data for the method query.</param>
    /// <param name="obj">Target object on which the returned
    /// <paramref name="mi"/> can be applied.</param>
    /// <param name="mi">Method info object which can be applied on the
    /// returned <paramref name="obj"/>.</param>
    /// <returns><c>true</c>, if this path segment represents a method on the
    /// <paramref name="source"/> object, <c>false</c> if there is no matching
    /// method. In case the return value is <c>true</c>, the returned
    /// parameters <paramref name="obj"/> and <paramref name="mi"/> can be
    /// used to call the method. In case the return value is <c>false</c>,
    /// the values of those two parameters are undefined.</returns>
    bool GetMethod(IDataDescriptor source, out object obj, out MethodInfo mi);
  }

  /// <summary>
  /// Represents an index expression on a predecessor value.
  /// Handles collection indexes as well as indexes on the object itself
  /// (Item[...]). The second case also handles dictionary accesses.
  /// </summary>
  public class IndexerPathSegment: IPathSegment
  {
    protected object[] _indices;

    public IndexerPathSegment(object[] indices)
    {
      _indices = indices;
    }

    public bool Evaluate(IDataDescriptor source, out IDataDescriptor result)
    {
      result = null;
      object value = source.Value;
      ParameterInfo[] indexerParams = IndexerDataDescriptor.GetIndexerTypes(value.GetType());
      object[] convertedIndices;
      // Search indexer on source type
      if (indexerParams != null && ReflectionHelper.ConsumeParameters(_indices,
          indexerParams, false, out convertedIndices))
      { // Index on Item property
        result = new IndexerDataDescriptor(value, convertedIndices);
        return true;
      }
      else if (ReflectionHelper.ConvertTypes(_indices, new Type[] { typeof(int) },
          out convertedIndices))
      { // Collection index
        if (!ReflectionHelper.GetEnumerationEntryByIndex(value, (int) convertedIndices[0], out result))
          throw new XamlBindingException("Index '{0}' cannot be applied on '{1}'",
              ToString(), value);
        return true;
      }
      return false;
    }

    public bool GetMethod(IDataDescriptor source, out object obj, out MethodInfo mi)
    {
      obj = null;
      mi = null;
      return false;
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append("[");
      foreach (object index in _indices)
        result.Append(index.ToString());
      result.Append("]");
      return result.ToString();
    }
  }

  /// <summary>
  /// Path segment representing an invocation of an attached property.
  /// </summary>
  public class AttachedPropertyPathSegment : IPathSegment
  {
    protected string _propertyProvider;
    protected string _propertyName;
    protected string _namespaceURI;
    protected INamespaceHandler _namespaceHandler;

    public AttachedPropertyPathSegment(IParserContext context, string propertyProvider, string propertyName,
        string namespaceURI)
    {
      if (string.IsNullOrEmpty(propertyProvider))
        throw new XamlParserException("Property provider name must not be empty");
      if (string.IsNullOrEmpty(propertyName))
        throw new XamlParserException("Property name must not be empty");
      _propertyProvider = propertyProvider;
      _propertyName = propertyName;
      _namespaceURI = namespaceURI;
      _namespaceHandler = context.GetNamespaceHandler(namespaceURI);
    }

    public bool Evaluate(IDataDescriptor source, out IDataDescriptor result)
    {
      result = null;
      try
      {
        result = _namespaceHandler.
          GetAttachedProperty(_propertyProvider, _propertyName, source.Value, _namespaceURI);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("AttachedPropertyPathSegment: Cannot evaluate attached property '{0}' on object '{1}'", e, ToString(), source.Value);
        return false;
      }
      return true;
    }

    public bool GetMethod(IDataDescriptor source, out object obj, out MethodInfo mi)
    {
      obj = null;
      mi = null;
      return false;
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append(_propertyProvider);
      result.Append('.');
      result.Append(_propertyName);
      return result.ToString();
    }
  }

  /// <summary>
  /// Represents an attribute (field/property) invocation or a method call.
  /// </summary>
  public class MemberPathSegment : IPathSegment
  {
    protected string _memberName;
    protected object[] _indices = null;

    public MemberPathSegment(string memberName)
    {
      if (string.IsNullOrEmpty(memberName))
        throw new XamlParserException("Member name must not be empty");
      _memberName = memberName;
    }

    protected static bool ExtractWorkingData(IDataDescriptor source, string memberName,
        out Type type, out object obj, out MemberInfo mi)
    {
      obj = source.Value;
      mi = null;
      if (obj is Type)
      {
        type = (Type) obj;
        obj = null;
      }
      else
      {
        type = obj.GetType();
      }
      MemberInfo[] members = type.GetMember(memberName);
      if (members.Length == 0)
        return false;
      mi = members[0];
      return true;
    }

    /// <summary>
    /// Subscripts the returned value of this attribute by the specified indices.
    /// </summary>
    /// <param name="indices">Objects used as index parameters for this
    /// property or for the object returned by the attribute query.
    /// The decision, if the specified indices will be applied as property
    /// indices or as subscripts on the returned object, is determined by
    /// the formal parameters of the property declaration on the type this
    /// path segment gets evaluated on.</param>
    public void SetIndices(object[] indices)
    {
      _indices = indices;
    }

    public bool Evaluate(IDataDescriptor source, out IDataDescriptor result)
    {
      result = null;
      Type type;
      object obj;
      MemberInfo mi;
      if (!ExtractWorkingData(source, _memberName+"Property", out type, out obj, out mi))
        if (!ExtractWorkingData(source, _memberName, out type, out obj, out mi))
          return false;
      if (mi is FieldInfo)
      { // Field access
        result = new FieldDataDescriptor(obj, (FieldInfo) mi);
        return true;
      }
      else if (mi is PropertyInfo)
      { // Property access
        PropertyInfo pi = (PropertyInfo) mi;
        // Handle indexed property
        object[] convertedIndices = null;
        // Check property indexer
        bool indicesOnProperty = _indices != null && _indices.Length > 0 &&
            ReflectionHelper.ConsumeParameters(_indices, pi.GetIndexParameters(),
                false, out convertedIndices);
        if (!indicesOnProperty)
          convertedIndices = null;
        if (pi.PropertyType == typeof(AbstractProperty))
        { // Property value -> request value and return DependencyPropertyDataDescriptor
          object val = pi.GetValue(obj, convertedIndices);
          result = new DependencyPropertyDataDescriptor(obj, _memberName, (AbstractProperty) val);
        }
        else
        { // Simple property
          result = new SimplePropertyDataDescriptor(obj, pi);
          if (convertedIndices != null && convertedIndices.Length > 0)
            ((SimplePropertyDataDescriptor) result).Indices = convertedIndices;
        }
        if (_indices != null && _indices.Length > 0 && !indicesOnProperty)
          // Item or collection index -> handle index expression per IndexerPathSegment
          return new IndexerPathSegment(_indices).Evaluate(result, out result);
        return true;
      }
      else if (mi is MethodInfo)
        // Method invocation is not supported in evaluation
        return false;
      else
        // Unsupported member type
        return false;
    }

    public bool GetMethod(IDataDescriptor source, out object obj, out MethodInfo mi)
    {
      mi = null;
      Type type;
      MemberInfo memberInfo;
      if (!ExtractWorkingData(source, _memberName, out type, out obj, out memberInfo))
        return false;
      mi = memberInfo as MethodInfo;
      return mi != null;
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append(_memberName);
      if (_indices != null)
      {
        result.Append('[');
        bool first = true;
        foreach (object index in _indices)
        {
          if (!first)
            result.Append(", ");
          first = false;
          result.Append(index);
        }
        result.Append(']');
      }
      return result.ToString();
    }
  }

  /// <summary>
  /// Negates a boolean value.
  /// </summary>
  public class NegatePathSegment : IPathSegment
  {
    public bool Evaluate(IDataDescriptor source, out IDataDescriptor result)
    {
      DataDescriptorRepeater ddr = new DataDescriptorRepeater
        {
            SourceValue = source,
            Negate = true
        };
      result = ddr;
      return true;
    }

    public bool GetMethod(IDataDescriptor source, out object obj, out MethodInfo mi)
    {
      obj = null;
      mi = null;
      return false;
    }

    public override string ToString()
    {
      return "<Negation>";
    }
  }

  /// <summary>
  /// Class for path expressions which are compiled in a parsing context.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A path expression in this context is an iterated query of attributes,
  /// properties, attached properties or method calls
  /// starting with a predecessor value. In contrast to a single-step evaluation
  /// of the resulting value during the parsing operation of the path expression,
  /// we do a "compilation" of the path expression, storing the expression in an
  /// internal format with additional context information for it
  /// (for example references to <see cref="INamespaceHandler"/>s),
  /// so we don't need the parser context at the future time of evaluation.
  /// This means once a path expression is compiled, it can be evaluated even after
  /// the XAML parsing process has finished and the parser context is disposed.
  /// </para>
  /// <para>
  /// Path segments stored in this path segment enumeration don't have a relation
  /// to each other at compile time. They are simply a list of evaluation executors
  /// to be used in the later evaluation process. A path expression so can be
  /// evaluated on any starting object which contains the structure queried
  /// by this path expression.
  /// </para>
  /// <para>
  /// <example>
  /// A path expression, which can be evaluated on a
  /// <see cref="MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes.Rectangle"/>, is for example:<br/>
  /// <code>Fill.RelativeTransform.Children[0].ScaleY</code>
  /// </example>
  /// </para>
  /// </remarks>
  public class PathExpression: IEnumerable<IPathSegment>
  {
    #region Protected fields

    protected IList<IPathSegment> _pathSegments = new List<IPathSegment>();

    #endregion

    #region Ctor

    /// <summary>
    /// PathExpression should only be instantiated by calling the factory method
    /// <see cref="PathExpression.Compile(IParserContext,string)"/>.
    /// </summary>
    protected PathExpression() { }

    #endregion

    #region Public methods

    /// <summary>
    /// Compiles a <see cref="string"/> to a <see cref="PathExpression"/>.
    /// </summary>
    /// <param name="context">The current parser context.</param>
    /// <param name="path">The path expression to compile. The path expression uses a syntax similar
    /// to the C# syntax for property access. Properties and field are specified by appending
    /// a <c>.[MemberName]</c>, attached properties are specified by a
    /// <c>.([AttachedPropertyProvider].[PropertyName])</c> syntax, indexers are specified by
    /// <c>[MemberName][[Index]]</c></param>
    /// <returns>Compiled path expression, which can be evaluated without the need of
    /// a <see cref="IParserContext">parser context object</see>.</returns>
    public static PathExpression Compile(IParserContext context, string path)
    {
      PathExpression result = new PathExpression();
      path = path.Trim();
      bool negate = false;
      int pos = 0;
      if (path.StartsWith("!"))
      {
        negate = true;
        pos++;
      }
      while (pos < path.Length)
      {
        IPathSegment ps;
        try
        {
          pos = ParsePathSegment(context, path, pos, out ps);
        }
        catch (Exception ex)
        {
          throw new XamlParserException("PathExpression '{0}' at position {1}: {2}", ex, path, pos, ex.Message);
        }
        result.AddPathSegment(ps);
        if (pos >= path.Length)
          break;
        if (path[pos] != '.')
          throw new XamlParserException("PathExpression '{0}': '.' expected at position {1}", path, pos);
        pos++;
      }
      if (negate)
        result.AddPathSegment(new NegatePathSegment());
      return result;
    }

    /// <summary>
    /// Adds a path segment to this path expression.
    /// </summary>
    public void AddPathSegment(IPathSegment ps)
    {
      _pathSegments.Add(ps);
    }

    /// <summary>
    /// Given a starting object (or type) in a data context object, this method
    /// evaluates this path on it. The path must only contain evaluable fields in it,
    /// no method calls are supported.
    /// </summary>
    /// <remarks>
    /// This method handles static and non-static property and field access, attached property
    /// access and indexers.
    /// </remarks>
    /// <param name="start">Object used as starting point for the path evaluation.
    /// This may be an <see cref="object"/> or a <see cref="Type"/>.</param>
    /// <param name="result">Returns the reflected value, if it could be resolved.</param>
    /// <returns><c>true</c>, if this path could be resolved on object
    /// <paramref name="start"/>, else <c>false</c>.</returns>
    /// <exception cref="XamlBindingException">If an error occurs during evaluation
    /// of this path on object <paramref name="start"/>.</exception>
    public bool Evaluate(IDataDescriptor start, out IDataDescriptor result)
    {
      try
      {
        result = start;
        foreach (IPathSegment ps in _pathSegments)
        {
          if (result == null || result.Value == null)
            return false;
          if (!ps.Evaluate(result, out result))
            return false;
        }
        return true;
      }
      catch (Exception e)
      {
        throw new XamlBindingException("PathExpression: Error evaluating path expression '{0}'", e, ToString());
      }
    }

    /// <summary>
    /// Given a starting object (or type) in a data context object, this method
    /// checks this path expression, if it yields a method call in its last path segment.
    /// If this is the case, it evaluates all predecessor path segments based on the
    /// specified <paramref name="start"/> descriptor and returns the <paramref name="obj"/>
    /// and the <paramref name="mi"/> to call the resulting method.
    /// </summary>
    /// <remarks>
    /// This method handles static and non-static property and field access, attached property
    /// access and indexers. If the last path segment is a method invocation expression,
    /// this method returns <c>true</c>, else it returns <c>false</c>.
    /// </remarks>
    /// <param name="start">Object used as starting point for the path evaluation.
    /// This may be an <see cref="object"/> or a <see cref="Type"/>.</param>
    /// <param name="obj">Reflected object on which the <paramref name="mi"/> may be
    /// evaluated. This parameter is only valid if the return value us <c>true</c>.</param>
    /// <param name="mi">Reflected method info to be evaluated on <paramref name="obj"/>.
    /// This parameter is only valid if the return value us <c>true</c>.</param>
    /// <returns><c>true</c>, if the path could be evaluated on object <paramref name="start"/>
    /// and the last path segment is a method invocation expression, else <c>false</c>.</returns>
    /// <exception cref="XamlBindingException">If this path could not be evaluated
    /// on object <paramref name="start"/>.</exception>
    public bool GetMethod(IDataDescriptor start,
        out object obj, out MethodInfo mi)
    {
      obj = null;
      mi = null;
      IDataDescriptor value = start;
      for (int i = 0; i < _pathSegments.Count - 1; i++)
      {
        IPathSegment ps = _pathSegments[i];
        if (!ps.Evaluate(value, out value))
          return false;
        if (value == null)
          return false;
      }
      return _pathSegments[_pathSegments.Count - 1].GetMethod(value, out obj, out mi);
    }

    #endregion

    #region Protected parsing methods

    protected static int ParseIndices(IParserContext context, string path,
        int pos, out object[] indices)
    {
      if (path[pos] != '[')
      {
        indices = null;
        return pos;
      }
      int indexerEnd = path.IndexOf(']', pos);
      if (indexerEnd < pos + 1)
        throw new XamlParserException("Path '{0}': Error in indexer expression at position {1}", path, pos);
      string indexExpression = path.Substring(pos + 1, (indexerEnd - pos) - 1);
      indices = ParserHelper.ParseIndexExpression(context, indexExpression);
      return indexerEnd + 1;
    }

    protected static int ParsePathSegment(IParserContext context, string path, int pos, out IPathSegment result)
    {
      pos = ParserHelper.SkipSpaces(path, pos);
      if (path[pos] == '[')
      { // Indexer expression
        object[] indices;
        pos = ParseIndices(context, path, pos, out indices);
        result = new IndexerPathSegment(indices);
        return pos;
      }
      else if (path[pos] == '(')
      { // Attached property
        int bracket = path.IndexOf(")", pos);
        if (bracket == -1)
          throw new XamlParserException("Path '{0}': ')' expected", path);
        int dot = path.IndexOf('.', pos);
        if (dot == -1 || dot > bracket)
          throw new XamlParserException("Path '{0}': Attached property expected", path);
        string propertyProvider = path.Substring(pos + 1, dot - pos - 1);
        string propertyName = path.Substring(dot + 1, bracket - dot - 1);
        string namespaceURI;
        context.LookupNamespace(propertyProvider, out propertyProvider, out namespaceURI);
        result = new AttachedPropertyPathSegment(context, propertyProvider, propertyName, namespaceURI);
        return bracket + 1;
      }
      else
      { // Member
        int end = path.IndexOfAny(new char[] { '.', '[' }, pos);
        if (end == -1)
          end = path.Length;
        result = new MemberPathSegment(path.Substring(pos, end - pos));

        if (path.IndexOf('[', pos) == end)
        { // Index follows member name
          object[] indices;
          end = ParseIndices(context, path, end, out indices);
          ((MemberPathSegment) result).SetIndices(indices);
        }
        return end;
      }
    }

    #endregion

    #region IEnumerable<IPathSegment> implementation

    IEnumerator<IPathSegment> IEnumerable<IPathSegment>.GetEnumerator()
    {
      return _pathSegments.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return _pathSegments.GetEnumerator();
    }

    #endregion

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      foreach (IPathSegment ps in _pathSegments)
      {
        if (result.Length > 0)
          result.Append('.');
        result.Append(ps.ToString());
      }
      return result.ToString();
    }
  }
}
