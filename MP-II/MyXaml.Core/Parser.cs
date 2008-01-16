/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

/*
 * There is an ongoing problem with attribute initialization vs. adding the child to its parent's collection.
 * Currently, attributes are processed first.  If we don't do this, for example, the Size property of the Form
 * does not get initialized, which hoses up child controls that Anchor right and/or bottom.
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

using Clifton.Tools.Strings;
//using Vts.UnitTest;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MyXaml.Core.Exceptions;

namespace MyXaml.Core
{
  /// <summary>
  /// The MyXaml object graph parser;
  /// </summary>
  public class Parser : IDisposable
  {
    /// <summary>
    /// AddToCollectionHelper delegate.
    /// </summary>
    public delegate void AddToCollectionDlgt(object parser, AddToCollectionEventArgs e);

    /// <summary>
    /// Delegate for InstantiateBegin and InstantiateEnd events.
    /// </summary>
    public delegate void InstantiateDlgt(object parser, InstantiateEventArgs e);

    public delegate void PropertyDeclarationTestDlgt(object parser, PropertyDeclarationTestEventArgs e);

    public delegate void InstantiatePropertyDeclarationDlgt(object parser, InstantiatePropertyDeclarationEventArgs e);

    public delegate void InstantiateClassDlgt(object parser, InstantiateClassEventArgs e);

    public delegate void CustomPropertyHandlerDlgt(object parser, CustomPropertyEventArgs e);

    public delegate void CustomTypeConverterDlgt(object parser, CustomTypeEventArgs e);

    public delegate object GetResourceDlgt(object parser, object obj, string resourceName);
    public delegate object GetBindingDlgt(object parser, object obj, string resourceName, PropertyInfo info);
    public delegate void SetContentDlg(object parser, object obj, object content);
    public delegate void ImportNamespaceDlgt(object parser, object obj, string nameSpace);

    /// <summary>
    /// Event is raised when the object graph implements adding instances to
    /// an ICollection implementer.
    /// </summary>
    public event AddToCollectionDlgt AddToCollection;

    /// <summary>
    /// Event is raised when the object is first instantiated.
    /// </summary>
    public event InstantiateDlgt InstantiateBegin;

    /// <summary>
    /// Event is raised when all child objects and attributes have been initialized,
    /// at the end of object instantiation.
    /// </summary>
    public event InstantiateDlgt InstantiateEnd;

    public event PropertyDeclarationTestDlgt PropertyDeclarationTest;

    public event InstantiatePropertyDeclarationDlgt InstantiatePropertyDeclaration;

    public event InstantiateClassDlgt InstantiateFromQName;

    public event CustomPropertyHandlerDlgt CustomPropertyHandler;

    public event CustomTypeConverterDlgt CustomTypeConvertor;

    public event GetResourceDlgt OnGetResource;
    public event GetBindingDlgt OnGetBinding;
    public event SetContentDlg OnSetContent;
    public event ImportNamespaceDlgt OnImportNameSpace;

    protected string currentFile;

    /// <summary>
    /// The document being processed.
    /// </summary>
    protected XmlDocument xmlDocument;

    /// <summary>
    /// Maintains the mapping of xml prefixes to namespaces.
    /// </summary>
    protected Hashtable prefixToNamespaceMap;

    /// <summary>
    /// Maintains the mapping of names and the class instance associated with
    /// the name.
    /// </summary>
    protected Hashtable nameToInstanceMap;

    /// <summary>
    /// The list of properties that could not be bound to referenced instances
    /// during the first pass of the parser.
    /// </summary>
    protected ArrayList lateBindings;

    /// <summary>
    /// The stack maintained during object graph instantiation.
    /// </summary>
    protected Stack objectStack;

    /// <summary>
    /// Implements a static list of extender Types.
    /// </summary>
    protected static ArrayList extenders = new ArrayList();

    private static string[] corePrefixes = new string[] { "def", "undef", "ref" };

    protected ArrayList specialPrefixes;

    /// <summary>
    /// Manages the extender instances.
    /// </summary>
    protected ArrayList extenderInstances;

    protected object target;

    public string CurrentFile
    {
      get { return currentFile; }
    }

    public object AutoInitTarget
    {
      get { return target; }
      set { target = value; }
    }

    protected bool ignoreMissingFields;

    /// <summary>
    /// Gets/sets ignoreMissingFields
    /// </summary>
    public bool IgnoreMissingFields
    {
      get { return ignoreMissingFields; }
      set { ignoreMissingFields = value; }
    }

    public Hashtable References
    {
      get { return nameToInstanceMap; }
    }

    /// <summary>
    /// The object graph parser.
    /// </summary>
    public Parser()
    {
      Trace.WriteLine("!!ut=*Unit Test Info");
      specialPrefixes = new ArrayList();
      specialPrefixes.AddRange(corePrefixes);
      extenderInstances = new ArrayList();
      nameToInstanceMap = new Hashtable();									// Instantiate the hashtable that manages the collection of instances with a def: prefix.
      objectStack = new Stack();											// The object stack is used for internal peeking at instances in the hierarchy
      lateBindings = new ArrayList();										// Keeps track of late binding MxBinding objects.
      prefixToNamespaceMap = new Hashtable();								// Tracks xml prefixes to namespace names.

      AddReference("MyXamlInstanceMap", nameToInstanceMap);				// Allows the parser to reference it's own instance map.  Useful in workflows, for example.
      AddReference("MyXamlDefs", nameToInstanceMap);						// For backward compatibility.
      AddReference("MyXamlParser", this);									// Add ourselves to the reference list.  Also useful in workflows, for example.

      Trace.WriteLine("CLR Version: " + Environment.Version.ToString());

      foreach (Type t in extenders)
      {
        IMyXamlExtender extender = Activator.CreateInstance(t) as IMyXamlExtender;
        if (extender == null)
        {
          throw (new ExtenderActivationException("Extender cannot be instantiated.  Does it implement IMyXamlExtender?"));
        }
        extender.Register(this);
        extenderInstances.Add(extender);
      }
    }

    /// <summary>
    /// Adds an extender in the specified assembly.
    /// </summary>
    /// <param name="assemblyName">The assembly implementing the extender.</param>
    /// <param name="namespaceName">The namespace containing the extender.</param>
    /// <param name="className">The extender class that implements IMyXamlExtender.</param>
    public static void AddExtender(string assemblyName, string namespaceName, string className)
    {
      Assembly assembly = Assembly.Load(assemblyName);
      Type t = assembly.GetType(namespaceName + "." + className);
      if (t == null)
      {
        throw (new ExtenderClassNotFoundException("Extender does not implement the " + namespaceName + "." + className + " class."));
      }
      extenders.Add(t);
    }

    public static void RemoveExtender(string assemblyName, string namespaceName, string className)
    {
      Assembly assembly = Assembly.Load(assemblyName);
      Type t = assembly.GetType(namespaceName + "." + className);
      if (t == null)
      {
        throw (new ExtenderClassNotFoundException("Extender does not implement the " + namespaceName + "." + className + " class."));
      }
      extenders.Remove(t);
    }

    public void AddSpecialPrefix(string prefix)
    {
      specialPrefixes.Add(prefix);
    }

    public void RemoveSpecialPrefix(string prefix)
    {
      specialPrefixes.Remove(prefix);
    }

    /// <summary>
    /// Disposes of the parser.
    /// </summary>
    public void Dispose()
    {
      foreach (IMyXamlExtender extender in extenderInstances)
      {
        extender.Unregister(this);
      }
    }

    /// <summary>
    /// Copies the references from another parser to this parser.
    /// </summary>
    /// <param name="p">The source parser.</param>
    /// <exception cref="ArgumentException">Thrown when source parser is null.</exception>
    public void CopyReferencesFrom(Parser p)
    {
      if (p == null)
      {
        throw (new ArgumentException("The source parser is null."));
      }

      foreach (DictionaryEntry entry in p.nameToInstanceMap)				// For each entry in the source parser's reference list...
      {
        string name = entry.Key.ToString();

        if ((name != "MyXamlInstanceMap") &&							// Ignore internal references
          (name != "ParserParser"))
        {
          AddReference(name, entry.Value);							// Copy the reference to this parser.
        }
      }
    }

    /// <summary>
    /// Adds a named reference.
    /// </summary>
    /// <param name="name">The reference name.</param>
    /// <param name="reference">The reference instance.</param>
    /// <exception cref="ArgumentException">Thrown when name is null.</exception>
    /// <exception cref="ReferenceExistsException">Thrown when the name already exists in the collection.</exception>
    public void AddReference(string name, object reference)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      if (nameToInstanceMap.Contains(name))
      {
        throw (new ReferenceExistsException("Reference " + name + " already exists."));
      }

      nameToInstanceMap[name] = reference;
    }

    public void AddOrUpdateReferences(Hashtable refs)
    {
      foreach (DictionaryEntry entry in refs)
      {
        nameToInstanceMap[entry.Key] = entry.Value;
      }
    }

    public void AddOrUpdateReference(string name, object reference)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      nameToInstanceMap[name] = reference;
    }

    /// <summary>
    /// Updates an existing reference to the reference collection.
    /// </summary>
    /// <param name="name">The reference name.</param>
    /// <param name="reference">The reference.</param>
    public void UpdateReference(string name, object reference)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      if (!nameToInstanceMap.Contains(name))
      {
        throw (new NoReferenceException("Reference " + name + " does not exists."));
      }

      nameToInstanceMap[name] = reference;
    }

    /// <summary>
    /// Removes a named reference.
    /// </summary>
    /// <param name="name">The reference name.</param>
    /// <exception cref="ArgumentException">Thrown when name is null.</exception>
    public void RemoveReference(string name)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      if (ContainsReference(name))
      {
        nameToInstanceMap.Remove(name);
      }
      else
      {
        throw (new NoReferenceException("Reference " + name + " does not exist."));
      }
    }

    /// <summary>
    /// Tests if the named reference exists in the reference collection.
    /// </summary>
    /// <param name="name">The reference name.</param>
    /// <returns>True if the reference exists.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null.</exception>
    public bool ContainsReference(string name)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      return nameToInstanceMap.Contains(name);
    }

    /// <summary>
    /// Gets the instance associated with the reference name.
    /// </summary>
    /// <param name="name">The reference name.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null.</exception>
    /// <exception cref="NoReferenceException">Thrown when the named reference does not exist.</exception>
    public object GetReference(string name)
    {
      if (name == null)
      {
        throw (new ArgumentException("Name cannot be null."));
      }
      if (!ContainsReference(name))
      {
        throw (new NoReferenceException("The reference " + name + " does not exist in the collection."));
      }

      return nameToInstanceMap[name];
    }

    /// <summary>
    /// Gets the property value of an instance.  This method traverses the
    /// properties/fields of the instance and child instances, returning the value
    /// associated with the final property or field.
    /// </summary>
    /// <param name="refName">The reference name.</param>
    /// <returns>The value of the reference or the property/field of the complex reference.</returns>
    /// <exception cref="NoReferenceException">Thrown when the named reference does not exist.</exception>
    public object ResolveValue(string refName)
    {
      object ret = null;
      if (refName.IndexOf('.') != -1)										// If this is a complex reference...
      {
        string objName = StringHelpers.LeftOf(refName, '.');				// Get the instance name.
        if (!ContainsReference(objName))								// If not in the reference list, throw an exception.
        {
          throw (new NoReferenceException("The reference " + refName + " does not exist in the collection."));
        }
        object obj = nameToInstanceMap[objName];							// Get the instance associated with the name.
        while (refName.IndexOf('.') != -1)								// Drill into the property or field.
        {
          refName = StringHelpers.RightOf(refName, '.');				// Get the remainder of the complex reference.
          string propName = StringHelpers.LeftOf(refName, '.');			// Get the first property/field name.
          PropertyInfo pi = GetPropertyInfo(obj, propName);			// Get the PropertyInfo for the property/field name.
          if (pi == null)												// If null, then test if this is a field.
          {
            FieldInfo fi = GetFieldInfo(obj, propName);                 // Get the FieldInfo for the assumed field name.

            if (fi != null)											// If not null, then get the value of the field.
            {
              obj = fi.GetValue(obj);								// Get the value of the field.
            }
            else
            {
              throw (new ImproperComplexReferenceException("The reference " + refName + " cannot be resolved."));
            }
          }
          else
          {
            obj = pi.GetValue(obj, null);								// Get the value of the property.
          }
        }																// The resulting "value" becomes the instance to drill into next, if there are more property/field descriptors.
        ret = obj;														// All done.  The resulting "value" is what we return.
      }
      else
      {
        ret = GetReference(refName);										// Simple reference resolution.
      }
      return ret;
    }

    public void InitializeFields(object target)
    {
      InitializeFields(target, ignoreMissingFields);
    }

    /// <summary>
    /// Searches the supplied target instance for MyXamlAutoInitialize attributes
    /// applied to fields in that target.  If found, the field is initialized
    /// with the value found in the reference collection, if it exists.
    /// </summary>
    /// <param name="target">The target instance whose fields will be searched.</param>
    public void InitializeFields(object target, bool ignoreMissingFields)
    {
      if (target == null)
      {
        throw (new ArgumentException("Can't initialize fields on a null target: " + target));
      }

      Type t = target.GetType();											// Get the type for this target.

      foreach (FieldInfo fi in GetFieldInfos(t))
      {
        string markupName = fi.Name;										// Assume that we're going to use the field name for the reference.

        foreach (Attribute attr in fi.GetCustomAttributes(false))		// For each attribute associated with the field...
        {
          if (attr is MyXamlAutoInitializeAttribute)					// If it's the auto-initialize attribute...
          {
            string alias = ((MyXamlAutoInitializeAttribute)attr).Alias;		// Get the override name.

            if (alias != null)										// If not null, reassign the reference name.
            {
              markupName = alias;
            }

            if (ContainsReference(markupName))						// The reference name must exist.
            {
              object obj = GetReference(markupName);				// Get the reference.
              try
              {
                fi.SetValue(target, obj);						// Try to assign it to the field.
              }
              catch
              {
                throw (new AutoInitializeException("Unable to auto-initialize " + fi.Name));
              }
            }
            else
            {
              if (!ignoreMissingFields)
              {
                throw (new NoReferenceException("The field " + fi.Name + " does not have an associated entry in the reference collection."));
              }
            }
            break;													// All done with the attributes for this field.
          }
        }
      }
    }

    public object Instantiate(string filename, string tagName)
    {
      return Instantiate(filename, tagName, false);
    }

    /// <summary>
    /// Instantiates the object graph from a file and the specified starting
    /// root child tag.  See XmlDocument.Load for exceptions that may be thrown.
    /// </summary>
    /// <param name="filename">The MyXaml file.</param>
    /// <param name="tagName">The root child tag to instantiate, or "*" for the
    /// first root child encountered.</param>
    /// <param name="ignoreMissingFields">Ignores missing fields in the target during InitializeFields call.</param>
    /// <returns>The instance of the topmost object in the graph.</returns>
    /// <exception cref="ArgumentException">Thrown when filename or tagName is null.</exception>
    /// <exception cref="UnknownTypeException">Thrown when there is no corresponding type to instantiate.</exception>
    /// <exception cref="InstantiationException">Thrown when a class cannot be instantiated (no default constructor).</exception>
    public object Instantiate(string filename, string tagName, bool ignoreMissingFields)
    {
      if (filename == null)
      {
        throw (new ArgumentException("Filename is null."));
      }
      if (tagName == null)
      {
        throw (new ArgumentException("tagName is null."));
      }
      XmlDocument doc = new XmlDocument();									// Create an XmlDocument instance.
      try
      {
        doc.Load(filename);												// Load from file.
      }
      catch (Exception e)
      {
        Trace.WriteLine(e.Message + "\r\n" + e.InnerException);
        throw (e);
      }

      currentFile = filename;

      object obj = Instantiate(doc, tagName, ignoreMissingFields);			// Instantiate using the XmlDocument.
      return obj;
    }

    public object InstantiateFromString(string xml, string tagName)
    {
      return InstantiateFromString(xml, tagName, false);
    }

    /// <summary>
    /// Instantiates the object graph from an xml string and the specified starting
    /// root child tag.  See XmlDocument.LoadXml for exceptions that may be thrown.
    /// </summary>
    /// <param name="xml">The xml string.</param>
    /// <param name="tagName">The root child tag to instantiate, or "*" for the
    /// first root child encountered.</param>
    /// <returns>The instance of the topmost object in the graph.</returns>
    /// <exception cref="XmlException">Thrown when an error exists in the xml.</exception>
    /// <exception cref="ArgumentException">Thrown when xml or tagName is null.</exception>
    public object InstantiateFromString(string xml, string tagName, bool ignoreMissingFields)
    {
      if (xml == null)
      {
        throw (new ArgumentException("xml is null."));
      }
      if (tagName == null)
      {
        throw (new ArgumentException("tagName is null."));
      }

      XmlDocument doc = new XmlDocument();									// Create an XmlDocument instance.
      doc.LoadXml(xml);													// Load from string.
      object obj = Instantiate(doc, tagName, ignoreMissingFields);			// Instantiate using the XmlDocument.
      return obj;
    }

    /// <summary>
    /// Instantiate the object graph given the XmlDocument.
    /// </summary>
    /// <param name="doc">The XmlDocument instance.</param>
    /// <param name="tagName">The tag for the desired xml root child that 
    /// specifies the object graph to instantiate.</param>
    /// <returns>The instance of the topmost object in the graph.</returns>
    protected object Instantiate(XmlDocument doc, string tagName, bool ignoreMissingFields)
    {
      this.ignoreMissingFields = ignoreMissingFields;
      object obj = null;
      xmlDocument = doc;													// Assign to the class instance, so others methods can reference the document.

      InitializeNamespaceList();											// Get the fully qualified namespace names associated with the xml namespaces.

      XmlNode node;														// Holds the node matching the tagName.
      obj = InstantiateRoot(tagName, out node);								// Instantiate the root object in the object graph.
      InstantiateChildren(obj, node);										// Instantiate its children.
      InitializeFields(obj);												// Do any field initializations on the root instance.

      if (target != null)
      {
        InitializeFields(target);
      }
      return obj;
    }

    /// <summary>
    /// Initialize the prefix to namespace map by scanning and parsing
    /// the xmlns attributes on the root element.
    /// </summary>
    protected void InitializeNamespaceList()
    {
      XmlElement rootElement = xmlDocument.DocumentElement;					// Get the root element.

      foreach (XmlAttribute attr in rootElement.Attributes)				// For each attribute in the root element...
      {
        if (!specialPrefixes.Contains(attr.LocalName))					// ignore special prefixes like "ref" and "def"
        {
          // add a namespace with a specified prefix
          string qName = GetFullName(attr.Value);						// Get the fully qualified .NET namespace name.
          if (attr.Prefix == String.Empty)								// If there is no prefix...
          {
            if (prefixToNamespaceMap.Contains(String.Empty))
            {
              if ((string)prefixToNamespaceMap[String.Empty] != qName)
              {
                throw new NamespaceMappingChangedException("Cannot change namespace mapping when parsing a file with an existing parser.");
              }
            }
            else
            {
              prefixToNamespaceMap.Add(String.Empty, qName);			// Then this is the default namespace.
            }
          }
          else
          {
            if (prefixToNamespaceMap.Contains(attr.LocalName))
            {
              if ((string)prefixToNamespaceMap[attr.LocalName] != qName)
              {
                throw new NamespaceMappingChangedException("Cannot change namespace mapping when parsing a file with an existing parser.");
              }
            }
            else
            {
              prefixToNamespaceMap.Add(attr.LocalName, qName);		// Otherwise, get the prefix as the LocalName.
            }
          }
        }
      }
    }

    /// <summary>
    /// Gets the assembly qualified name for a namespace.  The namespace may
    /// be of the short form, only the namespace name, a medium format, which
    /// specifies both the namespace and assembly name, and a long format, in
    /// which culture, version, and public key token are also provided.
    /// </summary>
    /// <param name="name">The namespace.</param>
    /// <returns>The assembly qualified name for the namespace.</returns>
    protected string GetFullName(string name)
    {
      Assembly assembly;
      string qName = name;													// The default is that name specifies a fully qualified namespace.

      if (name.IndexOf(',') == -1)										// If only the namespace is specified...
      {
        try
        {
          assembly = Assembly.Load(name);							// Get the assembly from the partial name.
        }
        catch
        {
          throw (new MissingAssemblyException("Can't locate assembly for " + name));
        }
        if (assembly == null)
        {
          throw (new MissingAssemblyException("Can't locate assembly for " + name));
        }
        qName = name + ", " + assembly.FullName;								// Prepend the namespace.
      }
      else if (StringHelpers.Count(name, ',') == 1)						// If the format is "namespace, assembly"...
      {
        string namespaceName = StringHelpers.LeftOf(name, ',').Trim();	// Get the namespace name.
        string assemblyName = StringHelpers.RightOf(name, ',').Trim();	// Get the assembly name.

        try
        {
          assembly = Assembly.Load(assemblyName);					// Get the assembly.
        }
        catch
        {
          throw (new MissingAssemblyException("Can't locate assembly for " + name));
        }
        qName = namespaceName + ", " + assembly.FullName;						// Prepend the namespace name.
      }
      else if (StringHelpers.Count(name, ',') != 4)
      {
        throw (new ImproperNamespaceFormatException("The namespace " + name + " is improperly formed."));
      }
      return qName;
    }

    /// <summary>
    /// Get the fully qualified namespace given the prefix.  This method is
    /// guaranteed to succeed because the xml parser will have detected prefixes
    /// that do not have a namespace definition.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <returns>The fully qualified namespace.</returns>
    public string Namespace(string prefix)
    {
      string ret = (string)prefixToNamespaceMap[prefix];
      return ret;
    }

    /// <summary>
    /// Instantiate the top class in the object graph.
    /// </summary>
    /// <param name="tagName">The tag name with a corresponding Name attribute
    /// in the xml.</param>
    /// <param name="objectNode">The XmlNode describing the object graph root.</param>
    /// <returns>The object graph root instance.</returns>
    protected object InstantiateRoot(string tagName, out XmlNode objectNode)
    {
      objectNode = null;													// Assume no XmlNode for the root of the object graph.
      XmlAttribute nameAttr = null;											// The attribute corresponding to "Name" in the markup.
      object instance = null;												// Assume no instance.
      bool done = false;													// Assume that we aren't done processing the xml tree.

      XmlElement topElement = xmlDocument.DocumentElement;					// Get the root XmlElement

      if (topElement.ChildNodes.Count == 0)
      {
        throw (new MissingGraphException("The markup has no object graph."));
      }

      if (tagName == "*")													// If a wildcard is being used...
      {
        objectNode = topElement.ChildNodes[0];							// just get the first child.

        foreach (XmlAttribute attr in objectNode.Attributes)			// Check each attribute...
        {
          if (attr.LocalName == "Name")								// Ignore def and ref prefixes, looking for a Name attribute...
          {
            nameAttr = attr;										// This is the name attribute.
            break;
          }
        }
      }
      else
      {
        foreach (XmlNode node in topElement.ChildNodes)					// For each child in the root..
        {
          if (node.Attributes != null)								// If it has attributes...
          {
            foreach (XmlAttribute attr in node.Attributes)			// Check each attribute...
            {
              if (attr.LocalName == "Name")							// Ignore def and ref prefixes, looking for a Name attribute...
              {
                if (attr.Value == tagName)						// If it matches the desired tag or the tag is a wildcard...
                {
                  objectNode = node;							// Then this node is the root node for the object graph.
                  nameAttr = attr;								// This is the name attribute.
                  done = true;									// And we're all done searching the xml.
                  break;
                }
              }
            }
            if (done)
            {
              break;
            }
          }
        }
      }

      if (objectNode == null)
      {
        throw (new GraphNotFoundException("The graph for " + tagName + " could not be found."));
      }

      if ((nameAttr != null) && (nameAttr.Prefix == "ref"))				// If the root of the object graph is a reference to an existing object...
      {
        string refName = nameAttr.Value;									// Get the reference name.
        if (!ContainsReference(refName))
        {
          throw (new NoReferenceException("The reference " + refName + " does not exist in the reference collection."));
        }
        instance = GetReference(refName);									// Get the instance.
      }
      else
      {
        string className = NameMangler(objectNode.LocalName);
        string classNamespace = Namespace(objectNode.Prefix);				// Get the namespace associated with the prefix.
        instance = InstantiateClass(classNamespace,
          className,
          objectNode);												// Instantiate the class.
      }
      return instance;
    }

    /// <summary>
    /// Instantiates all the children in the root of the object graph.
    /// </summary>
    /// <param name="instance">The root instance.</param>
    /// <param name="node">The root XmlNode.</param>
    protected void InstantiateChildren(object instance, XmlNode node)
    {
      ProcessInstance(instance, node, false, null, false, null);			// Recurse through the instance.

      foreach (MxBinding binding in lateBindings)							// Do any late property assignments for this instance.
      {
        binding.Bind(this);
      }
    }

    /// <summary>
    /// Process the current instance--it's child collections, it's attributes.
    /// </summary>
    /// <param name="instance">The class instance</param>
    /// <param name="element">The XmlNode describing this instance and its attributes</param>
    /// <param name="isPropertyDeclaration">Flag indicating that the node is a property declaration rather than an instance.</param>
    /// <param name="propertyName">The name of the property if the node is a property declaration.</param>
    /// <param name="attributesProcessed">Flag indicating that attributes have been processed (for structs).</param>
    /// <param name="returnType">The _ReturnType XmlAttribute, or none if this attribute does not exist for the element.</param>
    protected void ProcessInstance(object instance, XmlNode element, bool isPropertyDeclaration, string propertyName, bool attributesProcessed, XmlAttribute returnType)
    {
      // Push the instances.
      if (!isPropertyDeclaration)
      {
        OnBeginInstantiate(instance);
        objectStack.Push(new ObjectInfo(instance));						// Only push an instance.
      }
      else
      {
        ObjectInfo oi = (ObjectInfo)objectStack.Peek();					// Get the current instance.
        oi.HasPropertyDeclaration = true;									// Set the flag indicating that the instance has a property declaration.
        oi.PropertyName = propertyName;									// Preserve the property name.
        oi.ReturnType = returnType;
      }

      // Call BeginInit() for ISupportInitialize implementors.
      if ((!isPropertyDeclaration) &&									// For instances that support ISupportInitialize...
         (instance is ISupportInitialize))
      {
        ((ISupportInitialize)instance).BeginInit();						// call the BeginInit() method.
      }

      // Save a reference if there's a def: tag
      // MTC - 03/04/06: Remove the reference if the tag is "undef"
      if (element.Attributes != null)										// If the element has attributes...
      {
        foreach (XmlAttribute attr in element.Attributes)				// Check each attribute...
        {
          switch (attr.Prefix.ToLower())
          {
            case "def":
              AddReference(attr.Value, instance);						// Add the instance to the reference collection.
              break;													// Assume only one "def" prefix.
            case "undef":
              RemoveReference(attr.Value);
              break;
          }
        }
      }

      // MTC - 06/28/06 - Changed yet again, to process attributes first.
      // For example, the form's ClientSize has to be set before controls are added, otherwise
      // if the control is anchored to the right and bottom, the control position is all screwed up.
      if (!attributesProcessed)
      {
        ProcessAttributes(instance, element);
        SetText(instance, element);										// Set the text for the new instance.
      }

      // Process collections before attribute assignments.			
      ProcessCollections(instance, element);								// Process the instance's children.

      // Process attributes.
      //			if (instance.GetType().IsClass)										// Don't process attributes for structs.  These are already processed.
      // MTC - 03/04/06: Test if attributes have been processed instead
      //if (!attributesProcessed)
      //{
      //    ProcessAttributes(instance, element);
      //    SetText(instance, element);										// Set the text for the new instance.
      //}

      // Call EndInit() for ISupportInitialize implementors.
      if (!isPropertyDeclaration)
      {
        InitializeFields(instance, ignoreMissingFields);				// First initialize any auto-initializing fields in the instance.
        if (instance is ISupportInitialize)								// For instances that support ISupportInitialize...
        {
          ((ISupportInitialize)instance).EndInit();					// Then call the EndInit() method.
        }
      }

      // Pop the instance.
      if (!isPropertyDeclaration)											// If the instance is a true instance...
      {
        objectStack.Pop();												// pop the object stack.
        OnEndInstantiate(instance);
      }
    }

    /// <summary>
    /// Process the instance's child elements.
    /// </summary>
    /// <param name="classInstance">The current instance.</param>
    /// <param name="element">The node associated with the instance.</param>
    protected void ProcessCollections(object classInstance, XmlNode element)
    {
      foreach (XmlNode collectionElement in element.ChildNodes)			// Get the child nodes.
      {
        if (collectionElement is XmlElement)							// Ignore comments and other non-element nodes.
        {
          if (!specialPrefixes.Contains(collectionElement.Prefix))
          {
            bool ret = ProcessElement(classInstance, collectionElement);	// Process the element
          }
        }
        else if (collectionElement is XmlComment)
        {
          //skip comments
        }
        else if (collectionElement is XmlText)
        {
          //content...
          if (OnSetContent != null)
          {
            OnSetContent(this, classInstance, collectionElement.Value);
          }
        }
      }
    }

    /// <summary>
    /// Process the child element of the supplied instance.  Compound property
    /// notation is not supported.
    /// </summary>
    /// <param name="classInstance">The parent instance.</param>
    /// <param name="element">The XmlNode element.</param>
    protected bool ProcessElement(object classInstance, XmlNode element)
    {
      // Initialization.
      bool isArray = false;
      bool success = false;													// Assume failure.
      string elementName = element.LocalName;								// Get the element name without the prefix.
      string propertyValue = element.InnerText;								// Get the property value to assign.
      string propertyName = String.Empty;									// Assume no property name.
      object propertyObject = null;											// Assume no property object.
      XmlAttribute returnType = null;
      int pos = elementName.IndexOf(".");
      if (pos > 0)
      {
        elementName = elementName.Substring(pos + 1);
      }

      ObjectInfo oi = (ObjectInfo)objectStack.Peek();
      bool isPropertyDeclaration = oi.HasPropertyDeclaration;				// Get the flag indicating that the element is a property declaration.
      if (isPropertyDeclaration)											// If it's a property declaration...
      {
        propertyName = oi.PropertyName;									// Get the property name of the 
        returnType = oi.ReturnType;										// Get the return type attribute, if provided.
      }

      // Set the property value.
      bool attributesProcessed;											// If it's a struct, this flag will be set.
      SetPropertyValue(													// Set the property value.
        classInstance,													// In this particular case, the result should be an instance that is
        elementName,													// either stand-alone, a read-only collection, or an instance assigned
        propertyName,													// to a collection.
        propertyValue,
        ref propertyObject,
        element,
        element,
        false,
        ref isPropertyDeclaration,
        ref isArray,
        out attributesProcessed,
        ref returnType);

      if (propertyObject != null)											// If the result is an instance as we expect it should be...
      {
        success = true;													// Then we have success.
        ProcessInstance(												// Recurse into the new instance.
          propertyObject,
          element,
          isPropertyDeclaration,
          elementName,
          attributesProcessed,
          returnType);
      }
      else
      {
        if ((elementName == "Null") || (isArray))
        {
          success = true;
        }
      }

      ((ObjectInfo)objectStack.Peek()).HasPropertyDeclaration = false;		// Clear the flag indicating that this instance has a property declaration.
      // It may have another one, or it may have a collection in addition.
      return success;
    }

    /// <summary>
    /// Instantiates a class from information in the XmlElement.
    /// </summary>
    /// <param name="parent">The control's parent.</param>
    /// <param name="element">The element defining the control.</param>
    /// <returns>The instantiated control.</returns>
    protected object InstantiateClass(object parent, XmlNode element)
    {
      string className = NameMangler(element.LocalName);					// Get the class name.
      string nameSpace = Namespace(element.Prefix);							// Get the namespace for the class.
      bool attributesProcessed = false;										// Assume it's a class.
      object classInstance = null;											// Assume it's not a reference.
      bool undef = false;

      if (element.Attributes != null)										// If there are attributes...
      {
        foreach (XmlAttribute attr in element.Attributes)
        {
          string refName;

          // MTC - 03/04/06: Allow for "undef" tag, which returns the instance but removes it from the references list.
          switch (attr.Prefix.ToLower())
          {
            case "ref":
              refName = attr.Value;								// Get the reference name.
              classInstance = GetReference(refName);					// Get the reference as the instance.
              break;
            case "undef":
              refName = attr.Value;								// Get the reference name.
              classInstance = GetReference(refName);					// Get the reference as the instance.
              RemoveReference(refName);
              undef = true;
              break;
          }
        }
      }

      if (!undef)
      {
        if (classInstance == null)											// If not a reference...
        {
          classInstance = InstantiateClass(									// Instantiate the class.
            nameSpace,
            className,
            element);
          attributesProcessed = false;
        }

        ProcessInstance(													// Recurse through the new instance.
          classInstance,
          element,
          false,
          null,
          attributesProcessed,
          null);
      }

      return classInstance;
    }

    /// <summary>
    /// Instantiate a control given the namespace and control name.
    /// </summary>
    /// <param name="nameSpace">The namespace in which the class resides.</param>
    /// <param name="name">The class name.</param>
    /// <param name="element">The current XmlNode.</param>
    /// <returns>The instantiated control.</returns>
    protected object InstantiateClass(string nameSpace, string name, XmlNode element)
    {
      object classInstance = null;

      if (name == "String")													// If the class name is "String"...
      {
        classInstance = new String(element.InnerText.ToCharArray());		// Then create a String instance from the inner text.
      }
      else
      {
        string qualifiedName = name;
        if (nameSpace != null)
        {
          qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + name;
          qualifiedName = qualifiedName + "," + StringHelpers.RightOf(nameSpace, ',');
        }
        classInstance = OnInstantiateClass(qualifiedName, element);
        if (classInstance == null)
        {
          classInstance = InstantiateClass(qualifiedName);
        }
      }
      return classInstance;
    }

    /// <summary>
    /// Replaces "." chars with "+" chars to handle inner class syntax.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <returns>The class name in which dotted notation has been replaced with the notation
    /// required to resolve inner class types.</returns>
    protected string NameMangler(string name)
    {
      return name.Replace('.', '+');										// Name mangling for nested classes.
    }

    /// <summary>
    /// Instantiates a class given the fully qualified name.
    /// </summary>
    /// <param name="qualifiedName">The fully qualified name.</param>
    /// <returns>The class instance.</returns>
    /// <exception cref="UnknownTypeException">Thrown when the type cannot be found.</exception>
    /// <exception cref="InstantiationException">Thrown when the class cannot be instantiated,
    /// usually due to lack of default constructor support.</exception>
    public object InstantiateClass(string qualifiedName)
    {
      object classInstance = null;

      Type t = Type.GetType(qualifiedName);
      if (t == null)
      {
        ServiceScope.Get<ILogger>().Warn("XamlParser:" + CurrentFile + " Unknown type :" + qualifiedName);
        return null;
      }
      try
      {
        classInstance = Activator.CreateInstance(t);
      }
      catch (Exception e)
      {
        throw (new InstantiationException("Can't instantiate " + qualifiedName + "\r\n" + e.Message));
      }
      return classInstance;
    }

    /// <summary>
    /// Process the attribute collection of the node defining the form, control, or object added to a collection.
    /// </summary>
    /// <param name="obj">The object whose properties we are setting.</param>
    /// <param name="element">The element whose attributes we are going to process.</param>
    public void ProcessAttributes(object obj, XmlNode element)
    {
      XmlAttributeCollection attributes = element.Attributes;				// Get the attributes in the element.
      foreach (XmlAttribute attr in attributes)							// For each attribute...
      {
        string propertyName = attr.LocalName;
        int pospoint = propertyName.IndexOf(".");
        if (pospoint > 0)
        {
          propertyName = propertyName.Substring(pospoint + 1);
        }
        string propertyValue = attr.Value;

        if (attr.Prefix.ToLower().IndexOf("ref") != -1)					// Ignore ref: attributes
        {
          continue;
        }

        if (propertyName == "_ReturnType")								// Ignore _ReturnType attribute.
        {
          continue;
        }

        if (propertyName == "_IsProperty")								// Ignore _IsProperty attribute.
        {
          continue;
        }
        if (attr.Prefix == "xmlns")
        {
          if (OnImportNameSpace != null)
          {
            OnImportNameSpace(this, obj, attr.Value);
          }
          continue;
        }

        if (propertyValue.StartsWith("{") &&							// If the entire attribute value is a reference...
          propertyValue.EndsWith("}"))
        {
          string refVal = StringHelpers.Between(						// Get the reference value.
            propertyValue, '{', '}');

          /*** special case for resources ***/
          if (refVal.StartsWith("StaticResource") || refVal.StartsWith("DynamicResource"))
          {
            if (OnGetResource != null)
            {
              int pos = refVal.IndexOf(' ');
              object objValue = OnGetResource(this, obj, refVal.Substring(pos + 1));
              Type t = obj.GetType();
              PropertyInfo prop = t.GetProperty(propertyName);
              if (prop == null)
              {
                ServiceScope.Get<ILogger>().Warn("XamlParser:{0} Property:{1} not found on  {2}", CurrentFile, propertyName, obj.GetType().ToString());
                return;
              }
              if (objValue == null)
              {
                ServiceScope.Get<ILogger>().Warn("XamlParser:{0} Resource:{1} not found", CurrentFile, refVal);
                return;
              }
              MethodInfo setInfo = prop.GetSetMethod();
              setInfo.Invoke(obj, new object[] { objValue });
            }
          }
          else if (refVal.StartsWith("Binding"))
          {
            if (OnGetBinding != null)
            {
              Type t = obj.GetType();
              PropertyInfo prop = t.GetProperty(propertyName);
              if (prop == null)
              {
                ServiceScope.Get<ILogger>().Warn("XamlParser:{0} Property:{1} not found on  {2}", CurrentFile, propertyName, obj.GetType().ToString());

                return;
              }
              int pos = refVal.IndexOf(' ');
              OnGetBinding(this, obj, refVal.Substring(pos + 1), prop);
            }
          }
          else
          {
            /*** special case for resources ***/
            MxBinding binding = new MxBinding(							// Create a binding entry.
              obj,
              propertyName,
              refVal,
              attr);
            if (!binding.Bind(this))									// Attempt to resolve and assign the reference now.
            {
              lateBindings.Add(binding);								// Didn't succeed.  Add to the late binding collection.
            }
          }
        }
        else
        {
          while (propertyValue.IndexOf('{') != -1)					// If there are embedded references in the attribute value...
          {
            string refName =											// Get the reference name.
              StringHelpers.Between(propertyValue, '{', '}');
            if (!ContainsReference(refName))						// Verify that it already exists.  Embedded references must be immediately resolvable.
            {
              ServiceScope.Get<ILogger>().Warn("XamlParser:" + CurrentFile + " Cannot make a forward reference to :" + propertyValue + " on:" + obj.GetType().ToString());
              return;
            }

            object propObj = GetReference(refName);					// Get the reference.
            string val = propObj.ToString();							// Get the value as a string.
            propertyValue = StringHelpers.LeftOf(propertyValue, '{') +	// Replace the reference with the value.
              val +
              StringHelpers.RightOf(propertyValue, '}');
          }

          bool isProperty = SetPropertyValue(							// Assign the attribute value to the corresponding class property.
            obj,
            propertyName,
            "",
            propertyValue,
            element,
            attr,
            true);

          if (!isProperty)											// If it isn't a property...
          {
            if (attr.Prefix.ToLower() != "def")						// and it isn't a def'd attribute (we allow def tags on attributes that aren't properties)
            {
              bool isEvent =										// Maybe it's an event.
                SetEvent(obj, propertyName, propertyValue);

              if (!isEvent)										// If not an event, then this we don't know what it is.
              {
                if (!OnCustomProperty(obj, propertyName, propertyValue))
                {
                  ServiceScope.Get<ILogger>().Warn("XamlParser:" + CurrentFile + " The property " + propertyName + " does not exist. on:" + obj.GetType().ToString());
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// If the node has inner text, set the Text property of the instance.
    /// </summary>
    /// <param name="classInstance">The instance.</param>
    /// <param name="element">The associated XmlNode.</param>
    protected void SetText(object classInstance, XmlNode element)
    {
      if (element.InnerXml == element.InnerText)							// If the InnerXml matches the InnerText (no subnodes)
      {
        if (element.InnerText != String.Empty)							// And there is some inner text...
        {
          SetPropertyValue(classInstance, "Text", "", element.InnerText, element, true);
        }
      }
    }

    /// <summary>
    /// Sets the value of an instance's property.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="elementName"></param>
    /// <param name="propertyName"></param>
    /// <param name="val"></param>
    /// <param name="node"></param>
    /// <param name="isAttribute"></param>
    /// <returns></returns>
    public bool SetPropertyValue(object obj, string elementName, string propertyName, object val, XmlNode node, bool isAttribute)
    {
      object dummy = null;
      bool isPropertyDeclaration = false;
      bool isArray = false;
      bool attributesProcessed;
      XmlAttribute returnType = null;
      return SetPropertyValue(obj, elementName, propertyName, val, ref dummy, node, node, isAttribute, ref isPropertyDeclaration, ref isArray, out attributesProcessed, ref returnType);
    }

    /// <summary>
    /// Set's the value of an instance's property.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="elementName"></param>
    /// <param name="propertyName"></param>
    /// <param name="val"></param>
    /// <param name="element"></param>
    /// <param name="node"></param>
    /// <param name="isAttribute"></param>
    /// <returns></returns>
    public bool SetPropertyValue(object obj, string elementName, string propertyName, object val, XmlNode element, XmlNode node, bool isAttribute)
    {
      object dummy = null;
      bool isPropertyDeclaration = false;
      bool isArray = false;
      bool attributesProcessed;
      XmlAttribute returnType = null;
      return SetPropertyValue(obj, elementName, propertyName, val, ref dummy, element, node, isAttribute, ref isPropertyDeclaration, ref isArray, out attributesProcessed, ref returnType);
    }

    /// <summary>
    /// Set's the value of an instance's property.
    /// </summary>
    /// <param name="obj">The current instance.</param>
    /// <param name="elementName">The element name.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="val">The property value.</param>
    /// <param name="propertyObject">A property instance, if the property is a class.</param>
    /// <param name="element">The XmlNode element.</param>
    /// <param name="node">THe XmlNode of the node.</param>
    /// <param name="isAttribute">Flag indicating that the property is an attribute.</param>
    /// <param name="isPropertyDeclaration">Flag indicating that the property is a property declaration in the xml.</param>
    /// <param name="isArray">Flag indicating that the instance is an array.</param>
    /// <param name="attributesProcessed">Flag indicating that the instance is a struct.</param>
    /// <param name="returnType">The property return type, if any.</param>
    /// <returns>True if the value was successfully assigned to the instance.</returns>
    public bool SetPropertyValue(object obj, string elementName, string propertyName, object val, ref object propertyObject, XmlNode element, XmlNode node, bool isAttribute, ref bool isPropertyDeclaration, ref bool isArray, out bool attributesProcessed, ref XmlAttribute returnType)
    {
      //CodePath.Executing(100);

      // These code paths are only executed when an extender is implemented.
      //CodePath.Executing(127);
      //CodePath.Executing(145);

      attributesProcessed = false;
      propertyObject = null;
      PropertyInfo pi = null;

      if ((!isAttribute) && (element != null))							// If the property is an instance rather than a property, and has a corresponding XmlNode element...
      {
        //CodePath.Executing(101);
        if (element.Attributes != null)									// And there is an attribute collection...
        {
          //CodePath.Executing(102);
          foreach (XmlAttribute attr in element.Attributes)			// For each attribute...
          {
            if (attr.Prefix.ToLower() == "ref")						// Test to see if there's a ref prefix.
            {
              //CodePath.Executing(103);
              string refName = attr.Value;
              propertyObject = GetReference(refName);				// Get the reference.  It must be resolvable.
              object classInstance = propertyObject;
              TestForCollection(obj, classInstance, element);		// If the parent instance is a collection, add it now.
              return true;										// and exit.  No further processing is necessary.
            }
          }
        }
      }

      if ((element != null) && (element.Attributes != null))			// If there's a supporting XmlNode element and there are attributes...
      {
        //CodePath.Executing(104);
        XmlAttribute returnTypeAttr = element.Attributes["_ReturnType"];	// And one of them is _ReturnType...
        if (returnTypeAttr != null)
        {
          //CodePath.Executing(105);
          returnType = returnTypeAttr;
          string[] info = returnType.Value.Split(':');					// split the value into the prefix and type information.
          string ns = info[0];
          string type = info[1];
          string nameSpace = Namespace(ns);								// Get the namespace for the return type.
          string qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(type);
          //CodePath.Executing(109);
          qualifiedName = qualifiedName +								// Get the qname.
            "," +
            StringHelpers.RightOf(nameSpace, ',');
          Type t = Type.GetType(qualifiedName);							// Get the Type for the return type.
          pi = GetPropertyInfo(obj, elementName, t);					// Get the PropertyInfo for this specific return type.
          isPropertyDeclaration = true;									// This element must be a property declaration.
          propertyObject = obj;
          return true;
        }
      }

      //CodePath.Executing(106);
      pi = GetPropertyInfo(obj, elementName);								// No element or no element attributes.  Just get the PropertyInfo

      if (element.Attributes != null)
      {
        XmlAttribute isPropertyAttr = element.Attributes["_IsProperty"];
        if (isPropertyAttr != null)
        {
          if (!Convert.ToBoolean(isPropertyAttr.Value))
          {
            pi = null;
          }
        }
      }

      bool isProperty = pi != null;											// Set flag indicating that the element is actually a property of the parent.

      if ((isProperty) &&												// If the property type is Type, then we have a special handler.
        (pi.PropertyType.Equals(typeof(System.Type))))
      {
        //CodePath.Executing(107);
        string typeName = val.ToString();									// Get the value as a string.
        if (typeName.IndexOf(':') != -1)								// If a ':' char exists...
        {
          //CodePath.Executing(108);
          string[] info = typeName.Split(':');							// Split the value into the namespace and class name.
          string ns = info[0];
          string type = info[1];
          string nameSpace = Namespace(ns);
          typeName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(type) + ", " + StringHelpers.RightOf(nameSpace, ',');
        }
        Type t = Type.GetType(typeName);									// Get the type.
        if (t == null)
        {
          throw (new TypeAssignmentException("Can't create type " + val));
        }
        pi.SetValue(obj, t, null);										// Assign the type to the property.
        return true;													// All done!
      }

      if ((isProperty) &&												// If the type is a property of the parent...
        (!isPropertyDeclaration) &&										// and it isn't a property declaration...
        (!pi.PropertyType.IsArray))									// and it isn't an array...
      {
        //CodePath.Executing(109);
        if (!isAttribute)												// and it isn't an attribute...
        {
          //CodePath.Executing(110);
          if (pi.CanWrite)											// and it's writable...
          {
            //CodePath.Executing(111);
            if (pi.PropertyType.IsAbstract)							// and the property type is abstract or interface 
            {
              //CodePath.Executing(112);
              // if we can't read the property, then treat it as assignable only by a specified concrete instance.
              if (!pi.CanRead)									// and it's write only...
              {
                //CodePath.Executing(113);
                if (node.ChildNodes.Count != 1)					// then it's a property declaration.
                {
                  throw (new ExpectedSingleChildException("An abstract property type requires one and only one concrete instance to initialize it."));
                }
                propertyObject = obj;
                isPropertyDeclaration = true;
                return true;
              }

              //CodePath.Executing(114);
              if (node.ChildNodes.Count == 1)						// It's readable.  If there's only one child...
              {
                //CodePath.Executing(115);
                XmlNode child = node.ChildNodes[0];
                string tagName = child.LocalName;
                if (tagName == "Null")							// Special test for a Null child.
                {
                  //CodePath.Executing(116);
                  propertyObject = obj;
                  isPropertyDeclaration = true;					// Treat the property as a property declaration.
                  return true;
                }

                string nameSpace = Namespace(child.Prefix);		// Get the child type.
                //CodePath.Executing(117);
                string qualifiedName = tagName;
                if (nameSpace != null)
                  qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(tagName) + ", " + StringHelpers.RightOf(nameSpace, ',');
                if (OnPropertyDeclarationTest(pi, qualifiedName))
                {
                  isPropertyDeclaration = true;
                }
                else
                {
                  Type childType = Type.GetType(qualifiedName);
                  if (childType != null)
                  {
                    //CodePath.Executing(118);
                    if (pi.PropertyType.IsInterface)			// If the property type is an interface...
                    {
                      //CodePath.Executing(119);
                      TypeFilter typeFilter = new TypeFilter(InterfaceFilter);
                      Type[] interfaces = childType.FindInterfaces(typeFilter, pi.PropertyType.FullName);
                      if (interfaces.Length == 0)				// See if the child type implements the interface.
                      {
                        throw (new ChildTypeNotPropertyTypeException("The child " + tagName + " is not the same type as the parent's property."));
                      }
                      isPropertyDeclaration = true;
                    }
                    else
                    {
                      //CodePath.Executing(120);			// See if the child is a type of or a subclass of the property.
                      if ((childType.Equals(pi.PropertyType)) ||
                        (childType.IsSubclassOf(pi.PropertyType)))
                      {
                        //CodePath.Executing(121);		// If so, then the property is a property declaration.
                        isPropertyDeclaration = true;
                      }
                      else
                      {
                        // child does not specify a concrete type!  Get the instance instead.
                        throw (new ChildTypeNotPropertyTypeException("The child " + tagName + " is not the same type as the parent's property."));
                      }
                    }
                  }
                  else
                  {
                    throw (new UnknownTypeException("The child " + tagName + " is an unknown type."));
                  }
                }
                //CodePath.Executing(122);
                propertyObject = obj;								// isPropertyDeclaration is always true at this point.
                return true;
              }
              else
              {
                throw (new ExpectedSingleChildException("Expected a single child to assign to the property of the parent."));
              }
            }
            else if ((node.Attributes != null) &&					// The property type is a concrete class.  If there are no attributes...
              (node.Attributes.Count == 0))
            {
              //CodePath.Executing(123);
              // There can be one and only one child node.
              if (node.ChildNodes.Count == 1)						// and only one child...
              {
                //CodePath.Executing(124);
                XmlNode child = node.ChildNodes[0];
                string tagName = child.LocalName;
                if (tagName == "Null")							// Special handler for a Null child.
                {
                  //CodePath.Executing(125);
                  // handled by the NullModel implementation
                  propertyObject = obj;
                  isPropertyDeclaration = true;
                  return true;
                }

                string nameSpace = Namespace(child.Prefix);		// Get the child type.
                //CodePath.Executing(126);

                string qualifiedName = tagName;
                if (nameSpace != null)
                  qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(tagName) + ", " + StringHelpers.RightOf(nameSpace, ',');
                if (OnPropertyDeclarationTest(pi, qualifiedName))
                {
                  //CodePath.Executing(127);
                  propertyObject = obj;
                  isPropertyDeclaration = true;
                  return true;
                }
                Type childType = Type.GetType(qualifiedName);
                // A null will be returned with the child type is a string.
                // In this case, we want to fall through to the basic property
                // assignment handler, which will take care of the string
                // assignment.
                if (childType != null)
                {
                  //CodePath.Executing(128);			// If the child is a type or subclass of the property...
                  if ((childType.Equals(pi.PropertyType)) ||
                    (childType.IsSubclassOf(pi.PropertyType)))
                  {
                    //CodePath.Executing(129);		// Then we have a property declaration.
                    propertyObject = obj;
                    isPropertyDeclaration = true;
                    return true;
                  }
                  else
                  {
                    throw (new ChildTypeNotPropertyTypeException("The child " + tagName + " is not the same type as the parent's property."));
                  }
                }
              }
              else
              {
                throw (new ExpectedSingleChildException("Expected a single child to assign to the property of the parent."));
              }
            }
          }

          //CodePath.Executing(130);
          if ((!pi.CanWrite) && (pi.CanRead))							// Is this a read-only property?
          {
            //CodePath.Executing(131);
            // no custom handler.  Get the object.
            object classInstance = pi.GetValue(obj, null);				// Read the value...
            if (classInstance != null)
            {
              //CodePath.Executing(132);
              propertyObject = classInstance;							// and return it.
              return true;
            }
            else
            {
              throw (new ReadOnlyInstanceNullException("Property " +
                elementName +
                " is read-only and returned a null.  Cannot use null class instances."));
            }
          }
          // allow fall-through
        }

        //CodePath.Executing(133);									// read-writable.
        TypeConverter tc = TypeDescriptor.GetConverter(pi.PropertyType);				// Get the type converter for the property.
        if (val is String)												// If assigning a string...
        {
          //CodePath.Executing(134);
          if (tc != null && tc.CanConvertFrom(typeof(string)))		// and we can convert from a string to the property type...
          {
            //CodePath.Executing(135);
            object objConv = null;									// do the conversion.
            try
            {
              if ((string)val == "Auto" && pi.PropertyType == typeof(double))
              {
                objConv = (double)0.0;
              }
              else
              {
                objConv = tc.ConvertFromInvariantString((string)val);
              }
            }
            catch
            {
              ServiceScope.Get<ILogger>().Warn("XamlParser:" + CurrentFile + " Cannot convert '{0}' to {1}", val, pi.Name);
              //throw (new PropertyConversionException("Can't convert " + val));
              return true;
            }

            try
            {
              pi.SetValue(obj, objConv, null);				// Assign the converted value to the property.
            }
            catch (Exception e)
            {
              throw (new PropertyConversionException("Can't convert " + val + "\r\n" + e.Message));
            }
            return true;
          }
          else if (pi.PropertyType is System.Object)
          {
            if (CustomTypeConvertor != null)
            {
              CustomTypeEventArgs args = new CustomTypeEventArgs();
              args.PropertyType = pi.PropertyType;
              args.Value = val;
              CustomTypeConvertor(this, args);
              if (args.Result != null)
              {
                pi.SetValue(obj, args.Result, null);
              }
              else
              {
                pi.SetValue(obj, val, null);
              }
            }
            else
            {
              pi.SetValue(obj, val, null);
            }
          }
        }
        else															// Assigning an non-string to the property.
        {
          //CodePath.Executing(136);
          try
          {
            // Updated 2/9/06 - was throwing an exception if assigning a null value.
            if (val != null)
            {
              TypeConverter valTypeConverter =							// Get the type converter for the value to assign.
                TypeDescriptor.GetConverter(val.GetType());

              if (valTypeConverter.CanConvertTo(pi.PropertyType))
              {
                //CodePath.Executing(137);
                val = valTypeConverter.ConvertTo(						// Do the conversion.
                  val,
                  pi.PropertyType);
              }
            }
            pi.SetValue(obj, val, null);							// Assign the value.
          }
          catch (Exception e)
          {
            throw (new PropertyConversionException("Can't convert value in property assignment.\r\n" + e.Message));
          }
          return true;
        }
      }

      //CodePath.Executing(138);
      if ((!isProperty) && (isPropertyDeclaration))						// Element is not a property of the parent, but there's a property declaration in effect...
      {
        //CodePath.Executing(139);
        if (returnType != null)											// If a return type has been specified, use it to resolve the PropertyInfo of the property declaration.
        {
          //CodePath.Executing(140);
          string[] info = returnType.Value.Split(':');
          string ns = info[0];
          string type = info[1];
          string nameSpace = Namespace(ns);
          string qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(type);
          if (StringHelpers.RightOf(nameSpace, ',') != String.Empty)
          {
            //CodePath.Executing(141);
            qualifiedName = qualifiedName + "," + StringHelpers.RightOf(nameSpace, ',');
          }
          Type t = Type.GetType(qualifiedName);
          pi = GetPropertyInfo(obj, propertyName, t);
        }
        else
        {
          //CodePath.Executing(142);
          pi = GetPropertyInfo(obj, propertyName);
        }

        //CodePath.Executing(143);
        // reset, because the instance being assigned is not a property
        isPropertyDeclaration = false;									// Clear the property declaration flag.
        string nameSpace2 = Namespace(node.Prefix);
        if (elementName == "Null")
        {
          //CodePath.Executing(144);								// Special handler for a Null property tag.
          pi.SetValue(obj, null, null);
          return true;
        }

        string nestedName = NameMangler(elementName);
        object classInstance = null;

        classInstance = OnInstantiatePropertyDeclaration(pi, nestedName);
        if (classInstance != null)
        {
          //CodePath.Executing(145);
          attributesProcessed = false;
        }
        else
        {
          //CodePath.Executing(146);
          classInstance = InstantiateClass(								// Create an instance of the element.
            nameSpace2,
            nestedName,
            node);
        }
        attributesProcessed = false;
        //CodePath.Executing(147);
        if (pi.PropertyType.IsValueType)								// If it's a struct...
        {
          //CodePath.Executing(148);
          ProcessCollections(classInstance, node);					// Process collections and attributes now, since it's a value type.
          ProcessAttributes(classInstance, node);
          attributesProcessed = true;
        }
        try
        {

          pi.SetValue(obj, classInstance, null);						// Assign the instance to the parent's property.
        }
        catch
        {
          throw (new PropertyConversionException("Couldn't set property value."));
        }
        propertyObject = classInstance;									// Now continue processing the new instance.
        return true;
      }

      if ((isProperty) && (isPropertyDeclaration))
      {
        //CodePath.Executing(149);
        string nameSpace = Namespace(node.Prefix);
        string qualifiedName;
        qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(elementName) + ", " + StringHelpers.RightOf(nameSpace, ',');
        object classInstance = InstantiateClass(qualifiedName);			// then instantiate it as a stand-alone class.
        try
        {
          pi.SetValue(obj, classInstance, null);
        }
        catch
        {
          throw (new PropertyConversionException("Couldn't set property value."));
        }
        propertyObject = classInstance;
        return true;
      }

      //CodePath.Executing(150);										// No property declaration is in effect.
      if ((isProperty) && (pi.PropertyType.IsArray))					// If the property is an array...
      {
        //CodePath.Executing(151);

        // isPropertyDeclaration test added.  See example in DockPanelDemo, plugins.myxaml, regarding TableLayoutPanel
        if ((isPropertyDeclaration) &&
          (element.ChildNodes.Count != 0))							// And if there are children...
        {
          //CodePath.Executing(152);
          ArrayList arrayList = new ArrayList();						// Create an ArrayList to hold the children.
          foreach (XmlNode collectionElement in element.ChildNodes)	// For each child...
          {
            if (collectionElement is XmlElement)					// Process only XmlElement types.
            {
              //CodePath.Executing(153);
              object arrayElement =								// Create the child instance.
                InstantiateClass(obj, collectionElement);
              arrayList.Add(arrayElement);						// Add it to the instance collection.
            }
          }
          try
          {
            pi.SetValue(obj,										// Assign the array to the property.
              arrayList.ToArray(arrayList[0].GetType()), null);
          }
          catch
          {
            throw (new ArrayConversionException("Array elements not of the appropriate type."));
          }
          isArray = true;
        }
        else
        {
          //CodePath.Executing(154);
          string[] items = ((string)val).Split(',');					// Process the attribute value as a comma delimited list.
          string itemTypeName = StringHelpers.LeftOf(pi.PropertyType.AssemblyQualifiedName, '[') + StringHelpers.RightOf(pi.PropertyType.AssemblyQualifiedName, ']');
          Type itemType = Type.GetType(itemTypeName);					// Get the type that each string should be converted to.
          TypeConverter tc = TypeDescriptor.GetConverter(itemType);
          //CodePath.Executing(149);

          Array array = (Array)Activator.CreateInstance(				// Create an array of the property type.
            pi.PropertyType,
            new object[] { items.Length });
          //CodePath.Executing(150);

          int idx = 0;
          foreach (string s in items)									// Add each item in the comma delimited list to the array.
          {
            string item = s.Trim();
            object cval = null;
            //CodePath.Executing(151);
            try
            {
              cval = tc.ConvertFrom(item);							// Convert it to the array item type.
            }
            catch
            {
              throw (new ArrayConversionException("Can't convert '" + s + "' to array type."));
            }
            array.SetValue(cval, idx);								// Add to array.
            ++idx;
          }
          pi.SetValue(obj, array, null);								// Assign the array to the property.
          return true;
        }
      }

      //CodePath.Executing(152);
      if ((!isProperty) && (!isAttribute))								// If the element isn't a property of the parent and it isn't an attribute...
      {
        //CodePath.Executing(155);
        object classInstance = null;
        string nameSpace = Namespace(node.Prefix);
        string qualifiedName = elementName;
        if (nameSpace != null)
          qualifiedName = StringHelpers.LeftOf(nameSpace, ',') + "." + NameMangler(elementName) + ", " + StringHelpers.RightOf(nameSpace, ',');

        classInstance = OnInstantiateClass(qualifiedName, node);
        if (classInstance == null)
        {
          classInstance = InstantiateClass(qualifiedName);					// then instantiate it as a stand-alone class.
        }
        if (classInstance == null) return false;
        propertyObject = classInstance;

        if (classInstance.GetType().IsValueType)						// If it's a struct, process it's children and attributes now.
        {
          //CodePath.Executing(156);
          ProcessCollections(classInstance, node);
          ProcessAttributes(classInstance, node);
          attributesProcessed = true;
        }

        isProperty = true;
        attributesProcessed |= TestForCollection(obj, classInstance, node);	// See if it needs to be added to a collection.
      }

      //CodePath.Executing(157);
      return isProperty;
    }

    /// <summary>
    /// Adds the instance to the an object implementing IList.  If the object
    /// implements ICollection, then the AddToCollection event is raised.
    /// </summary>
    /// <param name="obj">The IList or ICollection instance.</param>
    /// <param name="classInstance">The instance to add to the collection.</param>
    /// <returns></returns>
    protected bool TestForCollection(object obj, object classInstance, XmlNode node)
    {
      bool ret = false;
      if (obj is IList)													// If the parent implements IList...
      {
        // MTC - 06/28/06 : Changed in the root caller.
        // MTC - 03/04/06 : Need to process attributes before adding a node to a collection.
        ((IList)obj).Add(classInstance);								// Attempt to add the instance.
        ProcessAttributes(classInstance, node);							// Process attributes before adding instance to collection!

        // Finish initialization before adding to collection.
        // 8/16/06:
        // This will result in a second call to EndInit, when the parser is finished processing the instance itself.
        // For now, do not call EndInit.  We need to re-evaluate this issue of parsing attributes, calling EndInit, and adding classes
        // to collections.
        //if (classInstance is ISupportInitialize)
        //{
        //    ((ISupportInitialize)classInstance).EndInit();
        //}

        // Commenting out the next line causes events in IList objects to be wired up twice.
        ret = true;
      }
      else if (obj is ICollection)										// If the parent implements ICollection...
      {
        // MTC - 06/28/06 : Changed in the root caller.
        // MTC - 03/04/06 : Need to process attributes before adding a node to a collection.
        ProcessAttributes(classInstance, node);							// process attributes before adding instance to collection!

        // Finish initialization before adding to collection.
        // 8/16/06:
        // This will result in a second call to EndInit, when the parser is finished processing the instance itself.
        // For now, do not call EndInit.  We need to re-evaluate this issue of parsing attributes, calling EndInit, and adding classes
        // to collections.
        //if (classInstance is ISupportInitialize)
        //{
        //    ((ISupportInitialize)classInstance).EndInit();
        //}

        ret = OnAddToCollection(obj, classInstance, node);				// let an extender manage adding the instance.
      }
      else
      {
        if (OnSetContent != null)
        {
          ProcessAttributes(classInstance, node);							// process attributes before adding instance to collection!
          OnSetContent(this, obj, classInstance);
          ret = true;
        }
      }
      return ret;
    }

    /// <summary>
    /// Wire's up the even to the specified handler.
    /// </summary>
    /// <param name="obj">The instance containing the event.</param>
    /// <param name="propertyName">The event.</param>
    /// <param name="val">The handler.</param>
    /// <returns>True if wireup succeeded.</returns>
    public bool SetEvent(object obj, string propertyName, string val)
    {
      EventInfo ei = obj.GetType().GetEvent(propertyName);					// Get the EventInfo, if any.
      bool isEvent = ei != null;
      if (isEvent)
      {
        string methodName = val;
        methodName = methodName.Trim();
        Delegate dlgt = FindHandlerForTarget(obj, ei, methodName);		// Find a delegate that matches the method signature.
        if (dlgt != null)
        {
          ei.AddEventHandler(obj, dlgt);								// Wire up the event to the handler.
        }
        else
        {
          throw (new EventWireUpException("The event signature is different than the handler " + val));
        }
      }
      return isEvent;
    }

    /// <summary>
    /// Get the PropertyInfo instance for the supplied object and property name.
    /// </summary>
    /// <param name="obj">The object on which we want to find the named property.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The PropertyInfo instance or null.</returns>
    protected PropertyInfo GetPropertyInfo(object obj, string propertyName)
    {
      PropertyInfo pi = null;
      try
      {
        //				pi=obj.GetType().GetProperty(propertyName);
        pi = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        if (pi == null)
        {
          System.Type t = obj.GetType().BaseType;

          while (t != null && pi == null)
          {
            pi = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            t = t.BaseType;
          }
        }
      }
      catch
      {
        throw (new AmbiguousPropertyException("The property " + propertyName + " has multiple matches due to different return types."));
      }
      return pi;
    }

    /// <summary>
    /// Get the FieldInfo instance for the supplied object and property name.
    /// </summary>
    /// <param name="obj">The object on which we want to find the named field.</param>
    /// <param name="propertyName">The name of the field.</param>
    /// <returns>The FieldInfo instance or null.</returns>
    protected FieldInfo GetFieldInfo(object obj, string fieldName)
    {
      FieldInfo fi = null;
      try
      {
        fi = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (fi == null)
        {
          System.Type t = obj.GetType().BaseType;
          while (t != null && fi == null)
          {
            fi = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            t = t.BaseType;
          }
        }
      }
      catch
      {
        throw (new AmbiguousFieldException("The field " + fieldName + " has multiple matches due to different return types."));
      }
      return fi;
    }

    /// <summary>
    /// Get all the fieldinfo's of a certain type. The derived one's are also added in this list
    /// </summary>
    /// <param name="objectType">The type for which to get the fields</param>
    /// <returns>An array with FieldInfo objects</returns>
    protected FieldInfo[] GetFieldInfos(System.Type objectType)
    {
      System.Collections.Generic.Dictionary<string, FieldInfo> temp = new System.Collections.Generic.Dictionary<string, FieldInfo>();
      System.Type t = objectType;
      while (t != null)
      {
        foreach (FieldInfo fi in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
        {
          if (!temp.ContainsKey(fi.Name))
          {
            // we add fields that are not in the list yet
            temp.Add(fi.Name, fi);
          }
        }

        t = t.BaseType;
      }

      // Convert the list to an array
      FieldInfo[] Retval = new FieldInfo[temp.Count];
      temp.Values.CopyTo(Retval, 0);

      return Retval;
    }
    /// <summary>
    /// Get's the property information for the supplied property and return type.
    /// </summary>
    /// <param name="obj">The instance.</param>
    /// <param name="propertyName">The instance's property.</param>
    /// <param name="retType">The return type.</param>
    /// <returns>The PropertyInfo object.</returns>
    /// <exception cref="AmbiguousPropertyException">Thrown when the property cannot be resolved.</exception>
    protected PropertyInfo GetPropertyInfo(object obj, string propertyName, Type retType)
    {
      PropertyInfo pi = null;
      try
      {
        pi = obj.GetType().GetProperty(propertyName, retType);
      }
      catch
      {
        throw (new AmbiguousPropertyException("The property " + propertyName + " has multiple matches due to different return types."));
      }
      return pi;
    }

    /// <summary>
    /// Search the event target for a delegate that matches the required signature
    /// for the specified method name.
    /// </summary>
    /// <param name="eventTarget">The event target instance to search.</param>
    /// <param name="ei">Provides the event handler type information.</param>
    /// <param name="methodName">The method to be wired up.</param>
    /// <returns>The delegate, or null if not found.</returns>
    public Delegate FindHandlerForTarget(object eventTarget, EventInfo ei, string methodName)
    {
      Delegate dlgt = null;													// Assume no delegate found.

      foreach (MethodInfo mi in eventTarget.GetType().GetMethods(			// For each method in the target...
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static))
      {
        if (mi.Name == methodName)										// If the name matches...
        {
          try
          {
            dlgt = Delegate.CreateDelegate(							// Try to create the delegate.
              ei.EventHandlerType,
              eventTarget,
              methodName);
          }
          catch { }													// Exceptions are ok, as there might be methods of the same name with different signatures.

          if (dlgt != null)
          {
            break;													// Found a match!
          }
        }
      }
      return dlgt;
    }

    private bool InterfaceFilter(Type typeObj, object criteriaObj)
    {
      return typeObj.ToString() == criteriaObj.ToString();
    }

    /// <summary>
    /// Raises the AddToCollection event.
    /// </summary>
    /// <param name="obj">The ICollection implementing instance.</param>
    /// <param name="instance">The instance to be added to the ICollection.</param>
    /// <returns>True if adding the instance to the ICollection succeeded.</returns>
    protected bool OnAddToCollection(object obj, object instance, XmlNode node)
    {
      bool ret = false;

      if (AddToCollection != null)
      {
        AddToCollectionEventArgs eventArgs = new AddToCollectionEventArgs(obj, instance, node);
        AddToCollection(this, eventArgs);
        ret |= eventArgs.Result;
      }
      return ret;
    }

    /// <summary>
    /// Raises the InstantiateBegin event.
    /// </summary>
    /// <param name="obj">The object being instantiated.</param>
    protected void OnBeginInstantiate(object obj)
    {
      if (InstantiateBegin != null)
      {
        InstantiateBegin(this, new InstantiateEventArgs(obj));
      }
    }

    /// <summary>
    /// Raises the InstantiateEnd event.
    /// </summary>
    /// <param name="obj">The object being instantiated.</param>
    protected void OnEndInstantiate(object obj)
    {
      if (InstantiateEnd != null)
      {
        InstantiateEnd(this, new InstantiateEventArgs(obj));
      }
    }

    protected bool OnPropertyDeclarationTest(PropertyInfo propertyInfo, string childQualifiedName)
    {
      bool ret = false;

      if (PropertyDeclarationTest != null)
      {
        PropertyDeclarationTestEventArgs e = new PropertyDeclarationTestEventArgs(propertyInfo, childQualifiedName);
        PropertyDeclarationTest(this, e);
        ret = e.Result;
      }
      return ret;
    }

    protected object OnInstantiatePropertyDeclaration(PropertyInfo propertyInfo, string childQualifiedName)
    {
      object ret = null;
      if (InstantiatePropertyDeclaration != null)
      {
        InstantiatePropertyDeclarationEventArgs e = new InstantiatePropertyDeclarationEventArgs(propertyInfo, childQualifiedName);
        InstantiatePropertyDeclaration(this, e);
        ret = e.Result;
      }
      return ret;
    }

    protected object OnInstantiateClass(string qname, XmlNode node)
    {
      object ret = null;
      if (InstantiateFromQName != null)
      {
        InstantiateClassEventArgs e = new InstantiateClassEventArgs(qname, node);
        InstantiateFromQName(this, e);
        ret = e.Result;
      }
      return ret;
    }

    protected bool OnCustomProperty(object obj, string propertyName, string propertyValue)
    {
      bool handled = false;
      if (CustomPropertyHandler != null)
      {
        CustomPropertyEventArgs e = new CustomPropertyEventArgs(obj, propertyName, propertyValue);
        CustomPropertyHandler(this, e);
        handled = e.Handled;
      }
      return handled;
    }
  }
}
