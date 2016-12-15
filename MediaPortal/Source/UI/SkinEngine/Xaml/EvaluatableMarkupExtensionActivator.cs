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

using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  public class EvaluatableMarkupExtensionActivator
  {
    protected IParserContext _context;
    protected IEvaluableMarkupExtension _eme;
    protected IDataDescriptor _dd;

    public EvaluatableMarkupExtensionActivator(IParserContext context, IEvaluableMarkupExtension eme, IDataDescriptor dd)
    {
      _context = context;
      _eme = eme;
      _dd = dd;
    }

    public void Activate()
    {
      object value;
      if (!_eme.Evaluate(out value))
        throw new XamlBindingException("Could not evaluate markup extension '{0}'", _eme);
      _context.AssignValue(_dd, value);
    }
  }
}