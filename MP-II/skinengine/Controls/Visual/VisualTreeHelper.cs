using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace SkinEngine.Controls.Visuals
{
  public class VisualTreeHelper
  {
    UIElement _root;
    Dictionary<string, object> _cache;
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
      _cache = new Dictionary<string, object>();
    }

    public object FindElement(UIElement visual, string name)
    {
      string[] parts = name.Split(new char[] { '.' });
      object obj = visual.FindElement(parts[0]);
      if (obj == null) return null;
      if (parts.Length == 1)
      {
        return obj;
      }
      if (obj == null)
        return null;

      int partNr = 1;
      int indexNo;
      while (partNr < parts.Length)
      {
        indexNo = -1;
        int p1 = parts[partNr].IndexOf('[');
        if (p1 > 0)
        {
          int p2 = parts[partNr].IndexOf(']');
          string indexStr = parts[partNr].Substring(p1 + 1, (p2 - p1) - 1);
          indexNo = Int32.Parse(indexStr);
          parts[partNr] = parts[partNr].Substring(0, p1);
        }
        object res = null;

        MethodInfo info =
         obj.GetType().GetProperty(parts[partNr],
                                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
        if (info == null)
        {
          ServiceScope.Get<ILogger>().Error("cannot get object for {0}", name);
          return null;
        }
        res = info.Invoke(obj, null);
        if (res == null)
        {
          return null;
        }
        partNr++;
        obj = res;
        if (indexNo >= 0)
        {
          info = obj.GetType().GetProperty("Item",
                                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", name);
            return null;
          }
          res = info.Invoke(obj, new object[] { indexNo });
          if (res == null)
          {
            return null;
          }
          obj = res;
        }
      }
      return obj;
    }
    /// <summary>
    /// Finds the element with the name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public object FindElement(string name)
    {
      if (_cache.ContainsKey(name))
      {
        return _cache[name];
      }
      string[] parts = name.Split(new char[] { '.' });
      object obj = _root.FindElement(parts[0]);
      if (parts.Length == 1)
      {
        _cache[name] = obj;
        return obj;
      }
      if (obj == null)
        return null;

      int partNr = 1;
      int indexNo;
      while (partNr < parts.Length)
      {
        indexNo = -1;
        int p1 = parts[partNr].IndexOf('[');
        if (p1 > 0)
        {
          int p2 = parts[partNr].IndexOf(']');
          string indexStr = parts[partNr].Substring(p1 + 1, (p2 - p1) - 1);
          indexNo = Int32.Parse(indexStr);
          parts[partNr] = parts[partNr].Substring(0, p1);
        }
        object res = null;

        MethodInfo info =
         obj.GetType().GetProperty(parts[partNr],
                                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
        if (info == null)
        {
          ServiceScope.Get<ILogger>().Error("cannot get object for {0}", name);
          return null;
        }
        res = info.Invoke(obj, null);
        if (res == null)
        {
          return null;
        }
        partNr++;
        obj = res;
        if (indexNo >= 0)
        {
          info = obj.GetType().GetProperty("Item",
                                   BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.InvokeMethod | BindingFlags.ExactBinding).GetGetMethod();
          if (info == null)
          {
            ServiceScope.Get<ILogger>().Error("cannot get object for {0}", name);
            return null;
          }
          res = info.Invoke(obj, new object[] { indexNo });
          if (res == null)
          {
            return null;
          }
          obj = res;
        }
      }
      return obj;
    }
  }
}
