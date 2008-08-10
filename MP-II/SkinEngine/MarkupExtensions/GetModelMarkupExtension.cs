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

using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.SkinEngine.Models;

namespace MediaPortal.SkinEngine.MarkupExtensions
{
  public class GetModelMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    protected string _assemblyName = null;
    protected string _className = null;

    #endregion

    public GetModelMarkupExtension() { }

    #region Properties

    public string AssemblyName
    {
      get { return _assemblyName; }
      set { _assemblyName = value; }
    }

    public string ClassName
    {
      get { return _className; }
      set { _className = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      if (AssemblyName == null || ClassName == null)
        throw new XamlBindingException("GetModelMarkupExtension: Both properties AssemblyName and ClassName have to be set");
      Model model = ModelManager.Instance.GetOrLoadModel(AssemblyName, ClassName);
      if (model == null)
        throw new XamlBindingException("GetModelMarkupExtension: Unknown model: {0}.{1}", AssemblyName, ClassName);
      return model.Instance;
    }

    #endregion
  }
}
