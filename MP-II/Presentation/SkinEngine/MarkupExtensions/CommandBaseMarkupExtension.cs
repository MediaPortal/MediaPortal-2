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
using System.Reflection;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.MpfElements.Resources;
using Presentation.SkinEngine.Xaml;
using Presentation.SkinEngine.Xaml.Interfaces;

namespace Presentation.SkinEngine.MarkupExtensions
{

  /// <summary>
  /// Base class for the MPF Command and CommandStencil markup extensions.
  /// </summary>
  public class CommandBaseMarkupExtension : DependencyObject, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected string _path = null;

    protected PathExpression _compiledPath = null;

    #endregion

    #region Ctor

    public CommandBaseMarkupExtension()
    {
      GetOrCreateDataContext();
    }

    public CommandBaseMarkupExtension(string path)
    {
      GetOrCreateDataContext();
      _path = path;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandBaseMarkupExtension cbme = source as CommandBaseMarkupExtension;
      DataContext.Source = copyManager.GetCopy(cbme.DataContext.Source);
      DataContext.ElementName = copyManager.GetCopy(cbme.DataContext.ElementName);
      DataContext.RelativeSource = copyManager.GetCopy(cbme.DataContext.RelativeSource);
      _path = copyManager.GetCopy(cbme._path);
      _compiledPath = copyManager.GetCopy(cbme._compiledPath);
    }

    #endregion

    #region Public properties

    public object Source
    {
      get { return DataContext.Source; }
      set { DataContext.Source = value; }
    }

    public string ElementName
    {
      get { return DataContext.ElementName; }
      set { DataContext.ElementName = value; }
    }

    public RelativeSource RelativeSource
    {
      get { return DataContext.RelativeSource; }
      set { DataContext.RelativeSource = value; }
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
      _compiledPath = PathExpression.Compile(context, _path);
      return this;
    }

    #endregion

    /// <summary>
    /// Executes this command with the specified <paramref name="parameters"/>. The parameter
    /// enumeration will be automatically converted to the formal parameters of the target method.
    /// <see cref="LateBoundValue"/> instances will be also resolved when used as parameters.
    /// </summary>
    /// <param name="parameters"></param>
    public void Execute(IEnumerable<object> parameters)
    {
      IDataDescriptor start;
      if (!DataContext.Evaluate(out start))
        return;
      object obj;
      MethodInfo mi;
      _compiledPath.GetMethod(start, out obj, out mi);
      if (mi == null)
        return;
      ParameterInfo[] parameterInfos = mi.GetParameters();
      IList<object> paramsList = LateBoundValue.ConvertLateBoundValues(parameters);
      object[] convertedParameters;
      if (ParserHelper.ConsumeParameters(paramsList, parameterInfos, true, out convertedParameters))
        mi.Invoke(obj, convertedParameters);
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
      string[] sl = new string[l.Count];
      l.CopyTo(sl, 0);
      return "{" + CommandTypeName + " " + string.Join(",", sl) + "}";
    }

    #endregion
  }
}
