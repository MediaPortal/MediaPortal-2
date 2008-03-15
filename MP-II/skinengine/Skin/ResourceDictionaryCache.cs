using System;
using System.Collections.Generic;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Skin
{                                        
  /// <summary>
  /// Holds a static mapping from resource names to <see cref="ResourceDictionary"/>
  /// instances and implements a getter method for loading a new
  /// <see cref="ResourceDictionary"/>, given its name.
  /// </summary>
  public class ResourceDictionaryCache
  {
    static ResourceDictionaryCache _instance;
    Dictionary<string, ResourceDictionary> _dictionaries;
    protected ResourceDictionaryCache()
    {
      _dictionaries = new Dictionary<string, ResourceDictionary>();
    }

    static public ResourceDictionaryCache Instance
    {
      get
      {
        if (_instance == null)
          _instance = new ResourceDictionaryCache();
        return _instance;
      }
    }

    public ResourceDictionary Get(string resourceName)
    {
      if (!_dictionaries.ContainsKey(resourceName))
      {
        XamlLoader loader = new XamlLoader();
        _dictionaries[resourceName] = (loader.Load(resourceName) as ResourceDictionary);
      }
      return _dictionaries[resourceName];
    }
  }
}
