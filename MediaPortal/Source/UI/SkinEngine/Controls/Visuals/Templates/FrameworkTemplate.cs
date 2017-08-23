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
using System.Windows.Markup;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.MpfElements;
using INameScope = MediaPortal.UI.SkinEngine.Xaml.Interfaces.INameScope;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  public delegate void FinishBindingsDlgt();

  /// <summary>
  /// Defines a container for UI elements which are used as template controls
  /// for all types of UI-templates. Special template types
  /// like <see cref="ControlTemplate"/> or <see cref="DataTemplate"/> are derived
  /// from this class. This class basically has no other job than holding those
  /// UI elements. Methods for cloning them will be introduced by subclasses.
  /// </summary>
  /// <remarks>
  /// Templated controls such as <see cref="Button">Buttons</see> or
  /// <see cref="ListView">ListViews</see> implement several properties holding
  /// instances of <see cref="FrameworkTemplate"/>, for each templated feature.
  /// </remarks>
  [ContentProperty("TemplateElement")]
  public class FrameworkTemplate: DependencyObject, INameScope, IAddChild<FrameworkElement>, IUnmodifiableResource
  {
    #region Protected fields

    protected ResourceDictionary _resourceDictionary;
    protected FrameworkElement _templateElement;
    protected IDictionary<string, object> _names = null;
    protected object _owner = null;

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
      if (ft._names == null)
        _names = null;
      else
      {
        _names = new Dictionary<string, object>(ft._names.Count);
        foreach (KeyValuePair<string, object> kvp in ft._names)
          if (_names.ContainsKey(kvp.Key))
            continue;
          else
            _names.Add(kvp.Key, copyManager.GetCopy(kvp.Value));
      }

      _owner = copyManager.GetCopy(ft._owner);
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(_templateElement);
      MPF.TryCleanupAndDispose(_resourceDictionary);
      base.Dispose();
    }

    #endregion

    #region Protected methodws

    protected INameScope FindParentNamescope()
    {
      DependencyObject current = LogicalParent;
      while (current != null)
      {
        if (current is INameScope)
          return (INameScope) current;
        current = current.LogicalParent;
      }
      return null;
    }

    protected IDictionary<string, object> GetOrCreateNames()
    {
      return _names ?? (_names = new Dictionary<string, object>());
    }

    #endregion

    #region Public properties

    public ResourceDictionary Resources
    {
      get { return _resourceDictionary; }
    }

    public FrameworkElement TemplateElement
    {
      get { return _templateElement; }
      set { /* Only for XAML */ }
    }

    #endregion

    #region IAddChild implementation

    public void AddChild(FrameworkElement o)
    {
      _templateElement = o;
      // We need to set the template namescope to make sure when copying the template element, it will have its own
      // namescope where its names are registered
      _templateElement.TemplateNameScope = new NameScope();
    }

    #endregion

    #region INamescope implementation

    public object FindName(string name)
    {
      object obj;
      if (_names != null && _names.TryGetValue(name, out obj))
        return obj;
      INameScope parent = FindParentNamescope();
      if (parent != null)
        return parent.FindName(name);
      return null;
    }

    public void RegisterName(string name, object instance)
    {
      IDictionary<string, object> names = GetOrCreateNames();
      object old;
      if (names.TryGetValue(name, out old) && ReferenceEquals(old, instance))
        return;
      names.Add(name, instance);
    }

    public void UnregisterName(string name)
    {
      if (_names == null)
        return;
      _names.Remove(name);
    }

    #endregion

    #region IUnmodifyableResource implementation

    public object Owner
    {
      get { return _owner; }
      set { _owner = value; }
    }

    #endregion

    #region IBindingContainer implementation

    void IBindingContainer.AddBindings(IEnumerable<IBinding> bindings)
    {
      // We don't bind bindings - simply ignore them
    }

    #endregion

    #region IInitializable implementation

    public override void FinishInitialization(IParserContext context)
    {
      base.FinishInitialization(context);
      ResourceDictionary.RegisterUnmodifiableResourceDuringParsingProcess(this, context);
    }

    #endregion
  }
}
