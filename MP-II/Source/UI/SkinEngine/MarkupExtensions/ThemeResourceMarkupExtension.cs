#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF ThemeResource markup extension. The ThemeResource markup extension works
  /// similar as the StaticResource markup, only the search order for resources is modified:
  /// The specified <see cref="ResourceKey"/> will be searched in the theme first,
  /// then the search will continues in the current parser context.
  /// </summary>
  public class ThemeResourceMarkupExtension: StaticResourceBase, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected string _resourceKey;

    #endregion

    public ThemeResourceMarkupExtension() { }

    public ThemeResourceMarkupExtension(string resourceKey)
    {
      _resourceKey = resourceKey;
    }

    #region Properties

    public string ResourceKey
    {
      get { return _resourceKey; }
      set { _resourceKey = value; }
    }

    #endregion

    #region IEvaluableMarkupExtension implementation

    object IEvaluableMarkupExtension.Evaluate(IParserContext context)
    {
      object result = FindResourceInTheme(_resourceKey);
      if (result == null)
        result = FindResourceInParserContext(_resourceKey, context);

      if (result == null)
        throw new XamlBindingException("ThemeResourceMarkupExtension: Resource '{0}' not found", _resourceKey);
      return result;
    }

    #endregion
  }
}
