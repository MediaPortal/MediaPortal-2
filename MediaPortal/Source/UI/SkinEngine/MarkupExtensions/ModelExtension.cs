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
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  public class ModelExtension: MPFExtensionBase, IEvaluableMarkupExtension, ISkinEngineManagedObject
  {

    #region Protected fields

    protected string _id = null;
    protected object _model = null;

    #endregion

    public ModelExtension() { }

    public ModelExtension(string id)
    {
      _id = id;
    }

    #region Properties

    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    void IEvaluableMarkupExtension.Initialize(IParserContext context)
    {
      if (Id == null)
        throw new XamlBindingException("GetModelMarkupExtension: Property Id has to be given");
      IModelLoader loader = context.GetContextVariable(typeof(IModelLoader)) as IModelLoader;
      if (loader == null)
        throw new XamlBindingException("GetModelMarkupExtension: No model loader instance present in parser context");
      _model = loader.GetOrLoadModel(new Guid(Id));
    }

    bool IEvaluableMarkupExtension.Evaluate(out object value)
    {
      value = _model;
      return _model != null;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("Model Id={0}", _id);
    }

    #endregion
  }
}
