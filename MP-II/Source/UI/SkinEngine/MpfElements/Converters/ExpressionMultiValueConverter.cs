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
using Jyc.Expr;
using MediaPortal.SkinEngine.MarkupExtensions;
using MediaPortal.SkinEngine.Xaml;
using Parser=Jyc.Expr.Parser;

namespace MediaPortal.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Multi value converter which evaluates an expression based on given values.
  /// </summary>
  /// <remarks>
  /// Supported operators:
  /// <list type="bullet">
  /// <item>Arithmetic: +, -, *, /</item>
  /// <item>Boolean: !, ||, &&, ^</item>
  /// <item>String: +</item>
  /// <item>Relational: ==, !=, <, >, <=, >=</item>
  /// <item>Conditional: ?:</item>
  /// </list>
  /// Supported types:
  /// <list type="bullet">
  /// <item>int</item>
  /// <item>float/double</item>
  /// <item>string</item>
  /// <item>bool</item>
  /// </list>
  /// </remarks>
  public class ExpressionMultiValueConverter : IMultiValueConverter
  {
    #region IMultiValueConverter implementation

    /// <summary>
    /// Evaluates a simple expression, given via the parameter <paramref name="parameter"/>.
    /// </summary>
    /// <remarks>
    /// This converter will often be used in XAML files. Note that in XAML, an attribute beginning with a '{' character
    /// is interpreted as an invocation of a markup extension. So the expression "{0} || {1}" must be escaped like this:
    /// "{}{0} || {1}". Note also that the boolean AND operator (&&) must be escaped too like this: "{}{0} &amp;&amp; {1}".
    /// </remarks>
    /// <param name="values">The values used for the variables {0} .. {n}.</param>
    /// <param name="targetType">Type to that the evaluated result should be converted.</param>
    /// <param name="parameter">String containing the expression. Variables can be accessed via numbers in
    /// curly braces, for example "!({0} || {2})". The variables are mapped to the values specified by
    /// the <paramref name="values"/> array.</param>
    /// <param name="result">Will return the evaluated result of the given <paramref name="targetType"/>.</param>
    public bool Convert(IDataDescriptor[] values, Type targetType, object parameter, out object result)
    {
      result = null;
      string expression = parameter as string;
      if (string.IsNullOrEmpty(expression))
        return false;
			try
			{
        // We're using an expression parser from "devilplusplus", "C# Eval function"
        // See http://www.codeproject.com/KB/dotnet/Expr.aspx
        // The parser was slightly adapted to our needs:
        // - To access a variable, the variable identifier has to be written in curly braces, for example:
        //   {0} + {1}

        Parser ep = new Parser();
        Evaluator evaluator = new Evaluator();
        ParameterVariableHolder pvh = new ParameterVariableHolder();

        // The used expression parser supports access to static functions for those of the parameters whose type is a class.
        // We could add classes here like the code commented out below. To access a static member on the string class,
        // the expression could be for example: {string}.Empty
        // For now, we don't need this functionality, so we don't add types (Albert, 2009-04-22).

        //pvh.Parameters["char"] = new Parameter(typeof(char));
        //pvh.Parameters["sbyte"] = new Parameter(typeof(sbyte));
        //pvh.Parameters["byte"] = new Parameter(typeof(byte));
        //pvh.Parameters["short"] = new Parameter(typeof(short)); 
        //pvh.Parameters["ushort"] = new Parameter(typeof(ushort)); 
        //pvh.Parameters["int"] = new Parameter(typeof(int));
        //pvh.Parameters["uint"] = new Parameter(typeof(uint));
        //pvh.Parameters["long"] = new Parameter(typeof(string));
        //pvh.Parameters["ulong"] = new Parameter(typeof(ulong));
        //pvh.Parameters["float"] = new Parameter(typeof(float));
        //pvh.Parameters["double"] = new Parameter(typeof(double));
        //pvh.Parameters["decimal"] = new Parameter(typeof(decimal));
        //pvh.Parameters["DateTime"] = new Parameter(typeof(DateTime));
        //pvh.Parameters["string"] = new Parameter(typeof(string));

        //pvh.Parameters["Guid"] = new Parameter(typeof(Guid));

        //pvh.Parameters["Convert"] = new Parameter(typeof(Convert));
        //pvh.Parameters["Math"] = new Parameter(typeof(Math));
        //pvh.Parameters["Array"] = new Parameter(typeof(Array));
        //pvh.Parameters["Random"] = new Parameter(typeof(Random));
        //pvh.Parameters["TimeZone"] = new Parameter(typeof(TimeZone));

        // Add child binding values
			  for (int i = 0; i < values.Length; i++)
			  {
			    IDataDescriptor value = values[i];
			    pvh.Parameters[i.ToString()] = new Parameter(value.Value, value.DataType);
			  }
        evaluator.VariableHolder = pvh;
        Tree tree = ep.Parse(expression);
        result = evaluator.Eval(tree);
        return TypeConverter.Convert(result, targetType, out result);
			}
			catch(Exception)
			{
			  return false;
			}
    }

    #endregion
  }
}
