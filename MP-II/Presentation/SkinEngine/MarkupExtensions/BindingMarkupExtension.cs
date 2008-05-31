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
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.XamlParser;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls;

namespace Presentation.SkinEngine.MarkupExtensions
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

  public enum SourceType
  {
    DataContext,
    SourceProperty,
    ElementName,
    RelativeSource
  }

  /// <summary>
  /// Implements the Binding markup extension.
  /// </summary>
  /// <remarks>
  /// This class has two main functions:
  /// <list type="bullet">
  /// <item>It can work as a data context for other bindings</item>
  /// <item>It can bind directly to a target property</item>
  /// </list>
  /// <para>
  /// In both cases, the class has to evaluate a <i>source value</i> which is specified
  /// by the binding properties and the context the binding is in, and which will
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
  public class BindingMarkupExtension: IBinding
  {
    #region Private/protected fields

    protected static IDictionary<object, ICollection<BindingMarkupExtension>> _objects2Bindings =
        new Dictionary<object, ICollection<BindingMarkupExtension>>();

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
    protected object _contextObject = null; // Bound to which object?
    protected bool _bound = false; // Did we already bind?
    protected bool _isUpdatingBinding = false; // Used to avoid recursive calls to method UpdateBinding
    protected IDataDescriptor _attachedSource = null; // To which source data are we attached?
    protected IList<Property> _attachedProperties = new List<Property>(); // To which data context properties are we attached?
    protected IDataDescriptor _targetDataDescriptor = null; // Bound to which property?

    // Derived properties
    protected PathExpression _compiledPath = null;
    protected bool _negate = false;
    protected BindingDependency bindingDependency = null;
    protected DataDescriptorRepeater _evaluatedSourceValue = new DataDescriptorRepeater();

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>Binding</i>. The new instance has to be configured before it can be
    /// used as Binding.
    /// </summary>
    public BindingMarkupExtension()
    {
      InitChangeHandlers();
    }

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>data context</i>. The new instance has to be configured before it
    /// can be used as data context.
    /// </summary>
    public BindingMarkupExtension(DependencyObject contextObject)
    {
      AttachToTargetObject(contextObject);
      InitChangeHandlers();
    }

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> for the use as
    /// <i>Binding</i>. After calling this constructor, the new instance may be
    /// further configured before used as Binding.
    /// </summary>
    /// <param name="path">Path value for this Binding.</param>
    public BindingMarkupExtension(string path)
    {
      Path = path;
      InitChangeHandlers();
    }

    /// <summary>
    /// Creates a new <see cref="BindingMarkupExtension"/> as a copy of the
    /// specified <paramref name="other"/> binding. The new binding instance
    /// will be re-targeted to the specified <paramref name="newTarget"/> object.
    /// If the <paramref name="other"/> binding was already
    /// <see cref="Bound"/>, this binding should be bound too by a call to
    /// <see cref="Bind()"/> or <see cref="UpdateBinding()"/>.
    /// </summary>
    /// <param name="other">Other Binding to copy.</param>
    /// <param name="newTarget">New target object for this Binding.</param>
    public BindingMarkupExtension(BindingMarkupExtension other, object newTarget)
    {
      Source = other.Source;
      ElementName = other.ElementName;
      RelativeSource = other.RelativeSource;
      Path = other.Path;
      Mode = other.Mode;
      UpdateSourceTrigger = other.UpdateSourceTrigger;
      CheckTypeOfSource();
      InitChangeHandlers();

      // Copy values initialized by the Prepare(IParserContext,IDataDescriptor) call,
      // retargeted to the newTarget.
      _targetDataDescriptor = other._targetDataDescriptor == null ? null :
          other._targetDataDescriptor.Retarget(newTarget);

      _compiledPath = other._compiledPath;
      _negate = other._negate;
      AttachToTargetObject(newTarget);
    }

    /// <summary>
    /// Given a <paramref name="sourceObject"/>, which has possibly attached
    /// bindings targeted to it, this method will copy those bindings
    /// retargeted at the specified <paramref name="targetObject"/>.
    /// </summary>
    /// <remarks>
    /// A typical usage of this method would be the cloning of a gui object,
    /// where the clone should behave exactly as the original object.
    /// </remarks>
    /// <param name="sourceObject">Object, whose bindings (which are targeted
    /// at it) will be copied.</param>
    /// <param name="targetObject">Object, to which the copied bindings will
    /// be retargeted.</param>
    public static void CopyBindings(object sourceObject, object targetObject)
    {
      if (_objects2Bindings.ContainsKey(sourceObject))
      {
        ICollection<BindingMarkupExtension> bindings = _objects2Bindings[sourceObject];
        foreach (BindingMarkupExtension binding in bindings)
        {
          BindingMarkupExtension newBinding = binding.CloneAndRetarget(targetObject);
          if (binding.Bound)
            newBinding.UpdateBinding();
        }
      }
    }

    /// <summary>
    /// Will clone this binding and retarget it to the specified new target object.
    /// </summary>
    /// <param name="newTarget">Target object the new binding should bind to.</param>
    /// <returns>New, retargeted binding instance. The new binding instance has the same
    /// function to the new target object as this binding has on the associated
    /// <see cref="_contextObject"/>.</returns>
    protected virtual BindingMarkupExtension CloneAndRetarget(object newTarget)
    {
      return new BindingMarkupExtension(this, newTarget);
    }

    protected void InitChangeHandlers()
    {
      _sourceProperty.Attach(OnBindingPropertiesChange);
      _elementNameProperty.Attach(OnBindingPropertiesChange);
      _relativeSourceProperty.Attach(OnBindingPropertiesChange);
      _pathProperty.Attach(OnBindingPropertiesChange);
      _modeProperty.Attach(OnBindingPropertiesChange);
      _updateSourceTriggerProperty.Attach(OnBindingPropertiesChange);

      _evaluatedSourceValue.Attach(OnSourceValueChange);
    }

    #endregion

    #region Properties

    public Property SourceProperty
    {
      get { return _sourceProperty; }
    }

    public object Source
    {
      get { return SourceProperty.GetValue(); }
      set { SourceProperty.SetValue(value); }
    }

    public Property @RelativeSourceProperty
    {
      get { return _relativeSourceProperty; }
    }

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

    public string ElementName
    {
      get { return (string) ElementNameProperty.GetValue(); }
      set { ElementNameProperty.SetValue(value); }
    }

    public Property PathProperty
    {
      get { return _pathProperty; }
    }

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

    /// <summary>
    /// Returns the information if this binding already got call to its
    /// <see cref="Bind()"/> method.
    /// </summary>
    public bool Bound
    {
      get { return _bound; }
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Called when some of our binding properties changed.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="property">The binding property which changed.</param>
    protected void OnBindingPropertiesChange(Property property)
    {
      CheckTypeOfSource();
      UpdateSourceValue();
    }

    /// <summary>
    /// Called when our binding source changed.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="dd">The source data descriptor which changed.</param>
    protected void OnBindingSourceChange(IDataDescriptor dd)
    {
      UpdateSourceValue();
    }

    /// <summary>
    /// Called when the data context changed where we bound to.
    /// Will trigger an update of our source value here.
    /// </summary>
    /// <param name="property">The data context property which changed its value.</param>
    protected void OnDataContextChange(Property property)
    {
      UpdateSourceValue();
    }

    /// <summary>
    /// Called after a new source value was evaluated for this binding.
    /// We will update our binding here, if necessary.
    /// </summary>
    /// <param name="sourceValue">Our <see cref="_evaluatedSourceValue"/> data descriptor.</param>
    protected void OnSourceValueChange(IDataDescriptor sourceValue)
    {
      if (_bound)
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
      get { return _targetDataDescriptor == null || _targetDataDescriptor.DataType == typeof(BindingMarkupExtension); }
    }

    protected void AttachToTargetObject(object obj)
    {
      // We could check here if obj is a DependencyObject and throw an Exception.
      // But by now, we will permit an arbitrary object.
      _contextObject = obj;
      ICollection<BindingMarkupExtension> bindingsOfObject;
      if (_objects2Bindings.ContainsKey(_contextObject))
        bindingsOfObject = _objects2Bindings[_contextObject];
      else
        _objects2Bindings[_contextObject] = bindingsOfObject = new List<BindingMarkupExtension>();
      bindingsOfObject.Add(this);
    }

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

    protected void AttachToSource(IDataDescriptor source)
    {
      if (source != null && source.SupportsChangeNotification)
      {
        _attachedSource = source;
        _attachedSource.Attach(OnBindingSourceChange);
      }
    }

    protected void AttachToSourcePathProperty(Property sourcePathProperty)
    {
      if (sourcePathProperty != null)
      {
        _attachedProperties.Add(sourcePathProperty);
        sourcePathProperty.Attach(OnDataContextChange);
      }
    }

    protected void ResetEventHandlerAttachments()
    {
      foreach (Property property in _attachedProperties)
        property.Detach(OnDataContextChange);
      _attachedProperties.Clear();
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
      if (!(obj is DependencyObject))
      {
        dataContext = null;
        return false;
      }
      DependencyObject current = (DependencyObject) obj;
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
    /// This method attaches change handlers to all relevant properties on the searched path.
    /// </remarks>
    /// <param name="obj">The object to get the parent of.</param>
    /// <param name="parent">The parent to walk up.</param>
    /// <returns><c>true</c>, if a valid parent was found. In this case, the
    /// <paramref name="parent"/> parameter references a not-null parent.
    /// <c>false</c>, if no valid parent was found.</returns>
    protected bool FindParent(DependencyObject obj, out DependencyObject parent)
    {
      Visual v = obj as Visual;
      Property parentProperty;
      if (v != null)
      { // The visual tree has priority to search our parent
        parentProperty = v.VisualParentProperty;
        AttachToSourcePathProperty(parentProperty);
      }
      else
      {
        // If no visual parent exists, use the logical tree
        parentProperty = obj.LogicalParentProperty;
        AttachToSourcePathProperty(parentProperty);
      }
      parent = parentProperty.GetValue() as DependencyObject;
      return parent != null;
    }

    /// <summary>
    /// Returns an <see cref="IDataDescriptor"/> for the binding in the nearest filled
    /// data context of a parent element in the visual or logical tree.
    /// </summary>
    /// <param name="result">Returns the data descriptor for the data context, if it
    /// could resolved.</param>
    /// <returns><c>true</c>, if the data context could be resolved or no data context
    /// is available, <c>false</c> if it could not be resolved (yet).
    /// In case the return value is <c>true</c> and <c><paramref name="result"/> == null</c>,
    /// no data context is available.</returns>
    protected bool FindDataContext(out IDataDescriptor result)
    {
      result = null;

      DependencyObject current = _contextObject as DependencyObject;
      if (current == null)
        return false;
      if (UsedAsDataContext)
      {
        // If we are already the data context, step one level up and start the search at our parent
        if (!FindParent(current, out current))
          return false;
      }
      while (current != null)
      {
        BindingMarkupExtension parentBinding;
        if (GetDataContext(current, out parentBinding))
        { // Data context found
          bool res = parentBinding.Evaluate(out result);
          AttachToSource(parentBinding.EvaluatedSourceValue);
          return res;
        }
        if (!FindParent(current, out current))
          return false;
      }
      return false;
    }

    /// <summary>
    /// Does the lookup for our binding source data. This includes evaluation of our source
    /// properties, the lookup for the data context and the search up the visual or logical tree.
    /// </summary>
    /// <param name="result">Resulting source descriptor, if it could be resolved.</param>
    /// <returns></returns>
    protected bool GetSourceDataDescriptor(out IDataDescriptor result)
    {
      ResetEventHandlerAttachments();
      result = null;
      if (_typeOfSource == SourceType.SourceProperty)
        result = new DependencyPropertyDataDescriptor(this, "Source", _sourceProperty);
      else if (_typeOfSource == SourceType.RelativeSource)
      {
        if (!(_contextObject is DependencyObject))
          return false;
        DependencyObject current = (DependencyObject) _contextObject;
        switch (RelativeSource.Mode)
        {
          case RelativeSourceMode.Self:
            result = new ValueDataDescriptor(current);
            return true;
          case RelativeSourceMode.TemplatedParent:
            while (current != null)
            {
              DependencyObject last = current;
              FindParent(last, out current);
              if (last is UIElement && ((UIElement) last).IsTemplateRoot)
              {
                result = new ValueDataDescriptor(current);
                return true;
              }
            }
            return false;
          case RelativeSourceMode.FindAncestor:
            if (current == null || !FindParent(current, out current)) // Start from the first ancestor
              return false;
            int ct = RelativeSource.AncestorLevel;
            while (current != null)
            {
              if (RelativeSource.AncestorType == null || RelativeSource.AncestorType.IsAssignableFrom(current.GetType()))
                ct -= 1;
              if (ct == 0)
              {
                result = new ValueDataDescriptor(current);
                return true;
              }
              if (!FindParent(current, out current))
                return false;
            }
            return false;
          //case RelativeSourceMode.PreviousData:
          //  // TODO: implement this
          //  throw new NotImplementedException(RelativeSourceMode.PreviousData.ToString());
          default:
            // Should never occur. If so, we have forgotten to handle a RelativeSourceMode
            throw new NotImplementedException();
        }
      }
      else if (_typeOfSource == SourceType.ElementName)
      {
        if (!(_contextObject is UIElement))
          return false;
        object obj = ((UIElement) _contextObject).FindElement(ElementName);
        if (obj == null)
          return false;
        result = new ValueDataDescriptor(obj);
      }
      else if (_typeOfSource == SourceType.DataContext)
      {
        if (!FindDataContext(out result))
          return false;
      }
      else
        // Should never occur. If so, we have forgotten to handle a SourceType
        throw new NotImplementedException();
      if (result != null)
        AttachToSource(result);
      return true;
    }

    /// <summary>
    /// Will be called to evaluate our source value based on all available
    /// property and context states.
    /// This method will also be called after any object involved in the
    /// evaluation process of our source value was changed.
    /// </summary>
    /// <returns><c>true</c>, if the source value based on all input data
    /// could be evaluated, else <c>false</c>.</returns>
    protected virtual bool UpdateSourceValue()
    {
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
      return true;
    }

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
    protected bool Evaluate(out IDataDescriptor result)
    {
      result = null;
      if (!UpdateSourceValue())
        return false;
      result = _evaluatedSourceValue;
      return true;
    }

    protected virtual bool UpdateBinding()
    {
      _bound = false;
      // Avoid recursive calls: For instance, this can occur when
      // the later call to Evaluate will change our evaluated source value, which
      // will cause a recursive call to UpdateBinding.
      if (_isUpdatingBinding)
        return false;
      _isUpdatingBinding = true;
      try
      {
        if (UsedAsDataContext) // This is the case only for the DataContext property
        { // We are a DataContext rather than a real binding
          _targetDataDescriptor.Value = this;
          return true;
        }
        IDataDescriptor sourceDd;
        if (!Evaluate(out sourceDd))
          return false;

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
          ResetEventHandlerAttachments();
          _targetDataDescriptor.Value = sourceDd.Value;
          return true; // In this case, we have finished with only assigning the value
        }
        if (bindingDependency != null)
          bindingDependency.Detach();
        bindingDependency = new BindingDependency(sourceDd, _targetDataDescriptor,
            attachToSource,
            attachToTarget && UpdateSourceTrigger == UpdateSourceTrigger.PropertyChanged,
            _negate);
        if (attachToTarget && UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
        {
          //FIXME: attach to LostFocus event of the next visual in context stack, create
          //change handler and call bd.UpdateSource() in the handler notification method
          throw new NotImplementedException();
        }
        return true;
      }
      finally
      {
        _isUpdatingBinding = false;
        _bound = true;
      }
    }

    #endregion

    public virtual void Dispose()
    {
      ResetEventHandlerAttachments();
    }

    #region IBinding implementation

    public virtual void Prepare(IParserContext context, IDataDescriptor dd)
    {
      AttachToTargetObject(context.ContextStack.CurrentElementContext.Instance);
      _targetDataDescriptor = dd;
      string path = Path ?? "";
      _negate = path.StartsWith("!");
      if (_negate)
        path = path.Substring(1);
      _compiledPath = string.IsNullOrEmpty(path) ? null : PathExpression.Compile(context, path);
    }

    public virtual bool Bind()
    {
      return UpdateBinding();
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
      string[] sl = new string[l.Count];
      l.CopyTo(sl, 0);
      return "{"+BindingTypeName + " " + string.Join(",", sl)+"}";
    }

    #endregion
  }
}
