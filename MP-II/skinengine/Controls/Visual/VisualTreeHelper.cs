using System;
using System.Collections.Generic;
using System.Text;

namespace SkinEngine.Controls.Visuals
{
  public class VisualTreeHelper
  {
    UIElement _root;
    Dictionary<string, UIElement> _cache;
    static VisualTreeHelper _instance;

    public static VisualTreeHelper Instance
    {
      get
      {
        if (_instance == null)
          _instance = new VisualTreeHelper();
        return _instance;
      }
    }

    /// <summary>
    /// Sets the root element.
    /// </summary>
    /// <param name="element">The element.</param>
    public void SetRootElement(UIElement element)
    {
      _root = element;
      _cache = new Dictionary<string, UIElement>();
    }

    /// <summary>
    /// Finds the element with the name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public UIElement FindElement(string name)
    {
      if (_cache.ContainsKey(name))
      {
        return _cache[name];
      }
      return _root.FindElement(name);
    }
  }
}
