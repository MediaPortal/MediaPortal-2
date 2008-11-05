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
using System.Collections;
using System.Globalization;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaPortal.SkinEngine.Commands.Expression
{
  public delegate void AdditionalFunctionEventHandler(object sender, AdditionalFunctionEventArgs e);
  public delegate void AdditionalVariableEventHandler(object sender, AdditionalVariableEventArgs e);
  /// <summary>
  /// This class provides functionality for evaluating functions
  /// </summary>
  //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
  public class FunctionEval : IExpression
  {
    #region Private Members

    private string _expression = "";
    private string _function = "";
    private bool _bParsed;
    private object[] _params;
    internal Hashtable _variables = new Hashtable();

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the expression to be evaluated
    /// </summary>
    /// <value></value>
    public string Expression
    {
      get { return _expression; }
      set
      {
        _expression = value;
        _function = "";
        _bParsed = false;
        _params = null;
      }
    }

    #endregion

    #region Construction

    /// <summary>
    /// Default Constructor
    /// </summary>
    public FunctionEval() { }

    /// <summary>
    /// Initializes the Expression Property
    /// </summary>
    /// <param name="expression">Expression to evaluate</param>
    public FunctionEval(string expression)
    { Expression = expression; }

    #endregion

    #region Methods

    /// <summary>
    /// Evaluates the Expression
    /// </summary>
    /// <returns></returns>
    public object Evaluate()
    {
      object ret = null;
      if (!_bParsed)
      {
        StringBuilder strbRet = new StringBuilder(Expression);
        string strNext = strbRet.ToString();
        Match m = DefinedRegex.Function.Match(strNext);

        if (m.Success)
        {
          _params = GetParameters(m);
          _function = m.Groups["Function"].Value;
        }
        _bParsed = true;
      }
      ret = ExecuteFunction(_function, _params);
      return ret;
    }

    /// <summary>
    /// Evaluates a string expression of a function
    /// </summary>
    /// <param name="expression"></param>
    /// <returns>evauluated value</returns>
    public static object Evaluate(string expression)
    {
      FunctionEval expr = new FunctionEval(expression);
      return expr.Evaluate();
    }

    /// <summary>
    /// Evaluates a string expression of a function
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="handler">attach a custom function handler</param>
    /// <returns>evauluated value</returns>
    public static object Evaluate(string expression, AdditionalFunctionEventHandler handler, AdditionalVariableEventHandler handler2)
    {
      FunctionEval expr = new FunctionEval(expression);
      if (handler != null)
      {
        expr.AdditionalFunctionEventHandler += handler;
      }
      if (handler2 != null)
      {
        expr.AdditionalVariableEventHandler += handler2;
      }
      return expr.Evaluate();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string Replace(string input)
    {
      FunctionEval expr = new FunctionEval(input);
      return expr.Replace();
    }

    /// <summary>
    /// This routine will replace functions existing in a input string with thier respective values
    /// </summary>
    /// <param name="input">input string</param>
    /// <param name="handler">Additional function handler for custom functions</param>
    /// <returns>input string with all found functions replaced with returned values</returns>
    public static string Replace(string input, AdditionalFunctionEventHandler handler, AdditionalVariableEventHandler handler2)
    {
      FunctionEval expr = new FunctionEval(input);
      if (handler != null)
        expr.AdditionalFunctionEventHandler += handler;
      if (handler2 != null)
      {
        expr.AdditionalVariableEventHandler += handler2;
      }
      return expr.Replace();
    }

    /// <summary>
    /// Since the static replace will not allow a second Replace(string), Replace(ex) will do so with
    /// this instance (so that variables will work)
    /// </summary>
    /// <param name="input">input string</param>
    /// <returns>filtered string</returns>
    public string ReplaceEx(string input)
    {
      if ("" + input == "")
        return "";
      Expression = input;
      return Replace();
    }

    /// <summary>
    /// This routine will replace functions existing in the Expression property with thier respective values
    /// </summary>
    /// <returns>Expression string with all found functions replaced with returned values</returns>
    public string Replace()
    {
      StringBuilder strbRet = new StringBuilder(Expression);
      Match m = DefinedRegex.Function.Match(Expression);

      while (m.Success)
      {
        int nDepth = 1;
        int nIdx = m.Index + m.Length;
        //Get the parameter string
        while (nDepth > 0)
        {
          if (nIdx >= strbRet.Length)
            throw new ArgumentException("Missing ')' in Expression");
          if (strbRet[nIdx] == ')')
            nDepth--;
          if (strbRet[nIdx] == '(')
            nDepth++;
          nIdx++;
        }
        string expression = strbRet.ToString(m.Index, nIdx - m.Index);
        FunctionEval eval = new FunctionEval(expression);
        eval.AdditionalFunctionEventHandler += this.AdditionalFunctionEventHandler;
        eval.AdditionalVariableEventHandler += this.AdditionalVariableEventHandler;
        eval._variables = this._variables;
        strbRet.Replace(expression, "" + eval.Evaluate());
        m = DefinedRegex.Function.Match(strbRet.ToString());
      }

      //Replace Variable in the path!
      m = DefinedRegex.Variable.Match(strbRet.ToString());
      while (m.Success)
      {
        strbRet.Replace(m.Value, "" + _variables[m.Groups["Variable"].Value]);
        m = DefinedRegex.Variable.Match(strbRet.ToString());
      }

      return strbRet.ToString();
    }

    /// <summary>
    /// Sets a variable's value
    /// </summary>
    /// <param name="key">variable name</param>
    /// <param name="value">variable value</param>
    public void SetVariable(string key, object value)
    {
      ClearVariable(key);
      _variables.Add(key, value);
    }

    /// <summary>
    /// Clear's a variable's value
    /// </summary>
    /// <param name="key">variable name</param>
    public void ClearVariable(string key)
    {
      if (_variables.ContainsKey(key))
        _variables.Remove(key);
    }

    /// <summary>
    /// string override, return Expression property
    /// </summary>
    /// <returns>returns Expression property</returns>
    public override string ToString() { return Expression; }

    /// <summary>
    /// returns the parameters of a function
    /// </summary>
    /// <param name="m">regex math value</param>
    /// <returns></returns>
    private object[] GetParameters(Match m)
    {
      string strParams = "";
      int nIdx = m.Index + m.Length;
      int nDepth = 1;
      int nLast = 0;
      bool bInQuotes = false;
      ArrayList ret = new ArrayList();

      //Get the parameter string
      while (nDepth > 0)
      {
        if (nIdx >= Expression.Length)
          throw new ArgumentException("Missing ')' in Expression");

        if (!bInQuotes && Expression[nIdx] == ')')
          nDepth--;
        if (!bInQuotes && Expression[nIdx] == '(')
          nDepth++;

        if ((Expression[nIdx] == '"' || Expression[nIdx] == '\'') && (nIdx == 0 || Expression[nIdx - 1] != '\\'))
          bInQuotes = !bInQuotes;

        if (nDepth > 0)
          nIdx++;
      }
      strParams = Expression.Substring(m.Index + m.Length, nIdx - (m.Index + m.Length));

      if ("" + strParams == "")
        return null;

      bInQuotes = false;
      for (nIdx = 0; nIdx < strParams.Length; nIdx++)
      {
        if (!bInQuotes && strParams[nIdx] == ')')
          nDepth--;
        if (!bInQuotes && strParams[nIdx] == '(')
          nDepth++;

        if ((strParams[nIdx] == '"' || strParams[nIdx] == '\'') && (nIdx == 0 || strParams[nIdx - 1] != '\\'))
          bInQuotes = !bInQuotes;

        if (!bInQuotes && nDepth == 0 && strParams[nIdx] == ',')
        {
          ret.Add(strParams.Substring(nLast, nIdx - nLast));
          nLast = nIdx + 1;
        }
      }
      ret.Add(strParams.Substring(nLast, nIdx - nLast));

      for (nIdx = 0; nIdx < ret.Count; nIdx++)
      {
        ExpressionEval eval = new ExpressionEval(ret[nIdx].ToString());
        eval.AdditionalFunctionEventHandler += AdditionalFunctionEventHandler;
        eval.AdditionalVariableEventHandler += AdditionalVariableEventHandler;
        eval._variables = this._variables;
        ret[nIdx] = eval;
      }
      return ret.ToArray();
    }

    /// <summary>
    /// Executes the function based upon the name of the function
    /// </summary>
    /// <param name="name">name of the function to execute</param>
    /// <param name="p">parameter list</param>
    /// <returns>returned value of executed function</returns>
    //[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private object ExecuteFunction(string name, object[] p)
    {
      object[] parameters = null;
      if (p != null)
      {
        parameters = (object[])p.Clone();
        if (name.ToLower(CultureInfo.CurrentCulture) == "if")
        {
          return Iif(parameters);
        }
      }
      parameters = null;
      if (p != null)
      {
        parameters = (object[])p.Clone();
        for (int x = 0; x < parameters.Length; x++)
          if (parameters[x] is IExpression)
            parameters[x] = ((IExpression)parameters[x]).Evaluate();
      }
      switch (name.ToLower(CultureInfo.CurrentCulture))
      {
        // Math functions
        case "sin": return Math.Sin(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "cos": return Math.Cos(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "tan": return Math.Tan(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "sec": return 1 / Math.Cos(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "csc": return 1 / Math.Sin(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "cot": return 1 / Math.Tan(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "asin": return Math.Asin(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "acos": return Math.Acos(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "atan": return Math.Atan(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "sinh": return Math.Sinh(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "cosh": return Math.Cosh(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "tanh": return Math.Tanh(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "abs": return Math.Abs(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "sqrt": return Math.Sqrt(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "ciel": return Math.Ceiling(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "floor": return Math.Floor(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "exp": return Math.Exp(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "log10": return Math.Log10(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "log": return (parameters.Length > 1) ?
                            Math.Log(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture), Convert.ToDouble(parameters[1], CultureInfo.CurrentCulture)) :
                            Math.Log(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "max": return Math.Max(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture), Convert.ToDouble(parameters[1], CultureInfo.CurrentCulture));
        case "min": return Math.Min(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture), Convert.ToDouble(parameters[1], CultureInfo.CurrentCulture));
        case "pow": return Math.Pow(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture), Convert.ToDouble(parameters[1], CultureInfo.CurrentCulture));
        case "round": return (parameters.Length > 1) ?
                              Math.Round(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture), Convert.ToInt32(parameters[1], CultureInfo.CurrentCulture)) :
                              Math.Round(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        //case "trunc":   return Math.Truncate(Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture));
        case "e": return Math.E;
        case "pi": return Math.PI;

        //DateTime functions
        case "now": return DateTime.Now;
        case "today": return DateTime.Today;
        case "mindate": return DateTime.MinValue;
        case "maxdate": return DateTime.MaxValue;
        case "monthname": return (new DateTime(2000, Convert.ToInt32(parameters[0]), 1)).ToString("MMMM");
        case "adddays": return Convert.ToDateTime(parameters[0]).AddDays(Convert.ToInt32(parameters[1]));
        case "addmonths": return Convert.ToDateTime(parameters[0]).AddMonths(Convert.ToInt32(parameters[1]));
        case "addyears": return Convert.ToDateTime(parameters[0]).AddYears(Convert.ToInt32(parameters[1]));
        case "addhours": return Convert.ToDateTime(parameters[0]).AddHours(Convert.ToInt32(parameters[1]));
        case "addminutes": return Convert.ToDateTime(parameters[0]).AddMinutes(Convert.ToInt32(parameters[1]));
        case "addseconds": return Convert.ToDateTime(parameters[0]).AddSeconds(Convert.ToInt32(parameters[1]));

        //Formatting Functions
        case "fmtnum": return Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture).ToString("" + parameters[1], CultureInfo.CurrentCulture);
        case "fmtdate": return Convert.ToDateTime(parameters[0], CultureInfo.CurrentCulture).ToString("" + parameters[1], CultureInfo.CurrentCulture);

        //Numerical Expression Evaluation
        case "expr":
          {
            ExpressionEval eval = new ExpressionEval("" + parameters[0]);
            eval.AdditionalFunctionEventHandler += this.AdditionalFunctionEventHandler;
            eval.AdditionalVariableEventHandler += this.AdditionalVariableEventHandler;
            eval._variables = this._variables;
            return eval.Evaluate();
          }

        //Casting Functions
        case "cdbl": return Convert.ToDouble(parameters[0], CultureInfo.CurrentCulture);
        case "cint": return Convert.ToInt32(parameters[0], CultureInfo.CurrentCulture);
        case "clong": return Convert.ToInt64(parameters[0], CultureInfo.CurrentCulture);
        case "cuint": return Convert.ToUInt32(parameters[0], CultureInfo.CurrentCulture);
        case "culong": return Convert.ToUInt64(parameters[0], CultureInfo.CurrentCulture);
        case "cdatetime": return Convert.ToDateTime(parameters[0], CultureInfo.CurrentCulture);
        case "str": return parameters[0].ToString();

        //Logical Functions
        case "if": return Iif(parameters);
        case "case": return Case(parameters);

        //security fucntions
        case "currentuserid": return WindowsIdentity.GetCurrent().Name.ToLower();

        default: return AdditionalFunctionHelper(name, parameters);
      }
    }

    /// <summary>
    /// This method helps fire the event for any function not intercepted internally
    /// </summary>
    /// <param name="name">name of the function</param>
    /// <param name="parameters">parameters</param>
    /// <returns>returned from event</returns>
    protected object AdditionalFunctionHelper(string name, object[] parameters)
    {
      AdditionalFunctionEventArgs e = new AdditionalFunctionEventArgs(name, parameters);
      if (AdditionalFunctionEventHandler != null)
        AdditionalFunctionEventHandler(this, e);
      return e.ReturnValue;
    }

    /// <summary>
    /// Does the work for the IIF function
    /// </summary>
    /// <param name="parameters">
    /// parameters first is condition, 
    /// second is val if true, 
    /// third is val if false
    /// </param>
    /// <returns>params 2 if param 1 is true, otherwise param 3</returns>
    //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public static object Iif(params object[] parameters)
    {
      if (parameters.Length < 3)
        return "Invalid Number of Parameters: iif(condition, val if true, val if false)";

      if (parameters[0] is IExpression)
        parameters[0] = ((IExpression)parameters[0]).Evaluate();
      if (Convert.ToBoolean(parameters[0], CultureInfo.CurrentCulture))
      {
        if (parameters[1] is IExpression)
          parameters[1] = ((IExpression)parameters[1]).Evaluate();
        return parameters[1];
      }
      if (parameters[2] is IExpression)
        parameters[2] = ((IExpression)parameters[2]).Evaluate();
      return parameters[2];
    }

    /// <summary>
    /// Executes a case/when statement
    /// </summary>
    /// <param name="parameters">condition, val, condition2, val2, ...</param>
    /// <returns>returns the parameter after the first condition parameter that evaluates to true</returns>
    public static object Case(params object[] parameters)
    {
      if (parameters.Length % 2 != 0 && parameters.Length > 0)
        return "Invalid Number of Parameters: case(condition, val, condition2, val2, ...)";
      for (int x = 0; x < parameters.Length; x += 2)
      {
        if ("" + parameters[x] == "else")
          return parameters[x + 1];
        if (Convert.ToBoolean(parameters[x], CultureInfo.CurrentCulture))
          return parameters[x + 1];
      }
      return null;
    }

    #endregion

    #region Events

    /// <summary>
    /// This event will trigger for every function that is not intercepted internally
    /// </summary>
    public event AdditionalFunctionEventHandler AdditionalFunctionEventHandler;
    public event AdditionalVariableEventHandler AdditionalVariableEventHandler;

    #endregion
  }
}
