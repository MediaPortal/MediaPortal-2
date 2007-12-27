/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;

namespace MyXaml.Core.Exceptions
{
	/// <summary>
	/// Base class for all MyXaml exceptions.
	/// </summary>
	public class MyXamlException : ApplicationException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public MyXamlException(string msg) : base (msg) {}
	}

	/// <summary>
	/// Thrown when errors occur assigning to auto-initialize fields.
	/// </summary>
	public class AutoInitializeException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public AutoInitializeException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the reference list does not contain the specified reference.
	/// </summary>
	public class NoReferenceException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public NoReferenceException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the object graph for the reference cannot be resolved.
	/// </summary>
	public class ImproperComplexReferenceException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ImproperComplexReferenceException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown on markup errors.
	/// </summary>
	public class MarkupException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public MarkupException(string msg) : base(msg) {}
	}
	
	/// <summary>
	/// Thrown when adding an existing reference.
	/// </summary>
	public class ReferenceExistsException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ReferenceExistsException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when there are no children to the xml document.
	/// </summary>
	public class MissingGraphException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public MissingGraphException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the specified tag name cannot be found in the document child collection.
	/// </summary>
	public class GraphNotFoundException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public GraphNotFoundException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when an assembly cannot be resolved.
	/// </summary>
	public class MissingAssemblyException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public MissingAssemblyException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the namespace is malformed.
	/// </summary>
	public class ImproperNamespaceFormatException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ImproperNamespaceFormatException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when a type cannot be resolved.
	/// </summary>
	public class UnknownTypeException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public UnknownTypeException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when an instantiation error occurs (usually, no default constructor).
	/// </summary>
	public class InstantiationException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public InstantiationException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when an attribute does not correspond to a property in the instance.
	/// </summary>
	public class UnknownPropertyException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public UnknownPropertyException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when a read-only property returns a null.  The property cannot be used
	/// as a reference to further objects in the graph.
	/// </summary>
	public class ReadOnlyInstanceNullException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ReadOnlyInstanceNullException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the referenced class is missing the handler method.
	/// </summary>
	public class ReferencedEventMissingHandlerException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ReferencedEventMissingHandlerException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when wiring up an event fails.
	/// </summary>
	public class EventWireUpException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public EventWireUpException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when a forward reference is not allowed.
	/// </summary>
	public class ForwardReferenceException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ForwardReferenceException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when type conversion fails during array population.
	/// </summary>
	public class ArrayConversionException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ArrayConversionException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when assignment to a property of Type fails.
	/// </summary>
	public class TypeAssignmentException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public TypeAssignmentException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the IExtenderProvider cannot extend the object.
	/// </summary>
	public class ExtenderProviderException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ExtenderProviderException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown the child is not of the same type as the property of the parent.
	/// </summary>
	public class ChildTypeNotPropertyTypeException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ChildTypeNotPropertyTypeException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when more than one child is attempted to assign to the parent property,
	/// and the parent property is not a collection.
	/// </summary>
	public class ExpectedSingleChildException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ExpectedSingleChildException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when adding to an IList instance fails.
	/// </summary>
	public class AddingToIListException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public AddingToIListException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when a property conversion fails.
	/// </summary>
	public class PropertyConversionException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public PropertyConversionException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when a property name has been redeclared with a 'new' in a derived class.
	/// Use _ReturnType to specify the desired property based on return type.
	/// </summary>
	public class AmbiguousPropertyException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public AmbiguousPropertyException(string msg) : base(msg) {}
	}
	
	/// <summary>
    /// Thrown when a field name has been redeclared with a 'new' in a derived class.
    /// Use _ReturnType to specify the desired field based on return type.
    /// </summary>
    public class AmbiguousFieldException : MyXamlException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="msg">The error message.</param>
        public AmbiguousFieldException(string msg) : base(msg) { }
    }	

	/// <summary>
	/// Thrown when the extender class cannot be found.
	/// </summary>
	public class ExtenderClassNotFoundException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ExtenderClassNotFoundException(string msg) : base(msg) {}
	}

	/// <summary>
	/// Thrown when the extender cannot be instantiated.
	/// </summary>
	public class ExtenderActivationException : MyXamlException
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="msg">The error message.</param>
		public ExtenderActivationException(string msg) : base(msg) {}
	}

	public class NamespaceMappingChangedException : MyXamlException
	{
		public NamespaceMappingChangedException(string msg) : base(msg) {}
	}
}
