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
using Presentation.SkinEngine.Loader;
using Presentation.SkinEngine.XamlParser;
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Resources
{
  public class ResourceDictionary: Dictionary<string, object>, IInitializable, INameScope, IDeepCopyable
  {
    protected string _source = "";
    protected IList<ResourceDictionary> _mergedDictionaries = new List<ResourceDictionary>();
    protected IDictionary<string, object> _names = new Dictionary<string, object>();
    protected INameScope _parent = null;

    public ResourceDictionary() { }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      ResourceDictionary rd = source as ResourceDictionary;
      Source = copyManager.GetCopy(rd.Source);
      foreach (ResourceDictionary crd in rd._mergedDictionaries)
        _mergedDictionaries.Add(copyManager.GetCopy(crd));
      foreach (KeyValuePair<string, object> kvp in rd._names)
        _names.Add(kvp.Key, copyManager.GetCopy(kvp.Value));
      _parent = copyManager.GetCopy(rd._parent);
    }

    public string Source
    {
      get { return _source; }
      set { _source = value; }
    }

    public IList<ResourceDictionary> MergedDictionaries
    {
      get { return _mergedDictionaries; }
      set { _mergedDictionaries = value; }
    }

    public void Merge(ResourceDictionary dict)
    {
      IEnumerator<KeyValuePair<string, object>> enumer = dict.GetEnumerator();
      while (enumer.MoveNext())
        this[enumer.Current.Key] = enumer.Current.Value;
    }

    #region IInitializable implementation

    public void Initialize(IParserContext context)
    {
      if (!string.IsNullOrEmpty(Source))
      {
        XamlLoader loader = new XamlLoader();
        ResourceDictionary mergeDict = loader.Load(Source) as ResourceDictionary;
        if (mergeDict == null)
          throw new Exception(String.Format("Resource '{0}' doesn't contain a resource dictionary", Source));
        Merge(mergeDict);
      }
      if (MergedDictionaries.Count > 0)
      {
        foreach (ResourceDictionary dictionary in MergedDictionaries)
        {
          XamlLoader loader = new XamlLoader();
          Merge(dictionary);
        }
      }
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
  }
}
