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

using System.Collections.Generic;
using MediaPortal.SkinEngine.Controls;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.SkinManagement;
using System.IO;

namespace MediaPortal.SkinEngine.MpfElements.Resources
{
  public class Include : NameScope, IInclude, IInitializable
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

    public object Content
    {
      get { return _content; }
    }               

    #endregion

    #region IInitializable implementation

    public virtual void Initialize(IParserContext context)
    {
      string includeFilePath = SkinContext.SkinResources.GetResourceFilePath(_includeName);
      if (includeFilePath == null)
        throw new XamlLoadException("Could not open include file '{0}'", includeFilePath);
      _content = context.LoadXaml(includeFilePath);
      if (_content is UIElement)
      {
        UIElement target = (UIElement) _content;
        // Merge resources with those from the included content
        MergeResourceDictionaries(Resources, target.Resources);
        // Merge namescope into the included content
        INameScope targetNs = target.FindNameScope();
        if (targetNs != null)
          foreach (KeyValuePair<string, object> kvp in _names)
            targetNs.RegisterName(kvp.Key, kvp.Value);
      }
      else if (_content is ResourceDictionary)
        MergeResourceDictionaries(Resources, (ResourceDictionary) _content);
    }

    public void MergeResourceDictionaries(ResourceDictionary source, ResourceDictionary target)
    {
      IEnumerator<KeyValuePair<string, object>> enumer = ((IDictionary<string, object>) source).GetEnumerator();
      IDictionary<string, object> targetDictionary = target.UnderlayingDictionary;
      while (enumer.MoveNext())
      {
        object entry = enumer.Current.Value;
        targetDictionary[enumer.Current.Key] = entry;
        if (entry is DependencyObject)
          ((DependencyObject) entry).LogicalParent = target;
      }
      target.FireChanged();
    }

    #endregion
  }
}
