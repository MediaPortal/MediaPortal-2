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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using MediaPortal.SkinEngine.Xaml.Exceptions;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.SkinEngine.Xaml.XamlNamespace;

namespace MediaPortal.SkinEngine.Xaml
{
  /// <summary>
  /// The XAML object graph parser.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Each parser instance is associated with a XAML file to parse. This means,
  /// for each XAML file to parse you'll need a new instance of the
  /// <see cref="Parser"/> class.
  /// </para>
  /// <para>
  /// <b>Preparation</b><br/>
  /// The parser will be created by a call to its
  /// <see cref="Parser">constructor</see>,
  /// where you should provide it the necessary data and callback functions to work.
  /// Once created, the parser is already functional. But before starting the
  /// parsing operation, you will likely first assign a delegate for custom
  /// type conversions, <see cref="SetCustomTypeConverter"/>.
  /// </para>
  /// <para>
  /// <b>Parsing operation</b><br/>
  /// The parsing operation starts when the method <see cref="Parser.Parse()"/>
  /// or <see cref="Parser.Parse()"/>
  /// is called.
  /// The parser will first read the XAML file with an XML reader. This will
  /// result in checking the conformance of the file to the XML language specification.
  /// In a second step, the parser will traverse through the XML element tree and
  /// instantiate all elements recursively.<br/>
  /// During the parsing operation, the parser instance holds context information
  /// about the current state, which includes a registry of all currently visible
  /// namespaces, all named elements created yet, and the current object which
  /// is built up, together with the context information of all parent elements
  /// up to the top of the tree. After the parsing process has finished,
  /// the <see cref="Parser.Parse()"/> method returns the root element
  /// of the created element tree.
  /// </para>
  /// <para>
  /// <b>XML Namespaces</b><br/>
  /// As defined in the XAML language specification, an XML namespace corresponds to a
  /// set of UI elements. So lets consider this example:
  /// <code>
  ///   <Page
  ///     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  ///   </Page>
  /// </code>
  /// The namespace URI in this example,
  /// "http://schemas.microsoft.com/winfx/2006/xaml/presentation", corresponds to
  /// the WPF UI elements namespace.
  /// Since the XAML parser isn't tied to WPF, we won't hard-code the WPF namespace but
  /// will let the application handle namespaces. For all namespaces
  /// the application will handle, it has to provide a namespace handler via
  /// the delegate passed in the constructor.
  /// </para>
  /// </remarks>
  public class Parser: IParserContext
  {
    /// <summary>
    /// The built-in URI for xmlns namespace declaration attributes.
    /// </summary>
    public const string XMLNS_NS_URI = "http://www.w3.org/2000/xmlns/";

    /// <summary>
    /// URI for the "x:" namespace.
    /// </summary>
    public const string XAML_NS_URI = "http://schemas.microsoft.com/winfx/2006/xaml";

    #region Delegate types

    /// <summary>
    /// Describes the signature of the method which will be called when a new
    /// XAML namespace is imported.
    /// </summary>
    /// <remarks>
    /// For each namespace, the client should either return a valid
    /// <see cref="INamespaceHandler"/> to handle the namespace's elements,
    /// or throw an exception if the namespace is not supported.
    /// If multiple namespaces are supported by the client, it is possible
    /// either to return different <see cref="INamespaceHandler"/> instances
    /// for each namespace, or to reuse the same instance.
    /// Note that XMLNS namespaces (<see cref="XMLNS_NS_URI"/>) are implicitly
    /// handled by the parser.
    /// </remarks>
    /// <exception cref="XamlNamespaceNotSupportedException">If the specified namespaceURI
    /// is not supported by the client.</exception>
    public delegate INamespaceHandler ImportNamespaceDlgt(IParserContext context, string namespaceURI);

    /// <summary>
    /// Describes the signature of the method to be called when an event handler
    /// delegate is needed to be assigned to an event.
    /// <summary>
    /// <param name="signature">The signature of the requested event handler method.</param>
    /// <param name="eventName">Name of the event handler to be assigned.</param>
    /// <returns>Delegate to handle the specified event with the specified
    /// <paramref name="signature"/>.</returns>
    public delegate Delegate GetEventHandlerDlgt(IParserContext context, MethodInfo signature, string eventName);

    #endregion

    #region Protected fields

    /// <summary>
    /// Holds our delegate for importing custom namespaces.
    /// </summary>
    /// <remarks>
    /// Instead of exposing an event, we hold this delegate instance as a
    /// protected field because it is essential for the function of this parser.
    /// The field will be assigned in the constructor.
    /// </remarks>
    protected ImportNamespaceDlgt _importCustomNamespace;

    /// <summary>
    /// Delegate to be called when an event handler delegate is needed.
    /// </summary>
    /// <remarks>
    /// In this implementation of a XAML parser, we delegate the complete job
    /// of finding the event handler delegate to our client. This lets clients
    /// the chance to resolve the appropriate event handler theirselves. We do not
    /// specify any syntax constraints for naming event handlers; we support
    /// an arbitrary, application-defined declaration syntax for the value in the
    /// event attribute of the XAML element.
    /// In this method, the client has the job to find a handler method, which
    /// matches the specified signature, and to return it.
    /// </remarks>
    protected GetEventHandlerDlgt _getEventHandler;

    /// <summary>
    /// Holds our delegate to convert custom types. May be null if no custom
    /// types are supported.
    /// </summary>
    protected ConvertTypeDlgt _customTypeConverter = null;

    /// <summary>
    /// The input for the XAML content to process.
    /// </summary>
    protected TextReader _reader;

    /// <summary>
    /// The document being processed.
    /// </summary>
    protected XmlDocument _xmlDocument;

    /// <summary>
    /// Holds the root object which will be build up by the <see cref="Parse()"/>
    /// method.
    /// </summary>
    protected object _rootObject;

    /// <summary>
    /// The stack of element context information maintained during the instantiation
    /// process of the object graph.
    /// Holds context information about objects instanciated for each of the
    /// XML elements from root up to the current element.
    /// </summary>
    protected ElementContextStack _elementContextStack = new ElementContextStack();

    #endregion

    #region Constructor

    /// <summary>
    /// Builds a new XAML parser for the input given by the specified <paramref name="reader"/>.
    /// </summary>
    /// <remarks>
    /// The parsing operation will not start immediately, you'll first have to
    /// register all necessary namespace handlers. To start the parsing operation, call
    /// method <see cref="Parse()"/>.
    /// </remarks>
    /// <param name="reader">The reader the parser will take its input to parse.</param>
    /// <param name="importNamespace">Delegate to be called when importing
    /// a new XML/XAML namespace.</param>
    /// <param name="getEventHandler">Delegate to be called when an event handler method
    /// should be assigned.</param>
    /// <exception cref="XmlException">If there is an error while reading or parsing the content from
    /// the specified <paramref name="reader"/>.</exception>
    public Parser(TextReader reader, ImportNamespaceDlgt importNamespace,
        GetEventHandlerDlgt getEventHandler)
    {
      if (importNamespace == null)
        throw new ArgumentNullException("importNamespace", "The ImportNamespace delegate must not be null");
      _importCustomNamespace = importNamespace;
      if (getEventHandler == null)
        throw new ArgumentNullException("The GetEventHandler delegate must not be null");
      _getEventHandler = getEventHandler;
      _reader = reader;
      _xmlDocument = new XmlDocument();
      _xmlDocument.Load(_reader);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns the root object which was instantiated for the root XAML element
    /// in the XML tree. This property returns a value different from <c>null</c>,
    /// when the <see cref="Parse()"/> method has finished.
    /// </summary>
    public object RootObject
    {
      get { return _rootObject; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Registers the specified converter for non-standard custom types.
    /// </summary>
    public void SetCustomTypeConverter(ConvertTypeDlgt customTypeConverter)
    {
      TypeConverter.CustomTypeConverter = customTypeConverter;
    }

    /// <summary>
    /// Parses the XAML file associated with this parser.
    /// </summary>
    /// <returns>The visual corresponding to the root XAML element.</returns>
    public object Parse()
    {
      if (_rootObject != null)
        throw new XamlParserException("XAML Parser: Parse() method was invoked multiple times");
      object key;
      _rootObject = UnwrapIncludes(Instantiate(_xmlDocument.DocumentElement, out key));
      if (key != null)
        throw new XamlParserException("A 'x:Key' attribute is not allowed at the XAML root element");
      return _rootObject;
    }

    #endregion

    #region Protected/private methods

    /// <summary>
    /// Instantiates the specified XML element located under the current element context
    /// in the <see cref="_elementContextStack"/> and sets the resulting element up.
    /// </summary>
    /// <remarks>
    /// Before calling this, the parent element, which is the current top element
    /// on the element context stack, has to be instantiated and its namespaces
    /// have to be imported. In the current implementation, its attributes will
    /// also already be processed.
    /// When this method returns, the returned visual element is completely set up,
    /// this means, it was instantiated, its members and events were processed
    /// and its content member was assigned. The processing of the complete
    /// structure under <paramref name="currentElement"/> has finished.
    /// </remarks>
    /// <param name="currentElement">The XML element under the
    /// current element on the element's context stack, which has to be
    /// instantiated and set up.</param>
    /// <param name="key">Will be set if an <c>x:Key</c> attribute was set on the
    /// element to parse.</param>
    /// <returns>Instantiated XAML visual element corresponding to the
    /// specified <paramref name="currentElement"/>.</returns>
    protected object Instantiate(XmlElement currentElement, out object key)
    {
      ElementContextInfo elementContext = _elementContextStack.PushElementContext(currentElement);
      try
      {
        string name = null;
        key = null;

        IList<XmlAttribute> remainingAttributes = new List<XmlAttribute>(currentElement.Attributes.Count);
        // Step 1: Process namespace declarations (to be done first)
        foreach (XmlAttribute attr in currentElement.Attributes)
        {
          // We have to sort out namespace declarations.
          // For both declarations xmlns="..." and xmlns:xx="...", the parser returns the
          // namespace URI "http://www.w3.org/2000/xmlns/" (static constant
          /// <see cref="XMLNS_NS_URI"/>) for the attribute, so we check for this
          /// namespace URI. The value of the attribute (for example
          /// "http://schemas.microsoft.com/winfx/2006/xaml") is the URI
          /// of the namespace to be imported.
          if (attr.NamespaceURI == XMLNS_NS_URI)
          {
            string importNamespaceURI = attr.Value;
            INamespaceHandler handler;
            if (importNamespaceURI == XAML_NS_URI)
              // Implicit namespace: Use default x:-handler
              handler = new XamlNamespaceHandler();
            else if (importNamespaceURI.StartsWith("clr-namespace:"))
              handler = ImportClrNamespace(importNamespaceURI);
            else
              handler = _importCustomNamespace(this, importNamespaceURI);
            _elementContextStack.RegisterNamespaceHandler(importNamespaceURI, handler);
          }
          else
          {
            // Store other attributes so we don't need to sort out the namespace declarations in step 3
            remainingAttributes.Add(attr);
            continue;
          }
        }

        // Step 2: Instantiate the element
        INameScope oldNameScope = _elementContextStack.GetCurrentNameScope();
        elementContext.Instance =
            GetNamespaceHandler(currentElement.NamespaceURI).
                InstantiateElement(this, currentElement.LocalName,
                    currentElement.NamespaceURI, new List<object>());

        // Step 3: Namescope registration
        if (elementContext.Instance is INameScope && oldNameScope != null)
          ((INameScope) elementContext.Instance).RegisterParent(oldNameScope);

        // Step 4: Name registration and check for x:Key (to be done before child objects are built)
        foreach (XmlAttribute attr in remainingAttributes)
        {
          CheckNameOrKey(attr, ref name, ref key);
        }
        foreach (XmlNode node in currentElement.ChildNodes)
        {
          CheckNameOrKey(node, ref name, ref key);
        }
        if (name != null)
          RegisterName(name, elementContext.Instance);

        // Step 5: Members and events in attribute syntax
        foreach (XmlAttribute attr in remainingAttributes)
        {
          HandleMemberOrEventAssignment(attr);
        }

        // Step 6: Member values in member element syntax
        foreach (XmlNode node in currentElement.ChildNodes)
        {
          // Hint: We do not enforce, that object elements, which are used to fill the
          // XAML content member within an object element, must be contiguous
          if (node is XmlElement && node.LocalName.IndexOf('.') != -1) // "." is used as indicator that we have a member declaration
          {
            // Check XML attributes on member element - only x:Uid is permitted
            foreach (XmlAttribute attr in node.Attributes)
            {
              if (attr.NamespaceURI != XAML_NS_URI || attr.LocalName != "Uid")
                throw new XamlParserException("No attributes are allowed on member elements except x:Uid");
              // TODO: Handle x:Uid markup extension
            }
            HandleMemberOrEventAssignment(node);
          }
        }

        // Step 7: Handle child elements
        if (elementContext.Instance is INativeXamlObject)
        { // Implementors of INativeXamlObject will handle their XAML child elements theirselves
          ((INativeXamlObject)elementContext.Instance).HandleChildren(this, currentElement);
        }
        else
        { // Content member value/Children handled by this XAML parser
          object value = ParseValue(currentElement);
          if (value != null)
          {
            IDataDescriptor dd;
            if (elementContext.Instance is IContentEnabled &&
                ((IContentEnabled) elementContext.Instance).FindContentProperty(out dd))
            {
              HandleMemberAssignment(dd, value);
            }
            else if (ReflectionHelper.CheckHandleCollectionAssignment(elementContext.Instance, value))
            { }
            else
              throw new XamlBindingException("Visual element '{0}' doesn't support adding children",
                                             currentElement.Name);
          }
        }

        // Step 8: Initialize
        if (elementContext.Instance is IInitializable)
          ((IInitializable) elementContext.Instance).Initialize(this);

        return elementContext.Instance;
      }
      finally
      {
        _elementContextStack.PopElementContext();
      }
    }

    /// <summary>
    /// Imports a namespace which starts in the XAML namespace definition with
    /// the <c>clr-namespace:</c> prefix. This method will return a handler for
    /// resolving elements and types in the specified clr-namespace.
    /// </summary>
    /// <param name="namespaceURI">Namespace import definition for a clr-namespace in
    /// the long form like <c>clr-namespace:System;assembly=mscorlib</c> or in the
    /// short form like <c>clr-namespace:Media</c>.</param>
    /// <returns>Namespace handler to handle types defined in the specified clr-namespace.</returns>
    protected static INamespaceHandler ImportClrNamespace(string namespaceURI)
    {
      return DefaultNamespaceHandler.createDefaultHandler(namespaceURI);
    }

    /// <summary>
    /// Handles nodes for member or event assignment to the currently processed
    /// element in both attribute syntax and member element syntax.
    /// This method ignores attributes from the <c>x:</c> namespace, which
    /// will be handled in method
    /// <see cref="CheckNameOrKey(XmlNode,ref string,ref string)"/>.
    /// </summary>
    /// <param name="memberDeclarationNode">Node containing a member or
    /// event assignment for the current element. This node can either
    /// be an <see cref="XmlAttribute"/> node (for attribute syntax) or an
    /// <see cref="XmlElement"/> node (for member element syntax).
    /// Nodes in the <c>x:</c> namespace will be ignored.</param>
    protected void HandleMemberOrEventAssignment(XmlNode memberDeclarationNode)
    {
      ElementContextInfo elementContext = _elementContextStack.CurrentElementContext;
      string memberName = memberDeclarationNode.LocalName;
      int i = memberName.IndexOf('.');
      string explicitTargetQualifier = i == -1 ? string.Empty : memberName.Substring(0, i);
      if (explicitTargetQualifier.Length > 0)
        memberName = memberName.Substring(explicitTargetQualifier.Length + 1); // Cut away target element name
      // Check if we have an attached property
      bool isAttached;
      if (explicitTargetQualifier == string.Empty)
        isAttached = false;
      else if (explicitTargetQualifier == elementContext.Element.LocalName)
      { // Qualifier == element name, maybe an attached property
        string namespaceURI = (memberDeclarationNode.NamespaceURI == string.Empty && memberDeclarationNode is XmlAttribute) ?
          _elementContextStack.GetNamespaceOfPrefix(string.Empty) :
          memberDeclarationNode.NamespaceURI;
        isAttached = GetNamespaceHandler(namespaceURI).HasAttachedProperty(
            explicitTargetQualifier, memberName, elementContext.Instance, namespaceURI);
      }
      else
        isAttached = true;
      if (isAttached)
      { // Attached property or event attribute
        // For unprefixed attributes, adopt the namespace from the parent node
        string namespaceURI = (memberDeclarationNode.NamespaceURI == string.Empty && memberDeclarationNode is XmlAttribute) ?
            _elementContextStack.GetNamespaceOfPrefix(string.Empty) :
            memberDeclarationNode.NamespaceURI;
        IDataDescriptor attachedPropertyDD = GetNamespaceHandler(namespaceURI).GetAttachedProperty(
            explicitTargetQualifier, memberName, elementContext.Instance, namespaceURI);
        object value = ParseValue(memberDeclarationNode);
        HandleMemberAssignment(attachedPropertyDD, value);
      }
      else
      { // Local member
        // We have to check if <c>memberDeclarationNode.Prefix == string.Empty</c>, because
        // unprefixed names normally match the default's namespace URI,
        // but here the namespace URI of those unprefixed nodes implicitly
        // belong to the current element.
        if (memberDeclarationNode.Prefix == string.Empty ||
            memberDeclarationNode.NamespaceURI == elementContext.Element.NamespaceURI)
        { // Member or event attribute located in the same namespace as the element
          object value = ParseValue(memberDeclarationNode);
          Type t = elementContext.Instance.GetType();
          // Search Member data descriptor
          IDataDescriptor dd;
          if (ReflectionHelper.FindMemberDescriptor(elementContext.Instance, memberName, out dd))
          { // Member assignment
            if (value != null)
              HandleMemberAssignment(dd, value);
            return;
          }
          EventInfo evt = t.GetEvent(memberName);
          if (evt != null)
          { // Event assignment
            HandleEventAssignment(elementContext.Instance, evt,
                (string) Convert(value, typeof(string)));
            return;
          }
          throw new XamlBindingException("XAML parser: Member '{0}' was not found on type '{1}'",
            memberName, t.Name);
        }
        else if (memberDeclarationNode.NamespaceURI == XAML_NS_URI)
        { // XAML attributes ("x:Attr") will be ignored here - they are evaluated
          // in the code processing the parent instance
        }
        else
          throw new XamlParserException("Attribute '{0}' is not defined in namespace '{1}'",
              memberDeclarationNode.LocalName, memberDeclarationNode.NamespaceURI);
      }
    }

    /// <summary>
    /// Checks the existance of a <c>Name</c> and the <c>x:Name</c> and <c>x:Key</c>
    /// pseudo member nodes in both attribute syntax and member element syntax.
    /// Other nodes from the <c>x:</c> namespace will be rejected by throwing
    /// an exception.
    /// Names given by a <c>x:Name</c> will be automatically assigned to the
    /// current element's "Name" attribute, if it is present.
    /// Except <c>Name</c>, this method ignores "normal" members and events,
    /// which will be handled in method
    /// <see cref="HandleMemberOrEventAssignment"/>.
    /// </summary>
    /// <remarks>
    /// Implementation node: We separate this method from method
    /// <see cref="HandleMemberOrEventAssignment"/>
    /// because both methods handle attributes of different scopes. The <c>x:</c>
    /// attributes are attributes which affect the outside world, as they associate
    /// a name or key to the current element in the outer scope. In contrast to
    /// this, "Normal" members are set on the current element and do not affect
    /// the outer scope.
    /// </remarks>
    /// <param name="memberDeclarationNode">Node containing a <c>x:</c> attribute
    /// for the current element. This node can either
    /// be an <see cref="XmlAttribute"/> node (for attribute syntax) or an
    /// <see cref="XmlElement"/> node (for member element syntax).
    /// This method ignores "normal" member and event attributes.</param>
    /// <param name="name">Will be set if the node to handle is an <c>x:Name</c>.</param>
    /// <param name="key">Will be set if the node to handle is an <c>x:Key</c>.</param>
    protected void CheckNameOrKey(XmlNode memberDeclarationNode, ref string name, ref object key)
    {
      ElementContextInfo elementContext = _elementContextStack.CurrentElementContext;
      // Name
      if ((memberDeclarationNode.Prefix == string.Empty ||
           memberDeclarationNode.NamespaceURI == elementContext.Element.NamespaceURI) &&
           memberDeclarationNode.LocalName == "Name")
      {
        name = Convert(ParseValue(memberDeclarationNode), typeof(string)) as string;
        return;
      }
      if (memberDeclarationNode.NamespaceURI != XAML_NS_URI)
        // Ignore other attributes not located in the x: namespace
        return;
      // x: attributes
      object value = ParseValue(memberDeclarationNode);
      if (memberDeclarationNode.LocalName == "Name") // x:Name
      { // x:Name
        if (name == null)
        {
          name = TypeConverter.Convert(value, typeof(string)) as string;
          // Assign name to "Name" member, if one exists
          IDataDescriptor dd;
          if (ReflectionHelper.FindMemberDescriptor(
              elementContext.Instance, "Name", out dd))
            // Member assignment
            HandleMemberAssignment(dd, value);
        }
        else
          throw new XamlBindingException("Attribute '{0}' was specified multiple times", memberDeclarationNode.Name);
      }
      else if (memberDeclarationNode.LocalName == "Key")
      { // x:Key
        if (key == null)
          key = value;
        else
          throw new XamlBindingException("Attribute '{0}' was specified multiple times", memberDeclarationNode.Name);
      }
      else
        // TODO: Is it correct to reject all usages of x:Attr attributes except x:Key and x:Name?
        throw new XamlParserException("Attribute '{0}' cannot be used here", memberDeclarationNode.Name);
    }

    #region Helper methods

    /// <summary>
    /// Given a root element parsed from a XAML file, this method extracts the root
    /// element, if the given <paramref name="rootElement"/> is an include.
    /// </summary>
    /// <param name="rootElement">Root element parsed from a XAML file.</param>
    /// <returns>Element found in the specified <paramref name="rootElement"/>.</returns>
    protected object UnwrapIncludes(object rootElement)
    {
      if (rootElement is IInclude)
        return UnwrapIncludes(((IInclude) rootElement).Content);
      else
        return rootElement;
    }

    /// <summary>
    /// Returns the implicit key for the specified object. The method will try to cast
    /// <paramref name="o"/> to <see cref="IImplicitKey"/>. If the object doesn't implement
    /// this interface, an exception will be raised.
    /// </summary>
    /// <param name="o">Object whose implicit key should be evaluated.</param>
    /// <returns>Implicit key for <paramref name="o"/>.</returns>
    /// <exception cref="XamlBindingException">If <paramref name="o"/> doesn't implement
    /// <see cref="IImplicitKey"/>.</exception>
    protected static object GetImplicitKey(object o)
    {
      if (o is IImplicitKey)
        return ((IImplicitKey) o).GetImplicitKey();
      else
        throw new XamlBindingException("Object '{0}' doesn't expose an implicit key", o);
    }

    #endregion

    /// <summary>
    /// Parses the contents of an <see cref="XmlNode"/> (not the node itself!).
    /// Parsed will be instances of <see cref="XmlAttribute"/> and <see cref="XmlElement"/>.
    /// In case the node is an <see cref="XmlAttribute"/>, its value will be parsed
    /// and returned. This will normally be a string or an instance of a
    /// markup extension. In case the node is an <see cref="XmlElement"/>,
    /// its children will be instantiatedan returned as an <see cref="IList"/> or
    /// an <see cref="IDictionary"/>, depending on whether the elements have a
    /// <c>x:Key</c> attribute associated.
    /// </summary>
    /// <param name="node">The XML node, an <see cref="XmlAttribute"/> or
    /// an <see cref="XmlElement"/>, whose contents should be parsed.</param>
    /// <returns>Parsed value. This is either a string, a markup extension
    /// instance or an IList<object>.</returns>
    protected object ParseValue(XmlNode node)
    {
      if (node is XmlAttribute) // Member attribute
        return ParseValue(node.Value);
      else if (node is XmlElement) // Explicit or implicit (Content) member element
      {
        // Return value's type will vary depending on parameter 'needDict'
        IList<object> resultList = new List<object>();
        IDictionary<object, object> resultDict = new Dictionary<object, object>();

        foreach (XmlNode childNode in node.ChildNodes)
        {
          if (childNode is XmlElement)
          {
            if (childNode.LocalName.IndexOf('.') != -1) // "." is used as indicator that we have a member declaration
            { // Ignore member assignments here - we focus on our real children
              continue;
            }
            else
            {
              object key;
              object value = Instantiate((XmlElement)childNode, out key);
              // Handle the case if a markup extension was instantiated as a child
              if (value is IEvaluableMarkupExtension)
                value = ((IEvaluableMarkupExtension)value).Evaluate(this);
              else if (value is IInclude)
                value = ((IInclude) value).Content;
              if (key == null)
                resultList.Add(value);
              else
                try
                {
                  resultDict.Add(key, value);
                }
                catch (ArgumentException e)
                {
                  throw new XamlParserException("Duplicate key '{0}'", e, key);
                }
              // If a key was provided, register the new element in the current context.
              // This is necessary if the new element should be accessed before all children
              // here created are added to their target collection.
              if (key != null)
                _elementContextStack.CurrentElementContext.AddKeyedElement(key, value);
            }
          }
          else if (childNode is XmlText || childNode is XmlCDataSection) // Ignore other XmlCharacterData nodes
          {
            resultList.Add(((XmlCharacterData)childNode).Data);
          }
        }
        if (resultList.Count > 0 && resultDict.Count > 0)
        { // Try to add implicit keys for resources which don't have one
          foreach (object o in resultList)
          {
            try
            {
              resultDict.Add(GetImplicitKey(o), o);
            }
            catch
            {
              throw new XamlBindingException("Xaml parser parsing Element '{0}': Child elements containing x:Key attributes cannot be mixed with child elements without x:Key attribute", node.Name);
            }
          }
          resultList.Clear();
        }

        if (resultDict.Count > 0)
          return resultDict;
        else
          return resultList.Count == 0 ? null : (
            resultList.Count == 1 ? resultList[0] : resultList);
      }
      else
        return null;
    }

    protected object ParseValue(string str)
    {
      str = str.Trim();
      if (str.StartsWith("{}")) // {} = escape sequence
        return str.Substring(2);
      else if (str.StartsWith("{"))
      { // Object instantiation in attribute value syntax
        if (!str.EndsWith("}"))
          throw new XamlParserException("Object instantiation expression '{0}' must be terminated by a '}}' character", str);
        string expr = str.Substring(1, str.Length - 2).Trim(); // Extract the expression
        ElementContextInfo elementContext = _elementContextStack.PushElementContext(expr);
        try
        {
          // Step 1: Process namespace declarations (doesn't apply here)
          INameScope oldNameScope = _elementContextStack.GetCurrentNameScope();
          string extensionName;
          IList<KeyValuePair<string, string>> parameters;
          bool namedParams;
          AttributeValueInstantiationParser.ParseInstantiationExpression(
              expr, out extensionName, out parameters, out namedParams);
          string namespaceURI;
          LookupNamespace(extensionName, out extensionName, out namespaceURI);
          if (namedParams)
          { // Parameters given in a Name=Value syntax
            // Step 2: Instantiate the element
            elementContext.Instance = GetNamespaceHandler(namespaceURI).
                InstantiateElement(this, extensionName, namespaceURI,
                                   new List<object>()); // Invoke default constructor
            // Step 3: Namescope registration
            if (elementContext.Instance is INameScope && oldNameScope != null)
              oldNameScope.RegisterParent(oldNameScope);

            // Step 5: Members and events in attribute syntax

            // We only process the given parameters and assign their values to the
            // target members. Property value inheritance, for example the
            // inheritance of a "Context" member for bindings, has to be
            // implemented on the visual's element class hierarchy.
            foreach (KeyValuePair<string, string> parameter in parameters) // Assign value to each specified member
            {
              string memberName = parameter.Key;
              IDataDescriptor dd;
              if (ReflectionHelper.FindMemberDescriptor(elementContext.Instance, memberName, out dd))
              {
                object paramVal = ParseValue(parameter.Value);
                HandleMemberAssignment(dd, paramVal);
                // Step 4: Name registration
                if (memberName == "Name")
                {
                  string value = Convert(paramVal, typeof(string)) as string;
                  RegisterName(value, elementContext.Instance);
                }
              }
              else
                throw new XamlBindingException("XAML parser: Member '{0}' was not found on element type '{1}'",
                                               memberName, elementContext.Instance.GetType().Name);
            }
          }
          else
          { // Parameters given as constructor parameters
            // Step 2: Instantiate the element
            IList<object> flatParams = new List<object>();
            foreach (string param in AttributeValueInstantiationParser.ExtractParameterValues(parameters))
            {
              object value = ParseValue(param);
              if (value is IEvaluableMarkupExtension)
                value = ((IEvaluableMarkupExtension) value).Evaluate(this);
              flatParams.Add(value);
            }
            elementContext.Instance = GetNamespaceHandler(namespaceURI).
                InstantiateElement(this, extensionName, namespaceURI, flatParams);
            // Step 3: Namescope registration
            if (elementContext.Instance is INameScope && oldNameScope != null)
              oldNameScope.RegisterParent(oldNameScope);

            // Step 5: Members and events in attribute syntax (doesn't apply here)
          }
          // Step 6: Member values in member element syntax (doesn't apply here)
          // Step 7: Handle child elements (doesn't apply here)
          // Step 8: Initialize
          if (elementContext.Instance is IInitializable)
            ((IInitializable) elementContext.Instance).Initialize(this);
          return elementContext.Instance;
        }
        finally
        {
          _elementContextStack.PopElementContext();
        }
      }
      else return str;
    }

    /// <summary>
    /// Will create an event handler association for the current element.
    /// </summary>
    /// <remarks>
    /// In contrast to method
    /// <see cref="HandleMemberAssignment"/>,
    /// this method can only be used for the current element, which is part of the
    /// visual tree. We do not support adding event handlers to markup extension
    /// instances.
    /// </remarks>
    /// <param name="obj">Object which defines the event to assign the event
    /// handler specified by <paramref name="value"/> to.</param>
    /// <param name="evt">Event which is defined on the class of
    /// <paramref name="obj"/>.</param>
    /// <param name="value">Name of the event handler to assign to the specified
    /// event.</param>
    protected void HandleEventAssignment(object obj, EventInfo evt, string value)
    {
      if (_getEventHandler == null)
        throw new XamlBindingException("Delegat 'GetEventHandler' ist nicht verknüpft");
      Delegate dlgt = _getEventHandler(this, evt.EventHandlerType.GetMethod("Invoke"), value);
      try
      {
        evt.AddEventHandler(obj, dlgt);
      }
      catch (Exception e)
      {
        throw new XamlBindingException("Error assigning event handler", e);
      }
    }

    /// <summary>
    /// Registers an instance which was created for a XAML element with its name.
    /// </summary>
    /// <param name="name">Name to register the instance. This is the value of a
    /// <c>x:Name</c> attribute of the XAML element corresponding to the instance.</param>
    /// <param name="instance">The instance to be registered. This is the instance which was
    /// instantiated for the XAML element which carries the specified <c>x:Name</c> attriute.</param>
    protected void RegisterName(string name, object instance)
    {
      try
      {
        INameScope currentNameScope = _elementContextStack.GetCurrentNameScope();
        if (currentNameScope != null)
          currentNameScope.RegisterName(name, instance);
      }
      catch (ArgumentException e)
      {
        throw new XamlParserException("Duplicate name '{0}'", e, name);
      }
    }

    protected object Convert(object val, Type targetType)
    {
      try
      {
        object result;
        if (TypeConverter.Convert(val, targetType, out result))
          return result;
        else
          throw new XamlBindingException("Could not convert object '{0}' to type '{1}'", val, targetType.Name);

      }
      catch (ArgumentException)
      {
        throw new XamlBindingException("Could not convert object '{0}' to type '{1}'", val, targetType.Name);
      }
    }

    #endregion

    #region IParserContext implementation

    /// <see cref="IParserContext.ContextStack"/>
    public ElementContextStack ContextStack
    { get { return _elementContextStack; } }

    /// <see cref="IParserContext.LookupNamespace(string,out string,out string)"/>
    public void LookupNamespace(string elementName, out string localName, out string namespaceURI)
    {
      string prefix;
      int i = elementName.IndexOf(':');
      if (i == -1)
      {
        prefix = string.Empty;
        localName = elementName;
      }
      else
      {
        prefix = elementName.Substring(0, i);
        localName = elementName.Substring(i+1);
      }
      namespaceURI = _elementContextStack.GetNamespaceOfPrefix(prefix);
    }

    /// <see cref="IParserContext.GetNamespaceHandler(string)"/>
    public INamespaceHandler GetNamespaceHandler(string namespaceURI)
    {
      return _elementContextStack.GetNamespaceHandler(namespaceURI);
    }

    /// <see cref="IParserContext.HandleMemberAssignment"/>
    public void HandleMemberAssignment(IDataDescriptor dd, object value)
    {
      if (value is IBinding)
      {
        IBinding binding = (IBinding) value;
        binding.Prepare(this, dd);
        binding.Activate();
        return;
      }
      else if (value is IEvaluableMarkupExtension)
      {
        IEvaluableMarkupExtension me = (IEvaluableMarkupExtension) value;
        value = me.Evaluate(this);
      }

      if (dd.SupportsWrite &&
          (value == null || dd.DataType.IsAssignableFrom(value.GetType())))
        dd.Value = value;
      else if (ReflectionHelper.CheckHandleCollectionAssignment(dd.Value, value))
      { }
      else
        dd.Value = Convert(value, dd.DataType);
    }

    #endregion
  }
}
