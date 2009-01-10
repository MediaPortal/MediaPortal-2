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

using System;
using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.Xaml;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.Controls;

namespace MediaPortal.SkinEngine.MarkupExtensions
{

  public enum BindingMode
  {
    OneWay,
    TwoWay,
    OneWayToSource,
    OneTime,
    Default
  }

  public enum UpdateSourceTrigger
  {
    PropertyChanged,
    LostFocus,
    Explicit
  }

  /// <summary>
  /// Implements the MPF Binding markup extension.
  /// </summary>
  /// <remarks>
  /// This class has two main functions:
  /// <list type="bullet">
  /// <item>It can work as a data context for other bindings</item>
  /// <item>It can bind directly to a target property</item>
  /// </list>
  /// <para>
  /// In both cases, the class has to evaluate a <i>source value</i> which is specified
  /// by the binding properties and the binding's context, and which will
  /// be used by subordinated bindings, which use this binding as data context, or as
  /// source value for the target property bound with this binding. Be careful
  /// with the term <i>source value</i> (or <i>evaluated source value</i>). There
  /// are three terms which sound similar but have a different meaning:
  /// The term <i>binding source property</i> refers diretly to the property
  /// <see cref="BindingMarkupExtension.Source"/>.
  /// The term <i>binding source</i> refers to the object which is derived from
  /// the binding's properties and context; depending on the values of
  /// the <see cref="BindingMarkupExtension.Source"/>,
  /// <see cref="BindingMarkupExtension.RelativeSource"/> and
  /// <see cref="BindingMarkupExtension.ElementName"/> properties and the next
  /// available parent, the binding source is the value the
  /// <see cref="BindingMarkupExtension.Path"/> will be based on.
  /// The <i>(evaluated) source value</i> is the value which is computed
  /// by applying the specified <see cref="BindingMarkupExtension.Path"/> to
  /// the binding source value.
  /// </para>
  /// <para>
  /// When used to bind to a target property, this class will create a
  /// <see cref="BindingDependency"/> to handle updates between the two
  /// referenced properties/values, if all required parameters are specified.
  /// The update strategy depends on the settings of the properties
  /// <see cref="BindingMarkupExtension.Mode"/> and
  /// <see cref="BindingMarkupExtension.UpdateSourceTrigger"/>.
  /// </para>
  /// </remarks>
  public class BindingMarkupExtension: BindingBase
  {
    #region Enums

    protected enum SourceType
    {
      DataContext,
      SourceProperty,
      ElementName,
      RelativeSource
    }

    protected enum FindParentMode
    {
      LogicalTree,
      HybridPreferVisualTree
    }

    #endregion

    #region Protected fields

    // Binding configuration properties
    protected SourceType _typeOfSource = SourceType.DataContext;
    protected Property _sourceProperty = new Property(typeof(object), null);
    protected Property _elementNameProperty = new Property(typeof(string), null);
    protected Property _relativeSourceProperty = new Property(typeof(RelativeSource), null);
    protected Property _pathProperty = new Property(typeof(string), null);
    protected Property _modeProperty = new Property(typeof(BindingMode), BindingMode.Default);
    protected Property _updateSourceTriggerProperty =
        new Property(typeof(UpdateSourceTrigger), UpdateSourceTrigger.PropertyChanged);

    // State variables
    protected bool _retryBinding = false; // Our BindingDependency could not be established because there were problems evaluating the binding source value -> UpdateBinding has to be called again
    protected bool _sourceValueValid = false; // Cache-valid flag to avoid unnecessary calls to UpdateSourceValue()
    protected bool _isUpdatingBinding = false; // Used to avoid recursive calls to method UpdateBinding
    protected IDataDescriptor _attachedSource = null; // To which source data are we attached?
    protected IList<Property> _attachedPropertiesList = new List<Property>(); // To which data contexts and other properties are we attached?

    // Derived properties
    protected PathExpression _compiledPath = null;
    protected bool _negate = false;
    protected BindingDependency _bindingDependency = null;
    protected DataDescriptorRepeater _evaluatedSourceValue = new DataDescriptorRepeater();

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>Binding</i>.
    /// </summary>
    public BindingMarkupExtension()
    {
      Attach();
    }

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>data context</i>.
    /// </summary>
    public BindingMarkupExtension(DependencyObject contextObject):
        base(contextObject)
    {
      Attach();
    }

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>Binding</i>. Works like <see cref="BindingMarkupExtension()"/> with additionally
    /// setting the <see cref="Path"/> property.
    /// </summary>
    /// <param name="path">Path value for this Binding.</param>
    public BindingMarkupExtension(string path)
    {
      Path = path;
      Attach();
    }

    protected void Attach()
    {
      _sourceProperty.Attach(OnBindingPropertyChanged);
      _elementNameProperty.Attach(OnBindingPropertyChanged);
      _relativeSourceProperty.Attach(OnBindingPropertyChanged);
      _pathProperty.Attach(OnBindingPropertyChanged);
      _modeProperty.Attach(OnBindingPropertyChanged);
      _updateSourceTriggerProperty.Attach(OnBindingPropertyChanged);

      _evaluatedSourceValue.Attach(OnSourceValueChanged);
    }

    protected void Detach()
    {
      _sourceProperty.Detach(OnBindingPropertyChanged);
      _elementNameProperty.Detach(OnBindingPropertyChanged);
      _relativeSourceProperty.Detach(OnBindingPropertyChanged);
      _pathProperty.Detach(OnBindingPropertyChanged);
      _modeProperty.Detach(OnBindingPropertyChanged);
      _updateSourceTriggerProperty.Detach(OnBindingPropertyChanged);

      _evaluatedSourceValue.Detach(OnSourceValueChanged);
    }

    public override void Dispose()
    {
      ResetChangeHandlerAttachments();
      base.Dispose();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      BindingMarkupExtension bme = (BindingMarkupExtension) source;
      Source = copyManager.GetCopy(bme.Source);
      ElementName = copyManager.GetCopy(bme.ElementName);
      RelativeSource = copyManager.GetCopy(bme.RelativeSource);
      CheckTypeOfSource();
      Path = copyManager.GetCopy(bme.Path);
      Mode = copyManager.GetCopy(bme.Mode);
      UpdateSourceTrigger = copyManager.GetCopy(bme.UpdateSourceTrigger);

      _compiledPath = bme._compiledPath;
      _negate = bme._negate;
      Attach();
    }

    #endregion

    /// <summary>
    /// Evaluates an <see cref="IDataDescriptor"/> instance which is our
    /// evaluated source value (or value object). This data descriptor
    /// will be the source endpoint for the binding operation, if any.
    /// If this binding is used as a parent binding in a superior data context,
    /// the returned data descriptor is the starting point for subordinated bindings.
    /// If this binding is used to update a target property, the returned data descriptor
    /// is used as value for the assignment to the target property.
    /// </summary>
    /// <param name="result">Returns the data descriptor for the binding's source value.
    /// This value is only valid if this method returns <c>true</c>.</param>
    /// <returns><c>true</c>, if the source value could be resolved,
    /// <c>false</c> if it could not be resolved (yet).</returns>
    public bool Evaluate(out IDataDescriptor result)
    {
      result = null;
      try
      {
        if (!_sourceValueValid && !UpdateSourceValue())
          return false;
        result = _evaluatedSourceValue;
        return true;
      } catch
      {
        return false;
      }
    }

    #region Properties

    public Property SourceProperty
    {
      get { return _sourceProperty; }
    }

    /// <summary>
    /// Specifies an object to be used as binding source object.
    /// Only one of the properties <see cref="Source"/>, <see cref="RelativeSource"/>
    /// and <see cref="ElementName"/> can be set on this instance.
    /// </summary>
    public object Source
    {
      get { return SourceProperty.GetValue(); }
      set { SourceProperty.SetValue(value); }
    }

    public Property @RelativeSourceProperty
    {
      get { return _relativeSourceProperty; }
    }

    /// <summary>
    /// Specifies a binding source object relative to our
    /// <see cref="BindingBase._contextObject">context object</see>. The context object is
    /// the target object this binding is applied to.
    /// Only one of the properties <see cref="Source"/>, <see cref="RelativeSource"/>
    /// and <see cref="ElementName"/> can be set on this instance.
    /// </summary>
    public RelativeSource @RelativeSource
    {
      get { return (RelativeSource) RelativeSourceProperty.GetValue(); }
      set { RelativeSourceProperty.SetValue(value); }
    }

//    public string XPath // TODO: Not implemented yet
//    { }

    public Property ElementNameProperty
    {
      get { return _elementNameProperty; }
    }

    /// <summary>
    /// Specifies an object with the given name to be used as binding source object.
    /// Only one of the properties <see cref="Source"/>, <see cref="RelativeSource"/>
    /// and <see cref="ElementName"/> can be set on this instance.
    /// </summary>
    public string ElementName
    {
      get { return (string) ElementNameProperty.GetValue(); }
      set { ElementNameProperty.SetValue(value); }
    }

    public Property PathProperty
    {
      get { return _pathProperty; }
    }

    /// <summary>
    /// Specifies a property evaluation path starting at the binding source
    /// object. The path syntax is the syntax used and defined by the class
    /// <see cref="PathExpression"/>.
    /// </summary>
    public string Path
    {
      get { return (string) PathProperty.GetValue(); }
      set
      {
        if (_compiledPath != null)
          throw new XamlBindingException("The path of a Binding which was already prepared cannot be changed");
        PathProperty.SetValue(value);
      }
    }

    public Property ModeProperty
    {
      get { return _modeProperty; }
    }

    public BindingMode Mode
    {
      get { return (BindingMode) ModeProperty.GetValue(); }
      set { ModeProperty.SetValue(value); }
    }

    public Property UpdateSourceTriggerProperty
    {
      get { return _updateSourceTriggerProperty; }
    }

    public UpdateSourceTrigger UpdateSourceTrigger
    {
      get { return (UpdateSourceTrigger) UpdateSourceTriggerProperty.GetValue(); }
      set { UpdateSourceTriggerProperty.SetValue(value); }
    }

    /// <summary>
    /// Holds the evaluated source value for this binding. Clients may attach
    /// change handlers to the returned data descriptor; if the evaluated
    /// source value changes, this data descriptor will remain the same,
    /// only the value will change.
    /// </summary>
    public IDataDescriptor EvaluatedSourceValue
    {
      get { return _evaluatedSourceValue; }
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Called when some of our binding properties changed.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="property">The binding property which changed.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnBindingPropertyChanged(Property property, object oldValue)
    {
      CheckTypeOfSource();
      if (_active)
        UpdateSourceValue();
    }

    /// <summary>
    /// Called when our binding source changed.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="dd">The source data descriptor which changed.</param>
    protected void OnBindingSourceChange(IDataDescriptor dd)
    {
      if (_active)
        UpdateSourceValue();
    }

    /// <summary>
    /// Called when the data context changed where we bound to.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="property">The data context property which changed its value.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnDataContextChanged(Property property, object oldValue)
    {
      if (_active)
        UpdateSourceValue();
    }

    /// <summary>
    /// Called after a new source value was evaluated for this binding.
    /// We will update our binding here, if necessary.
    /// </summary>
    /// <param name="sourceValue">Our <see cref="_evaluatedSourceValue"/> data descriptor.</param>
    protected void OnSourceValueChanged(IDataDescriptor sourceValue)
    {
      if (_active && _retryBinding)
        UpdateBinding();
    }

    #endregion

    #region Protected properties and methods

    /// <summary>
    /// Returns the XAML name of this binding.
    /// This is for debugging purposes only - ToString() method.
    /// </summary>
    protected virtual string BindingTypeName
    {
      get { return "Binding"; }
    }

    protected bool UsedAsDataContext
    {
      // This code is redundant to BindingBase.KeepBinding. This is by design - the two functions are mapped
      // coincidentally on the same expression.
      get { return _targetDataDescriptor == null || typeof(IBinding).IsAssignableFrom(_targetDataDescriptor.DataType); }
    }

    /// <summary>
    /// Will check if the property settings affecting the binding source
    /// aren't conflicting.
    /// </summary>
    /// <exception name="XamlBindingException">If the binding's properties
    /// affecting the binding source (<see cref="Source"/>, <see cref="RelativeSource"/>
    /// and <see cref="ElementName"/>) are conflicting.</exception>
    protected void CheckTypeOfSource()
    {
      int sourcePropertiesSet = 0;
      _typeOfSource = SourceType.DataContext;
      if (Source != null)
      {
        sourcePropertiesSet++;
        _typeOfSource = SourceType.SourceProperty;
      }
      if (RelativeSource != null)
      {
        sourcePropertiesSet++;
        _typeOfSource = SourceType.RelativeSource;
      }
      if (!string.IsNullOrEmpty(ElementName))
      {
        sourcePropertiesSet++;
        _typeOfSource = SourceType.ElementName;
      }
      if (sourcePropertiesSet > 1)
        throw new XamlBindingException("Conflicting binding property configuration: More than one source property is set");
    }

    /// <summary>
    /// Attaches a change handler to the specified data descriptor
    /// <paramref name="source"/>, which will be used as binding source.
    /// </summary>
    protected void AttachToSource(IDataDescriptor source)
    {
      if (source != null && source.SupportsChangeNotification)
      {
        _attachedSource = source;
        _attachedSource.Attach(OnBindingSourceChange);
      }
    }

    /// <summary>
    /// Attaches a change handler to the specified <paramref name="sourcePathProperty"/>,
    /// which is located in the resolving path to the source value. We will attach
    /// a change handler to every property, whose change will potentially affect
    /// the object evaluated as binding source.
    /// </summary>
    protected void AttachToSourcePathProperty(Property sourcePathProperty)
    {
      if (sourcePathProperty != null)
      {
        _attachedPropertiesList.Add(sourcePathProperty);
        sourcePathProperty.Attach(OnDataContextChanged);
      }
    }

    /// <summary>
    /// Will reset all change handler attachments to source property and
    /// source path properties. This should be called before the evaluation path
    /// to the binding's source will be processed again.
    /// </summary>
    protected void ResetChangeHandlerAttachments()
    {
      foreach (Property property in _attachedPropertiesList)
        property.Detach(OnDataContextChanged);
      _attachedPropertiesList.Clear();
      if (_attachedSource != null)
      {
        _attachedSource.Detach(OnBindingSourceChange);
        _attachedSource = null;
      }
    }

    /// <summary>
    /// Returns the data context of the specified <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">The target object to search the data context.</param>
    /// <param name="dataContext">The data context on object <paramref name="obj"/> or
    /// <c>null</c>, if no data context is present on the object.</param>
    /// <returns><c>true</c>, if a data context was found. In this case, the returned
    /// <paramref name="dataContext"/> is not-null. <c>false</c> else.</returns>
    /// <remarks>
    /// This method attaches change handlers to all relevant properties on the searched path.
    /// </remarks>
    protected bool GetDataContext(object obj, out BindingMarkupExtension dataContext)
    {
      DependencyObject current = obj as DependencyObject;
      if (current == null)
      {
        dataContext = null;
        return false;
      }
      Property dataContextProperty = current.DataContextProperty;
      AttachToSourcePathProperty(dataContextProperty);
      dataContext = dataContextProperty.GetValue() as BindingMarkupExtension;
      return dataContext != null;
    }

    /// <summary>
    /// This method does the walk in the visual or logical tree, depending on the existance
    /// of the visual tree for the specified <paramref name="obj"/>.
    /// </summary>
    /// <remarks>
    /// The tree walk will use the visual tree if the specified <paramref name="obj"/>
    /// is a <see cref="Visual"/>, else it will use its logical tree.
    /// This method attaches change handlers to all relevant properties on the searched path.
    /// </remarks>
    /// <param name="obj">The object to get the parent of.</param>
    /// <param name="parent">The parent which was found navigating the visual or
    /// logical tree.</param>
    /// <param name="findParentMode">Specifies, which tree will be used to find the parent
    /// object.</param>
    /// <returns><c>true</c>, if a valid parent was found. In this case, the
    /// <paramref name="parent"/> parameter references a not-<c>null</c> parent.
    /// <c>false</c>, if no valid parent was found.</returns>
    protected bool FindParent(DependencyObject obj, out DependencyObject parent,
        FindParentMode findParentMode)
    {
      Visual v = obj as Visual;
      Property parentProperty = null;
      parent = null;
      if (findParentMode == FindParentMode.HybridPreferVisualTree && v != null)
      { // The visual tree has priority to search our parent
        parentProperty = v.VisualParentProperty;
        AttachToSourcePathProperty(parentProperty);
        parent = parentProperty.GetValue() as DependencyObject;
      }
      if (parent == null)
      { // If no visual parent exists, use the logical tree
        parentProperty = obj.LogicalParentProperty;
        AttachToSourcePathProperty(parentProperty);
      }
      parent = parentProperty.GetValue() as DependencyObject;
      return parent != null;
    }

    /// <summary>
    /// Returns an <see cref="IDataDescriptor"/> for the binding in the nearest available
    /// data context of a parent element in the visual or logical tree.
    /// </summary>
    /// <param name="result">Returns the data descriptor for the data context, if it
    /// could be resolved.</param>
    /// <returns><c>true</c>, if a data context could be found and the data context
    /// could evaluate, <c>false</c> if it could not be resolved (yet).</returns>
    protected bool FindDataContext(out IDataDescriptor result)
    {
      result = null;
      DependencyObject current = _contextObject;
      if (current == null)
        return false;
      if (UsedAsDataContext)
      {
        // If we are already the data context, step one level up and start the search at our parent
        if (!FindParent(current, out current, FindParentMode.HybridPreferVisualTree))
          return false;
      }
      while (current != null)
      {
        BindingMarkupExtension parentBinding;
        if (GetDataContext(current, out parentBinding))
          // Data context found
          return parentBinding.Evaluate(out result);
        if (!FindParent(current, out current, FindParentMode.HybridPreferVisualTree))
          return false;
      }
      return false;
    }

    /// <summary>
    /// Returns the nearest element implementing <see cref="INameScope"/>, stepping up the
    /// logical tree from our context object.
    /// </summary>
    /// <param name="result">Returns the nearest name scope element, if there is one.</param>
    /// <returns><c>true</c>, if a name scope could be found, <c>false</c> if it could not
    /// be found (yet).</returns>
    protected bool FindNameScope(out INameScope result)
    {
      result = null;
      DependencyObject current = _contextObject;
      if (current == null)
        return false;
      while (current != null)
      {
        if (current is INameScope)
        {
          result = current as INameScope;
          return true;
        }
        else if (current is UIElement)
        {
          UIElement uiElement = (UIElement) current;
          Property templateNameScopeProperty = uiElement.TemplateNameScopeProperty;
          AttachToSourcePathProperty(templateNameScopeProperty);
          if ((result = ((INameScope) templateNameScopeProperty.GetValue())) != null)
            return true;
        }
        if (!FindParent(current, out current, FindParentMode.LogicalTree))
          return false;
      }
      return false;
    }

    /// <summary>
    /// Does the lookup for our binding source data. This includes evaluation of our source
    /// properties and the lookup for the data context.
    /// </summary>
    /// <remarks>
    /// During the lookup, change handlers will be attached to all relevant properties
    /// on the search path to the binding source. If one of the properties changes,
    /// this binding will re-evaluate.
    /// </remarks>
    /// <param name="result">Resulting source descriptor, if it could be resolved.</param>
    /// <returns><c>true</c>, if the binding source could be found and evaluated,
    /// <c>false</c> if it could not be resolved (yet).</returns>
    protected bool GetSourceDataDescriptor(out IDataDescriptor result)
    {
      ResetChangeHandlerAttachments();
      result = null;
      try
      {
        switch (_typeOfSource)
        {
          case SourceType.DataContext:
            return FindDataContext(out result);
          case SourceType.SourceProperty:
            result = new DependencyPropertyDataDescriptor(this, "Source", _sourceProperty);
            return true;
          case SourceType.RelativeSource:
            DependencyObject current = _contextObject;
            if (current == null)
              return false;
            switch (RelativeSource.Mode)
            {
              case RelativeSourceMode.Self:
                result = new ValueDataDescriptor(current);
                return true;
              case RelativeSourceMode.TemplatedParent:
                while (current != null)
                {
                  DependencyObject last = current;
                  FindParent(last, out current, FindParentMode.HybridPreferVisualTree);
                  if (last is UIElement && ((UIElement) last).IsTemplateControlRoot)
                  {
                    result = new ValueDataDescriptor(current);
                    return true;
                  }
                }
                return false;
              case RelativeSourceMode.FindAncestor:
                if (!FindParent(current, out current, FindParentMode.HybridPreferVisualTree)) // Start from the first ancestor
                  return false;
                int ct = RelativeSource.AncestorLevel;
                while (current != null)
                {
                  if (RelativeSource.AncestorType == null ||
                      RelativeSource.AncestorType.IsAssignableFrom(current.GetType()))
                    ct -= 1;
                  if (ct == 0)
                  {
                    result = new ValueDataDescriptor(current);
                    return true;
                  }
                  if (!FindParent(current, out current, FindParentMode.HybridPreferVisualTree))
                    return false;
                }
                return false;
                //case RelativeSourceMode.PreviousData:
                //  // TODO: implement this
                //  throw new NotImplementedException(RelativeSourceMode.PreviousData.ToString());
              default:
                // Should never occur. If so, we have forgotten to handle a RelativeSourceMode
                throw new NotImplementedException(
                    string.Format("RelativeSourceMode '{0}' is not implemented", RelativeSource.Mode));
            }
          case SourceType.ElementName:
            INameScope nameScope;
            if (!FindNameScope(out nameScope))
              return false;
            object obj = nameScope.FindName(ElementName) as UIElement;
            if (obj == null)
              return false;
            result = new ValueDataDescriptor(obj);
            return true;
          default:
            // Should never occur. If so, we have forgotten to handle a SourceType
            throw new NotImplementedException(
                string.Format("SourceType '{0}' is not implemented", _typeOfSource));
        }
      }
      finally
      {
        AttachToSource(result);
      }
    }

    /// <summary>
    /// Will be called to evaluate our source value based on all available
    /// property and context states.
    /// This method will also be automatically re-called when any object involved in the
    /// evaluation process of our source value was changed.
    /// </summary>
    /// <returns><c>true</c>, if the source value based on all input data
    /// could be evaluated, else <c>false</c>.</returns>
    protected virtual bool UpdateSourceValue()
    {
      _sourceValueValid = false;
      IDataDescriptor evaluatedValue;
      if (!GetSourceDataDescriptor(out evaluatedValue))
        // Do nothing if not all necessary properties can be resolved at the current time
        return false;
      if (_compiledPath != null)
        try
        {
          if (!_compiledPath.Evaluate(evaluatedValue, out evaluatedValue))
            return false;
        }
        catch (XamlBindingException)
        {
          return false;
        }
      // If no path is specified, evaluatedValue will be the source value
      _evaluatedSourceValue.SourceValue = evaluatedValue;
      _sourceValueValid = true;
      return true;
    }

    protected virtual bool UpdateBinding()
    {
      // Avoid recursive calls: For instance, this can occur when
      // the later call to Evaluate will change our evaluated source value, which
      // will cause a recursive call to UpdateBinding.
      if (_isUpdatingBinding)
        return false;
      _isUpdatingBinding = true;
      try
      {
        if (KeepBinding) // This is the case if our target descriptor has a binding type
        { // This instance should be used rather than the evaluated source value
          _targetDataDescriptor.Value = this;
          _retryBinding = false;
          return true;
        }
        IDataDescriptor sourceDd;
        if (!Evaluate(out sourceDd))
        {
          _retryBinding = true;
          return false;
        }

        bool attachToSource = false;
        bool attachToTarget = false;
        if (Mode == BindingMode.OneWay || Mode == BindingMode.Default)
        // Currently, we don't really support the Default binding mode in
        // MediaPortal skin engine. Maybe we will support it in future -
        // then we'll be able to initialize the mode with a default value
        // implied by our target data endpoint.
        {
          attachToSource = true;
        }
        else if (Mode == BindingMode.TwoWay)
        {
          attachToSource = true;
          attachToTarget = true;
        }
        else if (Mode == BindingMode.OneWayToSource)
        {
          attachToTarget = true;
        }
        else if (Mode == BindingMode.OneTime)
        {
          Dispose();
          _targetDataDescriptor.Value = sourceDd.Value;
          _retryBinding = false;
          return true; // In this case, we have finished with only assigning the value
        }
        if (_bindingDependency != null)
          _bindingDependency.Detach();
        DependencyObject parent = null;
        if (UpdateSourceTrigger == MarkupExtensions.UpdateSourceTrigger.LostFocus)
          FindParent(_contextObject, out parent, FindParentMode.HybridPreferVisualTree);
        _bindingDependency = new BindingDependency(sourceDd, _targetDataDescriptor, attachToSource,
            attachToTarget ? UpdateSourceTrigger : MarkupExtensions.UpdateSourceTrigger.Explicit,
            parent as UIElement, _negate);
        if (attachToTarget && UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
        {
          // FIXME: attach to LostFocus event of the next visual in the tree, create
          // change handler and call bd.UpdateSource() in the handler notification method
          throw new NotImplementedException("UpdateSourceTrigger.LostFocus is not implemented");
        }
        _retryBinding = false;
        return true;
      }
      finally
      {
        _isUpdatingBinding = false;
      }
    }

    #endregion

    #region IBinding implementation

    public override void Prepare(IParserContext context, IDataDescriptor dd)
    {
      base.Prepare(context, dd);
      string path = Path ?? "";
      _negate = path.StartsWith("!");
      if (_negate)
        path = path.Substring(1);
      _compiledPath = string.IsNullOrEmpty(path) ? null : PathExpression.Compile(context, path);
    }

    public override void Activate()
    {
      base.Activate();
      UpdateBinding();
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      IList<string> l = new List<string>();
      if (Source != null)
        l.Add("Source="+Source);
      if (RelativeSource != null)
        l.Add("RelativeSource="+RelativeSource);
      if (ElementName != null)
        l.Add("ElementName="+ElementName);
      if (!string.IsNullOrEmpty(Path))
        l.Add("Path="+Path);
      return "{"+BindingTypeName + " " + StringUtils.Join(",", l)+"}";
    }

    #endregion
  }
}
