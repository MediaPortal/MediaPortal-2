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

using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Resources;

namespace Presentation.SkinEngine.MarkupExtensions
{
  public class StaticResourceMarkupExtension: IEvaluableMarkupExtension
  {

    #region Protected fields

    protected string _resourceKey;

    #endregion

    public StaticResourceMarkupExtension() { }

    public StaticResourceMarkupExtension(string resourceKey)
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
      object result = null;
      // FIXME Albert78: Our current visual element's implementation doesn't support a materialized
      // logical tree, so we have to use the parser's context stack
      foreach (ElementContextInfo current in context.ContextStack)
      {
        if (current.ContainsKey(_resourceKey))
        {
          result = current.GetKeyedElement(_resourceKey);
          break;
        }
        else if (current.Instance is UIElement)
        {
          result = ((UIElement)current.Instance).FindResource(_resourceKey);
          if (result != null)
            break;
        }
        else if (current.Instance is ResourceDictionary)
        {
          ResourceDictionary rd = (ResourceDictionary)current.Instance;
          if (rd.ContainsKey(_resourceKey))
          {
            result = rd[_resourceKey];
            break;
          }
        }
      }

      if (result == null)
        throw new XamlBindingException("StaticResourceMarkupExtension: Resource '{0}' not found", _resourceKey);
      return result;
    }

    #endregion
  }
}
