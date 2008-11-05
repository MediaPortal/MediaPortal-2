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

    protected string _registrationLocation = null;
    protected string _id = null;

    #endregion

    public GetModelMarkupExtension() { }

    #region Properties

    public string RegistrationLocation
    {
      get { return _registrationLocation; }
      set { _registrationLocation = value; }
    }

    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      if (RegistrationLocation == null || Id == null)
        throw new XamlBindingException("GetModelMarkupExtension: Both properties RegistrationLocation and Id have to be set");
      Model model = ModelManager.Instance.GetOrLoadModel(RegistrationLocation, Id);
      if (model == null)
        throw new XamlBindingException("GetModelMarkupExtension: Unknown model: {0}.{1}", RegistrationLocation, Id);
      return model.Instance;
    }

    #endregion
  }
}
