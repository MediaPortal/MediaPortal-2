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
using MediaPortal.Presentation.Properties;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.Controls;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.General.Exceptions;
using Presentation.SkinEngine.MpfElements.Resources;
using Presentation.SkinEngine.SkinManagement;
using Presentation.SkinEngine.General;
using Presentation.SkinEngine.XamlParser.Interfaces;

namespace Presentation.SkinEngine.MarkupExtensions
{
  public enum TreeSearchMode
  {
    LogicalTree,
    VisualTree,
    Hybrid
  }

  /// <summary>
  /// Implements the MPF DynamicResource markup extension.
  /// </summary>
  /// <remarks>
  /// This class is realized as a <see cref="IBinding">Binding</see>, because it
  /// has to track changes of the source resource and of the search path to it.
  /// </remarks>
  public class DynamicResourceMarkupExtension: BindingBase
  {
    #region Protected fields

    protected IList<ResourceDictionary> _attachedResources = new List<ResourceDictionary>(); // To which resources are we attached?
    protected IList<Property> _attachedPropertiesList = new List<Property>(); // To which properties are we attached?
    protected bool _attachedToSkinResources = false; // Are we attached to skin resources?
    protected string _resourceKey = null; // Resource key to resolve
    protected Property _treeSearchModeProperty;

    #endregion

    #region Ctor

    /// <summary>
    /// Default constructor. Property <see cref="ResourceKey"/> has to be
    /// specified before this binding is bound.
    /// </summary>
    public DynamicResourceMarkupExtension()
    {
      Init();
      Attach();
    }

    /// <summary>
    /// Creates a new <see cref="DynamicResourceMarkupExtension"/> with the
    /// specified <paramref name="resourceKey"/>.
    /// </summary>
    /// <param name="resourceKey">Key of the resource this instance should resolve.</param>
    public DynamicResourceMarkupExtension(string resourceKey): this()
    {
      _resourceKey = resourceKey;
    }

    void Init()
    {
      _treeSearchModeProperty = new Property(typeof (TreeSearchMode), MarkupExtensions.TreeSearchMode.LogicalTree);
    }

    void Attach()
    {
      _treeSearchModeProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _treeSearchModeProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Attach();
      base.DeepCopy(source, copyManager);
      DynamicResourceMarkupExtension drme = source as DynamicResourceMarkupExtension;
      ResourceKey = copyManager.GetCopy(drme.ResourceKey);
      TreeSearchMode = copyManager.GetCopy(drme.TreeSearchMode);
      Detach();
    }

    #endregion

    #region Public properties

    public string ResourceKey
    {
      get { return _resourceKey; }
      set { _resourceKey = value; }
    }

    public Property TreeSearchModeProperty
    {
      get { return _treeSearchModeProperty; }
    }

    public TreeSearchMode TreeSearchMode
    {
      get { return (TreeSearchMode) _treeSearchModeProperty.GetValue(); }
      set { _treeSearchModeProperty.SetValue(value); }
    }

    #endregion

    #region Protected methods & properties

    protected void OnPropertyChanged(Property property)
    {
      if (_active)
        UpdateTarget();
    }

    protected void OnResourcesChanged(ResourceDictionary changedResources)
    {
      if (_active)
        UpdateTarget();
    }

    protected void OnSkinResourcesChanged(SkinResources newResources)
    {
      if (_active)
        UpdateTarget();
    }

    protected void OnSourcePathChanged(Property property)
    {
      if (_active)
        UpdateTarget();
    }

    protected void AttachToResources(ResourceDictionary rd)
    {
      rd.ResourcesChanged += OnResourcesChanged;
      _attachedResources.Add(rd);
    }

    protected void AttachToSkinResources()
    {
      SkinContext.SkinResourcesChanged += OnSkinResourcesChanged;
      _attachedToSkinResources = true;
    }

    protected void AttachToSourcePathProperty(Property sourcePathProperty)
    {
      if (sourcePathProperty != null)
      {
        _attachedPropertiesList.Add(sourcePathProperty);
        sourcePathProperty.Attach(OnSourcePathChanged);
      }
    }

    protected void ResetEventHandlerAttachments()
    {
      foreach (ResourceDictionary resources in _attachedResources)
        resources.ResourcesChanged -= OnResourcesChanged;
      _attachedResources.Clear();
      if (_attachedToSkinResources)
      {
        SkinContext.SkinResourcesChanged -= OnSkinResourcesChanged;
        _attachedToSkinResources = false;
      }
      foreach (Property property in _attachedPropertiesList)
        property.Detach(OnSourcePathChanged);
      _attachedPropertiesList.Clear();
    }

    protected void UpdateTarget(object value)
    {
      _targetDataDescriptor.Value = TypeConverter.Convert(value, _targetDataDescriptor.DataType);
    }

    /// <summary>
    /// Tries to resolve the resource specified by the <see cref="ResourceKey"/>
    /// property, and updates the binding target property.
    /// </summary>
    /// <remarks>
    /// This method must not be called before the
    /// <see cref="BindingBase.Prepare(IParserContext,IDataDescriptor)"/> method was called.
    /// </remarks>
    protected bool UpdateTarget()
    {
      ResetEventHandlerAttachments();
      if (KeepBinding)
      { // This instance should be used rather than the evaluated source value
        _targetDataDescriptor.Value = this;
        return true;
      }
      DependencyObject current = _contextObject as DependencyObject;
      while (current != null)
      {
        ResourceDictionary resources = null;
        if (current is UIElement)
          resources = ((UIElement) current).Resources;
        else if (current is ResourceDictionary)
          resources = (ResourceDictionary) current;
        if (resources != null)
        {
          // Attach change handler to resource dictionary
          AttachToResources(resources);
          if (resources.ContainsKey(_resourceKey))
          {
            UpdateTarget(resources[_resourceKey]);
            return true;
          }
        }
        Property parentProperty = null;
        if (TreeSearchMode == MarkupExtensions.TreeSearchMode.LogicalTree)
          parentProperty = current.LogicalParentProperty;
        else if (TreeSearchMode == MarkupExtensions.TreeSearchMode.VisualTree)
        {
          if (current is Visual)
            parentProperty = ((Visual)current).VisualParentProperty;
          else
            break;
        }
        else if (TreeSearchMode == MarkupExtensions.TreeSearchMode.Hybrid)
        {
          parentProperty = current.LogicalParentProperty;
          if (parentProperty.GetValue() == null && current is Visual)
            parentProperty = ((Visual) current).VisualParentProperty;
        }

        // Attach change handler to LogicalParent property
        AttachToSourcePathProperty(parentProperty);
        current = parentProperty.GetValue() as DependencyObject;
      }
      // Attach change handler to skin resources
      AttachToSkinResources();
      object result = SkinContext.SkinResources.FindStyleResource(_resourceKey);
      if (result != null)
      {
        UpdateTarget(result);
        return true;
      }
      return false;
    }

    #endregion

    #region IBinding implementation/overrides

    public override void Activate()
    {
      base.Activate();
      if (_resourceKey == null)
        throw new XamlBindingException("DynamicResource: property 'ResourceKey' must be given");
      UpdateTarget();
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return "{DynamicResource " + ResourceKey + "}";
    }

    #endregion
  }
}
