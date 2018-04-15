#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  public class ElementProvider<T> : IDeepCopyable, IUnmodifiableResource, IInitializable where T : class
  {
    #region Protected fields

    protected string _path = string.Empty;
    protected PathExpression _compiledPath = null;
    protected object _owner = null;

    #endregion

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      ElementProvider<T> ep = (ElementProvider<T>) source;
      _owner = copyManager.GetCopy(ep._owner);
    }

    /// <summary>
    /// Gets or sets a property path expression to the data string corresponding to a given object.
    /// </summary>
    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    public T GetElement(object source)
    {
      IDataDescriptor result;
      _compiledPath.Evaluate(new ValueDataDescriptor(source), out result);
      if (result == null)
        return null;
      return (T) TypeConverter.Convert(result.Value, typeof(T));
    }

    #region IUnmodifyableResource implementation

    public object Owner
    {
      get { return _owner; }
      set { _owner = value; }
    }

    #endregion

    #region IBindingContainer implementation

    void IBindingContainer.AddBindings(IEnumerable<IBinding> bindings)
    {
      // We don't bind bindings - simply ignore them
    }

    #endregion

    #region IInitializable implementation

    void IInitializable.StartInitialization(IParserContext context)
    {}

    void IInitializable.FinishInitialization(IParserContext context)
    {
      if (_path == null)
        throw new XamlBindingException("{0}: Path mustn't be null", GetType().Name);
      _compiledPath = PathExpression.Compile(context, _path);
      ResourceDictionary.RegisterUnmodifiableResourceDuringParsingProcess(this, context);
    }

    #endregion
  }
}