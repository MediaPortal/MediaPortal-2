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

using System.Collections.Generic;
using System;
using System.Reflection;

namespace Presentation.SkinEngine.XamlParser.Parsers
{
  public class ParserHelper
  {
    /// <summary>
    /// Skips all connected space characters in the specified <paramref name="expr"/>
    /// beginning at the specified <paramref name="index"/>.
    /// </summary>
    public static int SkipSpaces(string expr, int index)
    {
      while (index < expr.Length && expr[index] == ' ')
        index++;
      return index;
    }

    /// <summary>
    /// Parses a property index expression in the form <c>(sys:Int32)42,(sys:Int32)24,SampleString</c>,
    /// where sys is mapped to the system namespace.
    /// Inside indexers, multiple indexer parameters are separated by commas (,).
    /// The type of each parameter can be specified with parentheses.
    /// </summary>
    /// <param name="context">The current parser context.</param>
    /// <param name="expression">The index expression to parse. The index expression is
    /// the expression between the <c>[</c> and <c>]</c> characters, for example in
    /// <c>PropertyName[10,20]</c>, the index expression is <c>10,20</c>.</param>
    /// <returns>Object array containing index parameters from the
    /// <paramref name="expression"/>.</returns>
    public static object[] ParseIndexExpression(IParserContext context, string expression)
    {
      IList<object> indices = new List<object>();
      string[] indexStrings = expression.Split(new char[] {','});
      foreach (string s in indexStrings)
      {
        string valStr = s;
        Type explicitType = null;
        if (valStr.StartsWith("("))
        { // Explicit type is given
          int i = valStr.IndexOf(')');
          explicitType = ParseType(context, s.Substring(1, i - 1));
          valStr = valStr.Substring(i + 1);
        }
        object indexResult = valStr;
        if (explicitType != null)
          if (!TypeConverter.Convert(indexResult, explicitType, out indexResult))
            throw new XamlParserException("Could not convert '{0}' to type '{1}'", indexResult, explicitType.Name);
        indices.Add(indexResult);
      }
      object[] result = new object[indexStrings.Length];
      indices.CopyTo(result, 0);
      return result;
    }

    public static bool ConsumeParameters(object[] indices,
        ParameterInfo[] parameterInfos, bool mustMatchSignature,
        out object[] convertedIndices)
    {
      if (parameterInfos.Length == 0)
      {
        convertedIndices = null;
        return false;
      }
      Type[] indexTypes = new Type[parameterInfos.Length];
      int ti = 0;
      int numMandatory = 0;
      foreach (ParameterInfo parameter in parameterInfos)
      {
        indexTypes[ti++] = parameter.ParameterType;
        if (!parameter.IsOptional)
          numMandatory++;
      }
      bool result = ConvertTypes(indices, indexTypes, out convertedIndices);
      if (result && convertedIndices.Length <= indexTypes.Length &&
          convertedIndices.Length >= numMandatory)
        return true;
      else if (mustMatchSignature)
        throw new XamlBindingException("Wrong count of parameter for index (expected: {0}, got: {1})", parameterInfos.Length, indices == null ? 0 : indices.Length);
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
    public static bool ConvertTypes(object[] objects, Type[] types,
        out object[] convertedIndices)
    {
      // Convert objects to index types
      convertedIndices = new object[types.Length];
      int current = 0;
      foreach (object index in objects)
      {
        if (current >= types.Length)
          throw new XamlBindingException("Cannot convert object array, there are more objects than expected. Expected: {0}, got: {1}",
              types.Length, objects.Length);
        if (!TypeConverter.Convert(index, types[current], out convertedIndices[current]))
          return false;
        current++;
      }
      return true;
    }

    /// <summary>
    /// Given a string specifying a type, this method returns the type.
    /// </summary>
    /// <param name="context">The current document context.</param>
    /// <param name="typeName">Type string in the form <c>sys:Int32</c>, where the prefix
    /// is a prefix currently registered in the <paramref name="context"/>, and the suffix
    /// is a type known by the namespace handler specified by the prefix.</param>
    /// <returns>Type object for the specified type string.</returns>
    public static Type ParseType(IParserContext context, string typeName)
    {
      string localName;
      string namespaceURI;
      context.LookupNamespace(typeName, out localName, out namespaceURI);
      return context.GetNamespaceHandler(namespaceURI).GetElementType(localName, namespaceURI);
    }
  }
}
