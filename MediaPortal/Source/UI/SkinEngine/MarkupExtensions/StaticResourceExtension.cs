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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF StaticResource markup extension.
  /// </summary>
  public class StaticResourceExtension: StaticResourceBase, IEvaluableMarkupExtension
  {
    #region Protected fields

    protected string _resourceKey = null;

    protected object _resource = null;

    #endregion

    public StaticResourceExtension() { }

    public StaticResourceExtension(string resourceKey)
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
      _resource = ResourceDictionary.FindResourceInParserContext(_resourceKey, context) ?? FindResourceInTheme(_resourceKey, context);

      if (_resource == null)
        ServiceRegistration.Get<ILogger>().Error("StaticResourceMarkupExtension: Resource '{0}' not found", _resourceKey);
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
      return string.Format("StaticResource ResourceKey={0}", _resourceKey);
    }

    #endregion
  }
}
