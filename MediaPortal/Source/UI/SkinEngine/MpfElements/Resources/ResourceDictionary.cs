#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.MpfElements.Resources
{
  public delegate void ResourcesChangedHandler(ResourceDictionary changedResources);

  public class ResourceDictionary: DependencyObject, IDictionary<object, object>, INameScope
  {
    public const string KEY_ACTIVATE_BINDINGS = "ActivateBindings";

    #region Protected fields

    protected static readonly ICollection<object> EMPTY_OBJECT_COLLECTION = new List<object>(0).AsReadOnly();
    protected static readonly ICollection<KeyValuePair<object, object>> EMPTY_KVP_COLLECTION = new List<KeyValuePair<object, object>>(0);

    protected string _source = string.Empty;
    protected ICollection<string> _dependsOnStyleResources = new List<string>(0);
    protected IList<ResourceDictionary> _mergedDictionaries = null;
    protected IDictionary<string, object> _names = null;
    protected IDictionary<object, object> _resources = null;
    protected WeakEventMulticastDelegate _resourcesChangedDelegate = new WeakEventMulticastDelegate();

    #endregion

    #region Ctor & maintainance

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ResourceDictionary rd = (ResourceDictionary) source;
      Source = rd.Source;
      _dependsOnStyleResources = new List<string>(rd._dependsOnStyleResources);
      _mergedDictionaries = rd._mergedDictionaries; // MergedDictionaries won't be copied as they come from outside the application, see the WPF docs for that property
      if (rd._names == null)
        _names = null;
      else
      {
        _names = new Dictionary<string, object>(rd._names.Count);
        foreach (KeyValuePair<string, object> kvp in rd._names)
          if (_names.ContainsKey(kvp.Key))
            continue;
          else
            _names.Add(kvp.Key, copyManager.GetCopy(kvp.Value));
      }
      if (rd._resources == null)
        _resources = null;
      else
      {
        _resources = new Dictionary<object, object>(rd._resources.Count);
        foreach (KeyValuePair<object, object> kvp in rd._resources)
        {
          object valueCopy = copyManager.GetCopy(kvp.Value);
          _resources.Add(copyManager.GetCopy(kvp.Key), valueCopy);
          Registration.SetOwner(valueCopy, this);
        }
      }
    }

    public override void Dispose()
    {
      if (_resources != null)
        foreach (object res in _resources.Values)
          // Really dispose objects if we are the owner
          Registration.CleanupAndDisposeResourceIfOwner(res, this);
      base.Dispose();
    }

    #endregion

    #region Protected members

    public static object FindResourceInParserContext(object resourceKey, IParserContext context)
    {
      object result = null;
      // Step up the parser's context stack to find the resource.
      // The logical tree is not yet defined at the load time of the
      // XAML file. This is the reason why we have to step up the parser's context
      // stack. We will have to simulate the process of finding a resource
      // which is normally done by UIElement.FindResource(string).
      // The parser's context stack maintains a dictionary of current keyed
      // elements for each stack level because the according resource
      // dictionaries are not built yet.
      foreach (ElementContextInfo current in context.ContextStack)
      {
        if (current.TryGetKeyedElement(resourceKey, out result) ||
            // Don't call UIElement.FindResource here, because the logical tree
            // may be not set up yet.
            (current.Instance is UIElement && ((UIElement) current.Instance).Resources.TryGetValue(resourceKey, out result)) ||
            (current.Instance is ResourceDictionary && ((ResourceDictionary) current.Instance).TryGetValue(resourceKey, out result)))
          break;
      }
      if (result == null)
        return null;
      IEnumerable<IBinding> deferredBindings; // Don't execute bindings in copy
      // We do a copy of the result to avoid later problems when the property where the result is assigned to is copied.
      // If we don't cut the result's logical parent, a deep copy of the here assigned property would still reference
      // the static resource's logical parent, which would copy an unnecessary big tree.
      // And we cannot simply clean the logical parent of the here found resource because we must not change it.
      // So we must do a copy where we cut the logical parent.
      return MpfCopyManager.DeepCopyCutLP(result, out deferredBindings);
    }

    public static void RegisterUnmodifiableResourceDuringParsingProcess(IUnmodifiableResource resource, IParserContext context)
    {
      IEnumerator<ElementContextInfo> enumer = ((IEnumerable<ElementContextInfo>) context.ContextStack).GetEnumerator();
      if (!enumer.MoveNext())
        return;
      if (!enumer.MoveNext())
        return;
      ResourceDictionary rd = enumer.Current.Instance as ResourceDictionary;
      if (rd != null)
        Registration.SetOwner(resource, rd);
    }

    internal IDictionary<object, object> GetOrCreateUnderlayingDictionary()
    {
      return _resources ?? (_resources = new Dictionary<object, object>());
    }

    internal void Add(object key, object value, bool fireChanged)
    {
      IDictionary<object, object> resources = GetOrCreateUnderlayingDictionary();
      resources.Add(key, value);
      Registration.SetOwner(value, this);
      if (fireChanged)
        FireChanged();
    }

    internal bool Remove(object key, bool fireChanged)
    {
      if (_resources == null)
        return false;
      object oldRes;
      if (_resources.TryGetValue(key, out oldRes))
        Registration.CleanupAndDisposeResourceIfOwner(oldRes, this);
      bool result = _resources.Remove(key);
      if (fireChanged)
        FireChanged();
      return result;
    }

    internal void Set(object key, object value, bool fireChanged)
    {
      IDictionary<object, object> resources = GetOrCreateUnderlayingDictionary();
      object oldRes;
      if (resources.TryGetValue(key, out oldRes))
        Registration.CleanupAndDisposeResourceIfOwner(oldRes, this);
      resources[key] = value;
      Registration.SetOwner(value, this);
      if (fireChanged)
        FireChanged();
    }

    protected IList<ResourceDictionary> GetOrCreateMergedDictionaries()
    {
      return _mergedDictionaries ?? (_mergedDictionaries = new List<ResourceDictionary>());
    }

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

    protected void SetName(string name, object instance)
    {
      IDictionary<string, object> names = GetOrCreateNames();
      names[name] = instance;
    }

    #endregion

    #region Public properties & events

    public event ResourcesChangedHandler ResourcesChanged
    {
      add { _resourcesChangedDelegate.Attach(value); }
      remove { _resourcesChangedDelegate.Detach(value); }
    }

    /// <summary>
    /// Gets or sets the source file for a dictionary to be merged into this dictionary.
    /// The value has to be a relative resource path, which will be searched as a resource
    /// in the current skin context (See <see cref="SkinContext.SkinResources"/>).
    /// </summary>
    public string Source
    {
      get { return _source; }
      set { _source = value; }
    }

    /// <summary>
    /// Can be used to declare dependencies to some style resources. If specified, this
    /// <see cref="ResourceDictionary"/> will try to load the specified style resources
    /// first.
    /// </summary>
    /// <remarks>
    /// The styles have to be given in a comma-separated list of style resources.
    /// Each value specified in this list should denote the name of a style resource
    /// file, that means a file name relative to the style directory, without the ".xaml"
    /// extension.
    /// </remarks>
    public string DependsOnStyleResources
    {
      get { return StringUtils.Join(", ", _dependsOnStyleResources); }
      set
      {
        if (value == null)
          return;
        string[] styleResources = value.Split(',');
        _dependsOnStyleResources.Clear();
        foreach (string styleResource in styleResources)
        {
          _dependsOnStyleResources.Add(styleResource.Trim());
          SkinContext.SkinResources.CheckStyleResourceFileWasLoaded(styleResource);
        }
      }
    }

    public IList<ResourceDictionary> MergedDictionaries
    {
      get { return GetOrCreateMergedDictionaries(); }
      set { _mergedDictionaries = value; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Takes over the control over the resources in the given resource dictionary. That means, the
    /// <see cref="DependencyObject.LogicalParent"/> properties of the resource keys and values will be set to this
    /// instance.
    /// </summary>
    /// <param name="dict">Resource dictionary whose contents should be taken over.</param>
    /// <param name="overwriteNames">If set to <c>true</c>, name collisions between the <paramref name="dict"/> and
    /// this dictionary will be ignored.</param>
    /// <param name="takeoverDictInstance">If set to <c>true</c>, the given <paramref name="dict"/> instance will be
    /// disposed by this method. Else, the <paramref name="dict"/> will be left untouched.</param>
    public void TakeOver(ResourceDictionary dict, bool overwriteNames, bool takeoverDictInstance)
    {
      bool wasChanged = false;
      foreach (KeyValuePair<object, object> entry in (IDictionary<object, object>) dict)
      {
        object key = entry.Key;
        object value = entry.Value;
        Set(key, value, false);
        Registration.SetOwner(value, this);
        DependencyObject depObj = key as DependencyObject;
        if (depObj != null)
          depObj.LogicalParent = this;
        depObj = value as DependencyObject;
        if (depObj != null)
          depObj.LogicalParent = this;
        wasChanged = true;
      }
      if (dict._names != null)
        foreach (KeyValuePair<string, object> kvp in dict._names)
          if (overwriteNames)
            SetName(kvp.Key, kvp.Value);
          else
            RegisterName(kvp.Key, kvp.Value);
      if (takeoverDictInstance)
      {
        if (dict._resources != null)
          dict._resources.Clear();
        dict.Dispose();
      }
      if (wasChanged)
        FireChanged();
    }

    /// <summary>
    /// Merges the resources in the given resource dictionary to this resource dictionary. That means, the given dict
    /// will be copied and then the copy will be used as parameter for <see cref="TakeOver"/>.
    /// instance.
    /// </summary>
    /// <param name="dict">Resource dictionary whose contents should be merged.</param>
    public void Merge(ResourceDictionary dict)
    {
      TakeOver(MpfCopyManager.DeepCopyCutLP(dict), false, true);
    }

    public void RemoveResources(ResourceDictionary dict)
    {
      if (_resources == null)
        return;
      bool wasChanged = false;
      foreach (object key in dict.Keys)
      {
        object oldObj;
        if (_resources.TryGetValue(key, out oldObj))
        {
          Registration.CleanupAndDisposeResourceIfOwner(oldObj, this);
          _resources.Remove(key);
          wasChanged = true;
        }
      }
      if (wasChanged)
        FireChanged();
    }

    public void FireChanged()
    {
      _resourcesChangedDelegate.Fire(new object[] {this});
    }

    #endregion

    #region IInitializable implementation

    public override void FinishInitialization(IParserContext context)
    {
      base.FinishInitialization(context);
      if (!string.IsNullOrEmpty(_source))
      {
        string sourceFilePath = SkinContext.SkinResources.GetResourceFilePath(_source);
        if (sourceFilePath == null)
          throw new XamlLoadException("Could not open ResourceDictionary source file '{0}' (evaluated path is '{1}')", _source, sourceFilePath);
        object obj = XamlLoader.Load(sourceFilePath,
            (IModelLoader) context.GetContextVariable(typeof(IModelLoader)),
            (bool) context.GetContextVariable(KEY_ACTIVATE_BINDINGS));
        ResourceDictionary mergeDict = obj as ResourceDictionary;
        if (mergeDict == null)
        {
          if (obj != null)
            TryDispose(ref obj);
          throw new Exception(String.Format("Resource '{0}' doesn't contain a resource dictionary", _source));
        }
        TakeOver(mergeDict, false, true);
      }
      if (_mergedDictionaries != null && _mergedDictionaries.Count > 0)
      {
        foreach (ResourceDictionary dictionary in _mergedDictionaries)
          TakeOver(dictionary, false, true);
        _mergedDictionaries.Clear();
      }
      FireChanged();
    }

    #endregion

    #region INameScope implementation

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

    #region IDictionary<object,object> implementation

    public bool ContainsKey(object key)
    {
      return _resources != null && _resources.ContainsKey(key);
    }

    public void Add(object key, object value)
    {
      Add(key, value, true);
    }

    public bool Remove(object key)
    {
      return Remove(key, true);
    }

    public bool TryGetValue(object key, out object value)
    {
      value = null;
      return _resources != null && _resources.TryGetValue(key, out value);
    }

    public object this[object key]
    {
      get
      {
        if (key == null)
          throw new ArgumentNullException("key");
        if (_resources == null)
          throw new KeyNotFoundException(string.Format("Key '{0}' was not found in this ResourceDictionary", key));
        return _resources[key];
      }
      set { Set(key, value, true); }
    }

    public ICollection<object> Keys
    {
      get { return _resources == null ? EMPTY_OBJECT_COLLECTION : _resources.Keys; }
    }

    public ICollection<object> Values
    {
      get { return _resources == null ? EMPTY_OBJECT_COLLECTION : _resources.Values; }
    }

    #endregion

    #region ICollection<KeyValuePair<object,object>> implementation

    public void Add(KeyValuePair<object, object> item)
    {
      IDictionary<object, object> resources = GetOrCreateUnderlayingDictionary();
      resources.Add(item);
      Registration.SetOwner(item.Value, this);
      FireChanged();
    }

    public void Clear()
    {
      if (_resources == null)
        return;
      foreach (KeyValuePair<object, object> kvp in _resources)
        Registration.CleanupAndDisposeResourceIfOwner(kvp.Value, this);
      _resources = null;
      FireChanged();
    }

    public bool Contains(KeyValuePair<object, object> item)
    {
      return _resources != null && _resources.Contains(item);
    }

    public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
    {
      if (_resources == null)
        return;
      _resources.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<object, object> item)
    {
      if (_resources == null)
        return false;
      object oldRes;
      if (_resources.TryGetValue(item.Key, out oldRes))
        Registration.CleanupAndDisposeResourceIfOwner(oldRes, this);
      bool result = _resources.Remove(item);
      FireChanged();
      return result;
    }

    public int Count
    {
      get { return _resources == null ? 0 : _resources.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IEnumerable<KeyValuePair<string,object>> implementation

    IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
    {
      return (_resources ?? EMPTY_KVP_COLLECTION).GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return (_resources ?? EMPTY_KVP_COLLECTION).GetEnumerator();
    }

    #endregion
  }
}
