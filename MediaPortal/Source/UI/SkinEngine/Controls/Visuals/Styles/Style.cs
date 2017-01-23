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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;
using INameScope = MediaPortal.UI.SkinEngine.Xaml.Interfaces.INameScope;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Styles
{
  // We implement <see cref="INameScope"/> to break the namescope search and not escalate the search to our logical parent.
  // Named elements in a style must not interfere with other elements contained in our logical parent.
  [ContentProperty("Setters")]
  public class Style: DependencyObject, INameScope, IAddChild<SetterBase>, IImplicitKey, IUnmodifiableResource
  {
    #region Consts

    protected const string STYLE_TRIGGERS_ATTACHED_PROPERTY_NAME = "Style.Triggers";

    #endregion

    #region Protected fields

    protected Style _basedOn = null;
    protected SetterBaseCollection _setters = new SetterBaseCollection();
    protected AbstractProperty _targetTypeProperty;
    protected AbstractProperty _triggersProperty;
    protected ResourceDictionary _resources;
    protected object _owner = null;

    #endregion

    #region Ctor

    public Style()
    {
      Init();
    }

    void Init()
    {
      _targetTypeProperty = new SProperty(typeof(Type), null);
      _triggersProperty = new SProperty(typeof(TriggerCollection), new TriggerCollection());
      _resources = new ResourceDictionary();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Style s = (Style) source;
      BasedOn = copyManager.GetCopy(s.BasedOn);
      foreach (SetterBase setter in s._setters)
        _setters.Add(copyManager.GetCopy(setter));
      TargetType = s.TargetType;
      _resources = copyManager.GetCopy(s.Resources);

      _owner = copyManager.GetCopy(s._owner);
    }

    public override void Dispose()
    {
      foreach (SetterBase setterBase in _setters)
        setterBase.Dispose();
      foreach (TriggerBase triggerBase in Triggers)
        triggerBase.Dispose();
      MPF.TryCleanupAndDispose(_resources);
      base.Dispose();
    }

    #endregion

    public Style BasedOn
    {
      get { return _basedOn; }
      set { _basedOn = value; }
    }

    public AbstractProperty TargetTypeProperty
    {
      get { return _targetTypeProperty; }
    }

    /// <summary>
    /// Gets or sets the type of the target element this style can be applied to.
    /// </summary>
    public Type TargetType
    {
      get { return _targetTypeProperty.GetValue() as Type; }
      set { _targetTypeProperty.SetValue(value); }
    }

    public AbstractProperty TriggersProperty
    {
      get { return _triggersProperty; }
    }

    public TriggerCollection Triggers
    {
      get { return (TriggerCollection)_triggersProperty.GetValue(); }
    }

    public SetterBaseCollection Setters
    {
      get { return _setters; }
    }

    public ResourceDictionary Resources
    {
      get { return _resources; }
    }

    /// <summary>
    /// Applies this <see cref="Style"/> to the specified <paramref name="element"/>.
    /// </summary>
    /// <param name="element">The element to apply this <see cref="Style"/> to.</param>
    public void Set(UIElement element)
    {
      MergeResources(element);
      ICollection<TriggerBase> triggers = new List<TriggerBase>();
      UpdateSettersAndCollectTriggers(element, new HashSet<string>(), triggers);
      SetTriggers(element, triggers);
    }

    public void Reset(UIElement element)
    {
      ResetTriggers(element);
      ResetSetters(element, new HashSet<string>());
      ResetResources(element);
    }

    protected void MergeResources(UIElement element)
    {
      if (_basedOn != null)
        _basedOn.MergeResources(element);
      // Merge resources with those from the target element
      element.Resources.Merge(Resources);
    }

    protected void ResetResources(UIElement element)
    {
      // Remove resources from the target element
      element.Resources.RemoveResources(Resources);
      if (_basedOn != null)
        _basedOn.ResetResources(element);
    }

    /// <summary>
    /// Worker method to apply all setters on the specified <paramref name="element"/> which
    /// have not been set yet. The set of properties already assigned will be given in parameter
    /// <paramref name="finishedProperties"/>; all properties whose names are stored in this
    /// parameter will be skipped.
    /// </summary>
    /// <param name="element">The UI element this style will be applied on.</param>
    /// <param name="finishedProperties">Set of property names which should be skipped.</param>
    /// <param name="triggers">Returns a collection of triggers which should be added to the <paramref name="element"/>.</param>
    protected void UpdateSettersAndCollectTriggers(UIElement element,
        ICollection<string> finishedProperties, ICollection<TriggerBase> triggers)
    {
      foreach (SetterBase sb in _setters)
      {
        string propertyKey = sb.UnambiguousPropertyName;
        if (finishedProperties.Contains(propertyKey))
          continue;
        finishedProperties.Add(propertyKey);
        sb.Set(element);
      }
      if (_basedOn != null)
        _basedOn.UpdateSettersAndCollectTriggers(element, finishedProperties, triggers);
      foreach (TriggerBase trigger in Triggers)
        triggers.Add(MpfCopyManager.DeepCopySetLVPs(trigger, element, null));
    }

    protected void ResetSetters(UIElement element, ICollection<string> finishedProperties)
    {
      foreach (SetterBase sb in _setters)
      {
        string propertyKey = sb.UnambiguousPropertyName;
        if (finishedProperties.Contains(propertyKey))
          continue;
        finishedProperties.Add(propertyKey);
        sb.Restore(element);
      }
      if (_basedOn != null)
        _basedOn.ResetSetters(element, finishedProperties);
    }

    protected void SetTriggers(UIElement element, ICollection<TriggerBase> triggers)
    {
      element.UninitializeTriggers();
      element.SetAttachedPropertyValue(STYLE_TRIGGERS_ATTACHED_PROPERTY_NAME, triggers);
      CollectionUtils.AddAll(element.Triggers, triggers);
    }

    protected void ResetTriggers(UIElement element)
    {
      element.UninitializeTriggers();
      ICollection<TriggerBase> triggers = element.GetAttachedPropertyValue<ICollection<TriggerBase>>(STYLE_TRIGGERS_ATTACHED_PROPERTY_NAME, null);
      if (triggers != null)
      {
        foreach (TriggerBase trigger in triggers)
          if (element.Triggers.Remove(trigger))
            MPF.TryCleanupAndDispose(trigger);
        element.SetAttachedPropertyValue<ICollection<TriggerBase>>(STYLE_TRIGGERS_ATTACHED_PROPERTY_NAME, null);
      }
    }

    #region INamescope implementation

    public object FindName(string name)
    {
      return null;
    }

    public void RegisterName(string name, object instance) { }

    public void UnregisterName(string name) { }

    #endregion

    #region IAddChild implementation

    public void AddChild(SetterBase sb)
    {
      _setters.Add(sb);
    }

    #endregion

    #region IImplicitKey implementation

    public object GetImplicitKey()
    {
      return TargetType;
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
