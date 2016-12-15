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

using System;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  public class Include : NameScope, IInclude, IInitializable, IDisposable
  {
    #region Protected fields

    protected object _content = null;
    protected string _includeName = null;
    protected ResourceDictionary _resources;

    #endregion

    public Include()
    {
      Init();
    }

    public void Dispose()
    {
      MPF.TryCleanupAndDispose(_content);
      MPF.TryCleanupAndDispose(_resources);
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

    /// <summary>
    /// Gets or sets the source file to be included by this instance.
    /// The value has to be a relative resource path, which will be searched as a resource
    /// in the current skin context (See <see cref="SkinContext.SkinResources"/>).
    /// </summary>
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

    public object PassContent()
    {
      object result = _content;
      _content = null;
      return result;
    }               

    #endregion

    #region IInitializable implementation

    public void StartInitialization(IParserContext context)
    {}

    public void FinishInitialization(IParserContext context)
    {
      ISkinResourceBundle resourceBundle;
      string includeFilePath = SkinContext.SkinResources.GetResourceFilePath(_includeName, true, out resourceBundle);
      if (includeFilePath == null)
        throw new XamlLoadException("Include: Could not open include file '{0}' (evaluated path is '{1}')", _includeName, includeFilePath);
      _content = XamlLoader.Load(includeFilePath, resourceBundle, (IModelLoader) context.GetContextVariable(typeof(IModelLoader)));
      if (_content is UIElement)
      {
        UIElement target = (UIElement) _content;
        // Merge resources with those from the included content
        target.Resources.TakeOver(_resources, true, true);
        _resources = null;
      }
      else if (_content is ResourceDictionary)
      {
        ((ResourceDictionary) _content).TakeOver(_resources, true, true);
        _resources = null;
      }
    }

    #endregion
  }
}
