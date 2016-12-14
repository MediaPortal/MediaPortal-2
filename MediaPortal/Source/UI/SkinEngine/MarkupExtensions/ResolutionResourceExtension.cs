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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF ResolutionResource markup extension. The ResolutionResource markup extension works
  /// similar as the ThemeResource markup extension, but allows to select resources based on Skin's resolution:
  /// The specified <see cref="ResourceKey"/> will be searched in the theme first,
  /// then the search will continue in the current parser context.
  /// The lookup process internally appends Skin heights to <see cref="ResourceKey"/> to prefer best matching
  /// values.
  /// </summary>
  /// <example>
  /// {ResolutionResource ImageWidth} will be evaluated:
  /// For a 1280x720 skin:
  /// - {ThemeResource ImageWidth720}
  /// - {ThemeResource ImageWidth}
  /// 
  /// For a 1920x1080 skin:
  /// - {ThemeResource ImageWidth1080}
  /// - {ThemeResource ImageWidth720}
  /// - {ThemeResource ImageWidth}
  /// </example>
  public class ResolutionResourceExtension : StaticResourceBase, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected static readonly Dictionary<int, List<string>> _fallbacks = new Dictionary<int, List<string>>
    {
      // In future there could be 4k Skins, then we need to add a new config here
      { 1080, new List<string>{"1080", "720", ""} },
      { 720, new List<string>{"720", ""} }
    };

    protected string _resourceKey = null;

    protected object _resource = null;

    #endregion

    public ResolutionResourceExtension() { }

    public ResolutionResourceExtension(string resourceKey)
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

    void IEvaluableMarkupExtension.Initialize(IParserContext context)
    {
      var skinHeight = SkinContext.SkinResources.SkinHeight;
      List<string> fallbacks;
      if (!_fallbacks.TryGetValue(skinHeight, out fallbacks))
        fallbacks = new List<string> { string.Empty };

      // First try all theme resources, then resources in parser context
      foreach (string fallback in fallbacks)
      {
        _resource = FindResourceInTheme(_resourceKey + fallback, context);
        if (_resource != null)
          break;
      }
      if (_resource == null)
        foreach (string fallback in fallbacks)
        {
          _resource = ResourceDictionary.FindResourceInParserContext(_resourceKey + fallback, context);
          if (_resource != null)
            break;
        }

      if (_resource == null)
        ServiceRegistration.Get<ILogger>().Error("ResolutionResourceExtension: Resource '{0}' not found", _resourceKey);
    }

    bool IEvaluableMarkupExtension.Evaluate(out object value)
    {
      value = _resource;
      return _resource != null;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("ResolutionResource ResourceKey={0}", _resourceKey);
    }

    #endregion
  }
}
