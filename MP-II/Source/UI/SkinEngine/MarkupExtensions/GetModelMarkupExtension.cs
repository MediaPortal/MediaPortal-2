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
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  public class GetModelMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    protected string _id;

    #endregion

    public GetModelMarkupExtension() { }

    public GetModelMarkupExtension(string id)
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

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      if (Id == null)
        throw new XamlBindingException("GetModelMarkupExtension: Property Id has to be given");
      IModelLoader loader = context.GetContextVariable(typeof(IModelLoader)) as IModelLoader;
      if (loader == null)
        throw new XamlBindingException("GetModelMarkupExtension: No model loader instance present in parser context");
      return loader.GetOrLoadModel(new Guid(Id));
    }

    #endregion
  }
}
