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
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{

  /// <summary>
  /// Base class for the MPF Command and CommandStencil markup extensions.
  /// </summary>
  public class CommandBaseMarkupExtension : DependencyObject, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected BindingMarkupExtension _source;
    protected string _path = null;

    protected PathExpression _compiledPath = null;

    #endregion

    #region Ctor

    public CommandBaseMarkupExtension()
    {
      _source = new BindingMarkupExtension(this);
    }

    public CommandBaseMarkupExtension(string path): this()
    {
      _path = path;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandBaseMarkupExtension cbme = (CommandBaseMarkupExtension) source;
      _source.Source = copyManager.GetCopy(cbme._source.Source);
      _source.ElementName = copyManager.GetCopy(cbme._source.ElementName);
      _source.RelativeSource = copyManager.GetCopy(cbme._source.RelativeSource);
      _path = copyManager.GetCopy(cbme._path);
      _compiledPath = copyManager.GetCopy(cbme._compiledPath);
    }

    #endregion

    #region Public properties

    public object Source
    {
      get { return _source.Source; }
      set { _source.Source = value; }
    }

    public string ElementName
    {
      get { return _source.ElementName; }
      set { _source.ElementName = value; }
    }

    public RelativeSource RelativeSource
    {
      get { return _source.RelativeSource; }
      set { _source.RelativeSource = value; }
    }

    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    #endregion

    #region Protected properties and methods

    /// <summary>
    /// Returns the XAML name of this command class.
    /// This is for debugging purposes only - ToString() method.
    /// </summary>
    protected virtual string CommandTypeName
    {
      get { return "[-CommandBase-]"; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    public object Evaluate(IParserContext context)
    {
      if (_path == null)
        throw new XamlBindingException("CommandBaseMarkupExtension: Path mustn't be null");
      _compiledPath = PathExpression.Compile(context, _path);
      return this;
    }

    #endregion

    /// <summary>
    /// Executes this command with the specified <paramref name="parameters"/>. The parameter
    /// enumeration will be automatically converted to the formal parameters of the target method.
    /// <see cref="LateBoundValue"/> instances will be also resolved when used as parameters.
    /// </summary>
    /// <param name="parameters">Enumeration of actual parameters to be used for the command
    /// execution.</param>
    public void Execute(IEnumerable<object> parameters)
    {
      IDataDescriptor start;
      if (!_source.Evaluate(out start))
      {
        ServiceScope.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find source value, could not execute command ({0})",
            ToString());
        return;
      }
      object obj;
      MethodInfo mi;
      _compiledPath.GetMethod(start, out obj, out mi);
      if (mi == null)
      {
        ServiceScope.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find method, could not execute command ({0})",
            ToString());
        return;
      }
      ParameterInfo[] parameterInfos = mi.GetParameters();
      IList<object> paramsList = LateBoundValue.ConvertLateBoundValues(parameters);
      try
      {
        object[] convertedParameters;
        if (ReflectionHelper.ConsumeParameters(paramsList, parameterInfos, true, out convertedParameters))
          mi.Invoke(obj, convertedParameters);
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("CommandBaseMarkupExtension: Error executing command '{0}'", e, this);
      }
    }

    #region Base overrides

    public override string ToString()
    {
      IList<string> l = new List<string>();
      if (Source != null)
        l.Add("Source=" + Source);
      if (RelativeSource != null)
        l.Add("RelativeSource=" + RelativeSource);
      if (ElementName != null)
        l.Add("ElementName=" + ElementName);
      if (!string.IsNullOrEmpty(Path))
        l.Add("Path=" + Path);
      return "{" + CommandTypeName + " " + StringUtils.Join(",", l) + "}";
    }

    #endregion
  }
}
