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
using Presentation.SkinEngine.Loader;
using Presentation.SkinEngine.XamlParser;

namespace Presentation.SkinEngine.Controls.Resources
{
  public class ResourceDictionary: Dictionary<string, object>, IInitializable, INameScope
  {
    protected Property _sourceProperty = new Property(typeof(string), "");
    protected Property _mergedDictionariesProperty = new Property(typeof(ICollection<ResourceDictionary>), new List<ResourceDictionary>());
    protected IDictionary<string, object> _names = new Dictionary<string, object>();
    protected INameScope _parent = null;

    public ResourceDictionary() { }

    public string Source
    {
      get
      {
        return _sourceProperty.GetValue() as string;
      }
      set
      {
        _sourceProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the source property.
    /// </summary>
    /// <value>The source property.</value>
    public Property SourceProperty
    {
      get
      {
        return _sourceProperty;
      }
    }

    public ICollection<ResourceDictionary> MergedDictionaries
    {
      get
      {
        return _mergedDictionariesProperty.GetValue() as ICollection<ResourceDictionary>;
      }
      set
      {
        _sourceProperty.SetValue(value);
      }
    }

    public Property MergedDictionariesProperty
    {
      get
      {
        return _mergedDictionariesProperty;
      }
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
