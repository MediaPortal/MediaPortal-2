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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MediaPortal.SkinEngine.Controls;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.MpfElements.Resources
{
  public delegate void ResourcesChangedHandler(ResourceDictionary changedResources);

  public class ResourceDictionary: DependencyObject, IDictionary<string, object>, IInitializable, INameScope, IDeepCopyable
  {
    #region Protected fields

    protected string _source = "";
    protected IList<ResourceDictionary> _mergedDictionaries = new List<ResourceDictionary>();
    protected IDictionary<string, object> _names = new Dictionary<string, object>();
    protected INameScope _parent = null;
    protected IDictionary<string, object> _resources = new Dictionary<string, object>();

    #endregion

    #region Ctor

    public ResourceDictionary() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ResourceDictionary rd = (ResourceDictionary) source;
      Source = copyManager.GetCopy(rd.Source);
      foreach (ResourceDictionary crd in rd._mergedDictionaries)
        _mergedDictionaries.Add(copyManager.GetCopy(crd));
      foreach (KeyValuePair<string, object> kvp in rd._names)
        if (_names.ContainsKey(kvp.Key))
          continue;
        else
          _names.Add(copyManager.GetCopy(kvp.Key), copyManager.GetCopy(kvp.Value));
      _parent = copyManager.GetCopy(rd._parent);
    }

    #endregion

    #region Public properties & events

    public event ResourcesChangedHandler ResourcesChanged;

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

    public IDictionary<string, object> UnderlayingDictionary
    {
      get { return _resources; }
    }

    public IList<ResourceDictionary> MergedDictionaries
    {
      get { return _mergedDictionaries; }
      set { _mergedDictionaries = value; }
    }

    #endregion

    #region Public methods

    public void Merge(ResourceDictionary dict)
    {
      IEnumerator<KeyValuePair<string, object>> enumer = ((IDictionary<string, object>) dict).GetEnumerator();
      while (enumer.MoveNext())
        this[enumer.Current.Key] = enumer.Current.Value;
      FireChanged();
    }

    public void FireChanged()
    {
      if (ResourcesChanged != null)
        ResourcesChanged(this);
    }

    #endregion

    #region IInitializable implementation

    public void Initialize(IParserContext context)
    {
      if (!string.IsNullOrEmpty(_source))
      {
        string includeFilePath = SkinContext.SkinResources.GetResourceFilePath(_source);
        if (includeFilePath == null)
          throw new XamlLoadException("Could not open include file '{0}'", includeFilePath);
        ResourceDictionary mergeDict = context.LoadXaml(includeFilePath) as ResourceDictionary;
        if (mergeDict == null)
          throw new Exception(String.Format("Resource '{0}' doesn't contain a resource dictionary", _source));
        Merge(mergeDict);
      }
      if (MergedDictionaries.Count > 0)
      {
        foreach (ResourceDictionary dictionary in MergedDictionaries)
          Merge(dictionary);
      }
      FireChanged();
    }

    #endregion

    #region INameScope implementation

    public object FindName(string name)
    {
      if (_names.ContainsKey(name))
        return _names[name];
      else if (_parent != null)
        return _parent.FindName(name);
      else
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

    public void RegisterParent(INameScope parent)
    {
      _parent = parent;
    }

    #endregion

    #region IDictionary<string,object> implementation

    public bool ContainsKey(string key)
    {
      return _resources.ContainsKey(key);
    }

    public void Add(string key, object value)
    {
      _resources.Add(key, value);
      FireChanged();
    }

    public bool Remove(string key)
    {
      bool result = _resources.Remove(key);
      FireChanged();
      return result;
    }

    public bool TryGetValue(string key, out object value)
    {
      return _resources.TryGetValue(key, out value);
    }

    public object this[string key]
    {
      get { return _resources[key]; }
      set
      {
        _resources[key] = value;
        FireChanged();
      }
    }

    public ICollection<string> Keys
    {
      get { return _resources.Keys; }
    }

    public ICollection<object> Values
    {
      get { return _resources.Values; }
    }

    #endregion

    #region ICollection<KeyValuePair<string,object>> implementation

    public void Add(KeyValuePair<string, object> item)
    {
      _resources.Add(item);
      FireChanged();
    }

    public void Clear()
    {
      _resources.Clear();
      FireChanged();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
      return _resources.Contains(item);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      _resources.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
      return _resources.Remove(item);
      FireChanged();
    }

    public int Count
    {
      get { return _resources.Count; }
    }

    public bool IsReadOnly
    {
      get { return false; }
    }

    #endregion

    #region IEnumerable<KeyValuePair<string,object>> implementation

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
      return _resources.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return _resources.GetEnumerator();
    }

    #endregion
  }
}
