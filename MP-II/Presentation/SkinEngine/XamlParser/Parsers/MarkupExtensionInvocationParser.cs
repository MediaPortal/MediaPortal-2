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
using Presentation.SkinEngine.XamlParser.Parsers;

namespace Presentation.SkinEngine.XamlParser
{
  /// <summary>
  /// Holds some static helper methods for parsing markup extension invocations
  /// in the XAML parser.
  /// </summary>
  public class MarkupExtensionInvocationParser
  {
    /// <summary>
    /// Parses an expression for invoking a markup extension. The specified
    /// expression string must not contain the starting { and ending } characters.
    /// </summary>
    /// <param name="expr">Expression representing a markup extension invokation.
    /// This expression has the syntax
    /// <code>
    /// Markup-Invocation := [Expression-Name] <[Property]=[Value]<, ...>>
    /// </code>
    /// Every parameter itself can again be a simple value assignment or an
    /// invocation of another markup extension.
    /// In this case, the property assignment has the syntax
    /// <code>
    /// [Property]={[Markup-Invocation]}
    /// </code></param>
    /// <param name="extensionName">Will be assigned to the name of the markup
    /// extension to invoke.</param>
    /// <param name="parameters">Will contain a list of parameter assignments.</param>
    /// <param name="namedParams">Will contain the information, if the parameters
    /// are given in a <c>name = value</c> syntax or in a <c>param</c> syntax.
    /// Mixed syntax forms for different parameters are not supported.
    /// If no parameters were found, <paramref name="namedParams"/> will be set
    /// to <c>false</c>.</param>
    public static void ParseInvocation(string expr,
        out string extensionName, out IList<KeyValuePair<string, string>> parameters,
        out bool namedParams)
    {
      parameters = new List<KeyValuePair<string, string>>();
      int i = expr.IndexOf(' ');
      if (i == -1)
      {
        extensionName = expr;
        namedParams = false;
      }
      else
      {
        extensionName = expr.Substring(0, i);
        i = ParserHelper.SkipSpaces(expr, i);
        bool? allNamedParams = null;
        while (i < expr.Length)
        {
          string name;
          string value;
          i = ParseParameter(expr, i, out name, out value);
          parameters.Add(new KeyValuePair<string, string>(name, value));
          bool hasName = !string.IsNullOrEmpty(name);
          if (allNamedParams == null)
            allNamedParams = hasName;
          else if (allNamedParams.Value != hasName)
            throw new XamlParserException("Markup extension expression '{0}', position {1}: mixed named and unnamed parameters are not allowed", expr, i);
          i = ParserHelper.SkipSpaces(expr, i);
          if (i >= expr.Length)
            break; // End of expression reached - all parameters processed
          if (expr[i] != ',')
            throw new XamlParserException("Markup extension expression syntax '{0}' is invalid at position {1}", expr, i);
          i++;
          i = ParserHelper.SkipSpaces(expr, i);
          if (i >= expr.Length)
            throw new XamlParserException("Markup extension expression '{0}': parameter expected at position {1}", expr, i);
        }
        namedParams = (allNamedParams ?? false); // Default when no params were parsed: namedParams == false
      }
    }

    /// <summary>
    /// Given a parameter list returned by method
    /// <see cref="MarkupExtensionInvocationParser.ParseInvocation(string,out string,out List<KeyValuePair<string, string>>,bool)"/>,
    /// this method separates the value parts of the named parameter list and returns it.
    /// </summary>
    /// <param name="parameters">Parameter list in a name=value form.</param>
    /// <returns>Parameter values separated from <paramref name="parameters"/></returns>
    public static IList<string> ExtractParameterValues(IList<KeyValuePair<string, string>> parameters)
    {
      List<string> result = new List<string>();
      foreach (KeyValuePair<string, string> param in parameters)
        result.Add(param.Value);
      return result;
    }

    /// <summary>
    /// Parses the next parameter in the specified <paramref name="expr"/>.
    /// The returned value will indicate the index in this string following
    /// the parameter declaration.
    /// </summary>
    /// <remarks>
    /// Parses in the specified <paramref name="expr"/> beginning at the specified
    /// <paramref name="index"/> a XAML markup extension parameter specification
    /// of the form
    /// <code>
    /// 1)
    /// [Identifier]=[Value]
    /// </code>
    /// or
    /// <code>
    /// 2)
    /// [Value]
    /// </code>
    /// The value itself may be a simple string value or an invocation of
    /// another markup extension, surrounded by { and } characters.
    /// If form 1 is found, the returned parameter
    /// <paramref name="name"/> will contain the parsed name part, if form 2 is
    /// found, the <paramref name="name"/> parameter will be null.
    /// In both forms the <paramref name="value"/> parameter will contain the
    /// parsed value string.
    /// </remarks>
    /// <returns>The index in the <paramref name="expr"/> string following
    /// the parameter declaration.</returns>
    private static int ParseParameter(string expr, int index, out string name, out string value)
    {
      index = ParserHelper.SkipSpaces(expr, index);
      if (index >= expr.Length)
        throw new XamlParserException("Markup extension expression '{0}': '=' expected at position {1}", expr, index);

      name = null;
      if (expr[index] == '{') // Escaped value or markup extension invocation value
      {
        return ParseValue(expr, index, out value);
      }
      int start = index;
      // Remains to check: name=value or simple values
      index = expr.IndexOfAny(new char[] {' ', ',', '}', '='}, index);
      if (index == -1 || expr[index] != '=') // Check if name=value
      { // No name=value, ergo value
        value = expr.Substring(start); // We have here a simple value, we don't need to parse via ParseValue()
        index = expr.Length;
      }
      else
      { // expr[index] == '=', ergo name=value
        name = expr.Substring(start, index-start);
        // No spaces here!
        index++;
        // No spaces here!
        // Value
        index = ParseValue(expr, index, out value);
      }
      return index;
    }

    /// <summary>
    /// Parses a value expression in a markup extension parameter declaration.
    /// </summary>
    private static int ParseValue(string expr, int index, out string value)
    {
      bool escaped = false;
      if (expr.StartsWith("{}")) // Escape sequence, start reading after {}
      {
        index += 2;
        escaped = true;
      }
      int start = index;
      int level = 0;
      while (index < expr.Length &&
          (level > 0 || expr[index] != '}' && expr[index] != ' ' && expr[index] != ','))
      {
        if (!escaped && expr[index] == '{')
          level++;
        else if (!escaped && expr[index] == '}')
          level--;
        index++;
      }
      if (level > 0)
        throw new XamlParserException("Markup extension expression '{0}': '}' expected at position {1}", expr, index);
      value = expr.Substring(start, index-start);
      return index;
    }
  }
}
