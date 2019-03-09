#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
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
  public abstract class CommandBaseMarkupExtension : DependencyObject, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected BindingExtension _source;
    protected string _path = null;

    protected PathExpression _compiledPath = null;

    #endregion

    #region Ctor

    protected CommandBaseMarkupExtension()
    {
      _source = new BindingExtension(this);
    }

    protected CommandBaseMarkupExtension(string path): this()
    {
      _path = path;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandBaseMarkupExtension cbme = (CommandBaseMarkupExtension) source;
      _source.Source = copyManager.GetCopy(cbme._source.Source);
      _source.ElementName = cbme._source.ElementName;
      _source.RelativeSource = cbme._source.RelativeSource;
      _path = cbme._path;
      _compiledPath = cbme._compiledPath;
      _source.Converter = copyManager.GetCopy(cbme._source.Converter);
      _source.ConverterParameter = copyManager.GetCopy(cbme._source.ConverterParameter);
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

    public RelativeSourceExtension RelativeSource
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
    protected abstract string CommandTypeName { get; }

    #endregion

    #region IEvaluableMarkupExtension implementation

    void IEvaluableMarkupExtension.Initialize(IParserContext context)
    {
      if (_path == null)
        throw new XamlBindingException("CommandBaseMarkupExtension: Path mustn't be null");
      _compiledPath = PathExpression.Compile(context, _path);
    }

    bool IEvaluableMarkupExtension.Evaluate(out object value)
    {
      value = this;
      return true;
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
        ServiceRegistration.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find source value, could not execute command ({0})",
            ToString());
        return;
      }
      IList<object> paramsList = LateBoundValue.ConvertLateBoundValues(parameters);
      object obj;
      MethodInfo mi;
      _compiledPath.GetMethod(start, paramsList.Count, out obj, out mi);
      if (mi == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find method, could not execute command ({0})",
            ToString());
        return;
      }
      ParameterInfo[] parameterInfos = mi.GetParameters();
      try
      {
        object[] convertedParameters;
        if (ReflectionHelper.ConsumeParameters(paramsList, parameterInfos, true, out convertedParameters))
          mi.Invoke(obj, convertedParameters);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("CommandBaseMarkupExtension: Error executing command '{0}'", e, this);
      }
    }

    /// <summary>
    /// Returns the delegate for the command, matching the given delegate type
    /// </summary>
    /// <param name="delegateType">Type of the delegate</param>
    /// <returns>Delegate of the command method.</returns>
    public Delegate GetDelegate(Type delegateType)
    {
      // get number of parameters from delegate type
      var miInvoke = delegateType.GetMember("Invoke").FirstOrDefault() as MethodInfo;
      if (miInvoke == null)
      {
        throw new ArgumentException(@"Type is not a valid delegate", "delegateType");
      }
      int parameterCount = miInvoke.GetParameters().Length;

      IDataDescriptor start;
      if (!_source.Evaluate(out start))
      {
        ServiceRegistration.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find source value, could not create delegate for command ({0})",
            ToString());
        return null;
      }

      object obj;
      MethodInfo mi;
      _compiledPath.GetMethod(start, parameterCount, out obj, out mi);
      if (mi == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("CommandBaseMarkupExtension: Could not find method, could not create delegate for command ({0})",
            ToString());
        return null;
      }
      try
      {
        return mi.CreateDelegate(delegateType, obj);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("CommandBaseMarkupExtension: Error creating delegate for command '{0}'", e, this);
        return null;
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
