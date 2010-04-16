#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Core.General;
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

    #region Ctor

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
          _resources.Add(copyManager.GetCopy(kvp.Key), copyManager.GetCopy(kvp.Value));
      }
    }

    #endregion

    #region Protected members

    internal IDictionary<object, object> GetOrCreateUnderlayingDictionary()
    {
      if (_resources == null)
        _resources = new Dictionary<object, object>();
      return _resources;
    }

    protected IList<ResourceDictionary> GetOrCreateMergedDictionaries()
    {
      if (_mergedDictionaries == null)
        _mergedDictionaries = new List<ResourceDictionary>();
      return _mergedDictionaries;
    }

    protected IDictionary<string, object> GetOrCreateNames()
    {
      if (_names == null)
        _names = new Dictionary<string, object>();
      return _names;
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

    public void Merge(ResourceDictionary dict)
    {
      IEnumerator<KeyValuePair<object, object>> enumer = ((IDictionary<object, object>) dict).GetEnumerator();
      bool wasChanged = false;
      while (enumer.MoveNext())
      {
        this[enumer.Current.Key] = enumer.Current.Value;
        wasChanged = true;
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

    public override void Initialize(IParserContext context)
    {
      base.Initialize(context);
      if (!string.IsNullOrEmpty(_source))
      {
        string includeFilePath = SkinContext.SkinResources.GetResourceFilePath(_source);
        if (includeFilePath == null)
          throw new XamlLoadException("Could not open include file '{0}'", includeFilePath);
        ResourceDictionary mergeDict = XamlLoader.Load(includeFilePath,
            (IModelLoader) context.GetContextVariable(typeof(IModelLoader))) as ResourceDictionary;
        if (mergeDict == null)
          throw new Exception(String.Format("Resource '{0}' doesn't contain a resource dictionary", _source));
        Merge(mergeDict);
      }
      if (_mergedDictionaries != null && _mergedDictionaries.Count > 0)
      {
        foreach (ResourceDictionary dictionary in _mergedDictionaries)
          Merge(dictionary);
      }
      FireChanged();
    }

    #endregion

    #region INameScope implementation

    public object FindName(string name)
    {
      if (_names != null && _names.ContainsKey(name))
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
      IDictionary<string, object> names = GetOrCreateNames();
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
      IDictionary<object, object> resources = GetOrCreateUnderlayingDictionary();
      resources.Add(key, value);
      FireChanged();
    }

    public bool Remove(object key)
    {
      if (_resources == null)
        return false;
      bool result = _resources.Remove(key);
      FireChanged();
      return result;
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
        if (_resources == null)
        {
          if (key == null)
            throw new ArgumentNullException("key");
          throw new KeyNotFoundException(string.Format("Key '{0}' was not found in this ResourceDictionary", key));
        }
        return _resources[key];
      }
      set
      {
        IDictionary<object, object> resources = GetOrCreateUnderlayingDictionary();
        resources[key] = value;
        FireChanged();
      }
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
      FireChanged();
    }

    public void Clear()
    {
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
