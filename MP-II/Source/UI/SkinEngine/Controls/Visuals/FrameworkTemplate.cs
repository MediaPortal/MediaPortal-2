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
using MediaPortal.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.SkinEngine.MpfElements.Resources;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.MpfElements;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Defines a container for UI elements which are used as template controls
  /// for all types of UI-templates. Special template types
  /// like <see cref="ControlTemplate"/> or <see cref="DataTemplate"/> are derived
  /// from this class. This class basically has no other job than holding those
  /// UI elements and cloning them when the template should be applied
  /// (method <see cref="LoadContent()"/>).
  /// </summary>
  /// <remarks>
  /// Templated controls such as <see cref="Button">Buttons</see> or
  /// <see cref="ListView">ListViews</see> implement several properties holding
  /// instances of <see cref="FrameworkTemplate"/>, for each templated feature.
  /// </remarks>
  public class FrameworkTemplate: DependencyObject, INameScope, IAddChild<UIElement>
  {
    #region Protected fields

    protected ResourceDictionary _resourceDictionary;
    protected UIElement _templateElement;
    protected IDictionary<string, object> _names = new Dictionary<string, object>();

    #endregion

    #region Ctor

    public FrameworkTemplate()
    {
      Init();
    }

    void Init()
    {
      _resourceDictionary = new ResourceDictionary();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      FrameworkTemplate ft = (FrameworkTemplate) source;
      _templateElement = copyManager.GetCopy(ft._templateElement);
      _resourceDictionary = copyManager.GetCopy(ft._resourceDictionary);
      foreach (KeyValuePair<string, object> kvp in ft._names)
        if (_names.ContainsKey(kvp.Key))
          continue;
        else
          _names.Add(copyManager.GetCopy(kvp.Key), copyManager.GetCopy(kvp.Value));
    }

    #endregion

    #region Public properties

    public ResourceDictionary Resources
    {
      get { return _resourceDictionary; }
    }

    #endregion

    #region Public methods

    public UIElement LoadContent()
    {
      if (_templateElement == null)
        return null;
      MpfCopyManager cm = new MpfCopyManager();
      cm.AddIdentity(this, null);
      UIElement result = cm.GetCopy(_templateElement);
      INameScope ns = new NameScope();
      foreach (KeyValuePair<string, object> nameRegistration in _names)
        ns.RegisterName(cm.GetCopy(nameRegistration.Key), cm.GetCopy(nameRegistration.Value));
      cm.FinishCopy();
      result.TemplateNameScope = ns;
      return result;
    }

    #endregion

    #region IAddChild implementation

    public void AddChild(UIElement o)
    {
      _templateElement = o;
      _templateElement.Resources.Merge(Resources);
    }

    #endregion

    #region INamescope implementation

    public object FindName(string name)
    {
      if (_names.ContainsKey(name))
        return _names[name];
      INameScope parent = FindParentNamescope();
      if (parent != null)
        return parent.FindName(name);
      return null;
    }

    protected INameScope FindParentNamescope()
    {
      DependencyObject current = this;
      while (current.LogicalParent != null)
      {
        current = current.LogicalParent;
        if (current is INameScope)
          return (INameScope) current;
      }
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      _names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      _names.Remove(name);
    }

    #endregion
  }
}
