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

using System.Xml;
using System.Collections;
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Xaml
{
  /// <summary>
  /// Represents one context stack entry.
  /// </summary>
  public class ElementContextInfo
  {
    #region Variables

    protected object _instanciationData;
    protected object _instance = null;
    protected int _namespaceCount = 0;
    protected IDictionary<object, object> _keyedElements =
      new Dictionary<object, object>();

    #endregion

    #region Constructors/Destructors

    internal ElementContextInfo(XmlElement element)
    {
      _instanciationData = element;
    }

    internal ElementContextInfo(string instantiationExpression)
    {
      _instanciationData = instantiationExpression;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// The object which was instantiated for the corresponding XAML element.
    /// This property will be initialized deferred.
    /// </summary>
    public object Instance
    {
      get { return _instance; }
      set { _instance = value; }
    }

    /// <summary>
    /// The XML element corresponding to this element context information. May be <c>null</c>
    /// in case the current context was instantiated by an attribute value instantiation syntax.
    /// </summary>
    public XmlElement Element
    {
      get { return _instanciationData as XmlElement; }
    }

    /// <summary>
    /// The expression used to instantiate the current element context. May be <c>null</c>
    /// in case the current context was instantiated by using an <see cref="XmlElement"/>.
    /// </summary>
    public string InstantiationExpression
    {
      get { return _instanciationData as string; }
    }

    /// <summary>
    /// The number of namespaces stored for this context. The namespaces
    /// themselves are stored in an own namespace cache for performance
    /// reasons.
    /// </summary>
    public int NamespaceCount
    {
      get { return _namespaceCount; }
      set { _namespaceCount = value; }
    }

    #endregion

    #region Public methods

    public void AddKeyedElement(object key, object value)
    {
      _keyedElements.Add(key, value);
    }

    public bool ContainsKey(object key)
    {
      return _keyedElements.ContainsKey(key);
    }

    public object GetKeyedElement(object key)
    {
      return _keyedElements[key];
    }

    #endregion
  }

  /// <summary>
  /// Represents a stack of element context information, to be maintained
  /// during the recursive parsing process of the XAML parser.
  /// </summary>
  /// <remarks>
  /// Holds the parsing context data in form of <see cref="ElementContextInfo"/>
  /// instances.
  /// This stack stores the element context objects on the path to the root
  /// element.
  /// </remarks>
  public class ElementContextStack: IEnumerable<ElementContextInfo>
  {
    #region Variables

    /// <summary>
    /// Holds the context information stack of <see cref="ElementContextInfo"/>
    /// instances.
    /// </summary>
    protected Stack<ElementContextInfo> _contextStack =
        new Stack<ElementContextInfo>();

    /// <summary>
    /// Holds a separate stack of defined namespaces. This stack doesn't contain
    /// exactly one entry per <see cref="ElementContextInfo"/> entry on the
    /// <see cref="_contextStack"/>. Instead, for each entry in the context stack
    /// this instance contains zero or more namespace definitions.
    /// How many namespace definitions on this stack correspond to each
    /// context entry is stored in each context entry's attribute
    /// <see cref="ElementContextInfo.NamespaceCount"/>.
    /// </summary>
    protected Stack<KeyValuePair<string, INamespaceHandler>> _namespaceStack =
        new Stack<KeyValuePair<string, INamespaceHandler>>();

    // Cache variables
    protected INamespaceHandler _currentNamespaceHandler = null;

    #endregion

    #region Constructor

    public ElementContextStack() {}

    #endregion

    #region Public methods

    /// <summary>
    /// Creates a new <see cref="ElementContextInfo"/> structure for the specified
    /// XML element and pushes it on top of the stack of element context infos.
    /// </summary>
    /// <param name="currentElement">The XML element specifying the new element to push.</param>
    /// <returns>The created element's context info instance.</returns>
    public ElementContextInfo PushElementContext(XmlElement currentElement)
    { // This code is almost the same as PushElementContext(string)
      // Clear cache
      _currentNamespaceHandler = null;
      // Create new stack entry
      ElementContextInfo result = new ElementContextInfo(currentElement);
      _contextStack.Push(result);
      return result;
    }

    /// <summary>
    /// Creates a new <see cref="ElementContextInfo"/> structure for the specified
    /// string in attribute value instantiation syntax and pushes it on top of the
    /// stack of element context infos.
    /// </summary>
    /// <param name="currentInstantiationExpression">The instantiation expression for the
    /// new element to push.</param>
    /// <returns>The created element's context info instance.</returns>
    public ElementContextInfo PushElementContext(string currentInstantiationExpression)
    { // This code is almost the same as PushElementContext(XmlElement)
      // Clear cache
      _currentNamespaceHandler = null;
      // Create new stack entry
      ElementContextInfo result = new ElementContextInfo(currentInstantiationExpression);
      _contextStack.Push(result);
      return result;
    }

    /// <summary>
    /// Pops the current <see cref="ElementContextInfo"/> from the context element
    /// stack.
    /// </summary>
    public void PopElementContext()
    {
      // Clear cache
      _currentNamespaceHandler = null;
      // Remove all namespace definitions corresponding to the current element's context
      for (int i=CurrentElementContext.NamespaceCount; i>0; i--)
        _namespaceStack.Pop();
      // Remove element's context itself
      _contextStack.Pop();
    }

    /// <summary>
    /// Registers a local namespace handler for the specified
    /// <paramref name="namespaceURI"/>, which was declared in the XAML
    /// element corresponding to the current <see cref="ElementContextInfo"/>.
    /// </summary>
    /// <param name="namespaceURI">Namespace URI to be used as key for the
    /// <paramref name="handler"/>.</param>
    /// <param name="handler">Namespace handler object for the namespace.</param>
    public void RegisterNamespaceHandler(string namespaceURI, INamespaceHandler handler)
    {
      _contextStack.Peek().NamespaceCount++;
      _namespaceStack.Push(new KeyValuePair<string, INamespaceHandler>(namespaceURI, handler));
    }

    /// <summary>
    /// Returns the namespace handler for the specified <paramref name="namespaceURI"/>,
    /// which was registered with the specified <paramref name="namespaceURI"/>,
    /// either in the current element's context or in any parent context.
    /// </summary>
    /// <param name="namespaceURI">The URI for that the namespace handler was
    /// registered.</param>
    /// <returns>Namespace handler object which was registered for the specified
    /// <paramref name="namespaceURI"/>.</returns>
    /// <exception cref="XamlParserException">If the specified namespace was not (yet)
    /// registered, i.e. neither was registered in the current element's context,
    /// nor in any parent context.</exception>
    public INamespaceHandler GetNamespaceHandler(string namespaceURI)
    {
      // Albert78, 5.4.08: We don't use .net 3.5, so System.Linq.Stack.First is not available
      //KeyValuePair<string, IXamlNamespaceHandler> kvp = _namespaceStack.First(kvp => kvp.Key == namespaceURI);
      //if (kvp == null)
      //  throw new XamlParserException("Namespace-URI '{0}' was not declared", namespaceURI);
      //else
      //  return kvp.Value;
      foreach (KeyValuePair<string, INamespaceHandler> kvp in _namespaceStack)
        if (kvp.Key == namespaceURI)
          return kvp.Value;
      throw new XamlParserException("Namespace-URI '{0}' was not declared", namespaceURI);
    }

    public string GetNamespaceOfPrefix(string prefix)
    {
      XmlElement ele = null;
      // Find next element which was instantiated by an XmlElement,
      // skip elements instantiated using attribute value instantiation syntax
      foreach (ElementContextInfo eci in this)
        if ((ele = eci.Element) != null)
          break;
      if (ele == null)
        return string.Empty;
      string result = ele.GetNamespaceOfPrefix(prefix);
      if (string.IsNullOrEmpty(result))
        throw new XamlParserException("Namespace prefix '{0}' is not declared", prefix);
      return result;
    }

    public INameScope GetCurrentNameScope()
    {
      foreach (ElementContextInfo eci in this)
        if (eci.Instance is INameScope)
          return (INameScope) eci.Instance;
      return null;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the current element's context information object.
    /// </summary>
    public ElementContextInfo CurrentElementContext
    {
      get { return _contextStack.Peek(); }
    }

    #endregion

    #region IEnumerable<ElementContextInfo> implementation

    IEnumerator<ElementContextInfo> IEnumerable<ElementContextInfo>.GetEnumerator()
    {
      return _contextStack.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _contextStack.GetEnumerator();
    }

    #endregion
  }
}
