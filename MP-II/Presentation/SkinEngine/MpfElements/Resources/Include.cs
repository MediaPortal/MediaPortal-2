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
using Presentation.SkinEngine.Loader;
using Presentation.SkinEngine.Controls.Visuals;

namespace Presentation.SkinEngine.MpfElements.Resources
{
  public class Include : IInclude, IInitializable
  {
    #region Private fields

    protected object _content = null;
    protected string _includeName = null;
    protected ResourceDictionary _resources;

    #endregion

    public Include()
    {
      Init();
    }

    void Init()
    {
      _resources = new ResourceDictionary();
    }

    public void SetResources(ResourceDictionary resources)
    {
      _resources = resources;
    }

    #region Public properties    

    public string Source
    {
      get { return _includeName; }
      set { _includeName = value; }
    }

    public ResourceDictionary Resources
    {
      get { return _resources; }
    }

    #endregion

    #region IInclude implementation

    public object Content
    {
      get { return _content; }
    }               

    #endregion

    #region IInitializable implementation

    public virtual void Initialize(IParserContext context)
    {
      XamlLoader loader = new XamlLoader();
      _content = loader.Load(_includeName);
      if (_content is UIElement)
        ((UIElement) _content).Resources.Merge(Resources);
      else if (_content is ResourceDictionary)
        ((ResourceDictionary) _content).Merge(Resources);
    }

    #endregion
  }
}
