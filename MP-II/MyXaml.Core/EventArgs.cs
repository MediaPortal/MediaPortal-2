/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml;

namespace MyXaml.Core
{
	/// <summary>
	/// Defines the event args for the InstantiateBegin and InstantiateEnd events.
	/// </summary>
	public class InstantiateEventArgs : EventArgs
	{
		/// <summary>
		/// The instance being instantiated.
		/// </summary>
		protected object instance;

		/// <summary>
		/// Gets the instance being instantiated.
		/// </summary>
		public object Instance
		{
			get {return instance;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="instance">The instance being instantiated.</param>
		public InstantiateEventArgs(object instance)
		{
			this.instance=instance;
		}
	}

	/// <summary>
	/// Defines the event args for the AddToCollection event.
	/// </summary>
	public class AddToCollectionEventArgs : EventArgs
	{
		/// <summary>
		/// The container object implementing ICollection.
		/// </summary>
		protected object container;

		/// <summary>
		/// The instance being added to the collection.
		/// </summary>
		protected object instance;

		/// <summary>
		/// The result of the operation.
		/// </summary>
		protected bool result;

		/// <summary>
		/// Gets the container object implementing ICollection.
		/// </summary>
		public object Container
		{
			get {return container;}
		}

		/// <summary>
		/// Get the instance to be added to the collection.
		/// </summary>
		public object Instance
		{
			get {return instance;}
		}

		/// <summary>
		/// Get/set the result.  True if adding to the collection succeeded.
		/// </summary>
		public bool Result
		{
			get {return result;}
			set {result=value;}
		}

		protected XmlNode node;
		
		/// <summary>
		/// Gets/sets Node
		/// </summary>
		public XmlNode Node
		{
			get { return node; }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="container">The ICollection implementer.</param>
		/// <param name="instance">The instance to add to the collection.</param>
		public AddToCollectionEventArgs(object container, object instance, XmlNode node)
		{
			this.container=container;
			this.instance=instance;
			this.node = node;
		}
	}

	public class PropertyDeclarationTestEventArgs : EventArgs
	{
		protected PropertyInfo propertyInfo;
		protected string childQualifiedName;
		protected bool result;

		public PropertyInfo PropertyInfo
		{
			get {return propertyInfo;}
		}

		public string ChildQualifiedName
		{
			get {return childQualifiedName;}
		}

		public bool Result
		{
			get {return result;}
			set {result=value;}
		}

		public PropertyDeclarationTestEventArgs(PropertyInfo propertyInfo, string childQualifiedName)
		{
			this.propertyInfo=propertyInfo;
			this.childQualifiedName=childQualifiedName;
			result=false;
		}
	}

	public class InstantiatePropertyDeclarationEventArgs : EventArgs
	{
		protected PropertyInfo propertyInfo;
		protected string childQualifiedName;
		protected object result;

		public PropertyInfo PropertyInfo
		{
			get {return propertyInfo;}
		}

		public string ChildQualifiedName
		{
			get {return childQualifiedName;}
		}

		public object Result
		{
			get {return result;}
			set {result=value;}
		}

		public InstantiatePropertyDeclarationEventArgs(PropertyInfo propertyInfo, string childQualifiedName)
		{
			this.propertyInfo=propertyInfo;
			this.childQualifiedName=childQualifiedName;
			result=null;
		}
	}

	public class InstantiateClassEventArgs : EventArgs
	{
		protected string qname;
		protected XmlNode node;
		protected object result;

		public string AssemblyQualifiedName
		{
			get {return qname;}
		}

		public XmlNode Node
		{
			get {return node;}
		}

		public object Result
		{
			get {return result;}
			set {result=value;}
		}

		public InstantiateClassEventArgs(string qname, XmlNode node)
		{
			this.qname=qname;
			this.node=node;
		}
	}

	public class CustomPropertyEventArgs : EventArgs
	{
		protected object obj;
		protected string propertyName;
		protected string propertyValue;
		protected bool handled;

		public object Object
		{
			get {return obj;}
		}

		public string PropertyName
		{
			get {return propertyName;}
		}

		public string PropertyValue
		{
			get {return propertyValue;}
		}

		public bool Handled
		{
			get {return handled;}
			set {handled=value;}
		}

		public CustomPropertyEventArgs(object obj, string propertyName, string propertyValue)
		{
			this.obj=obj;
			this.propertyName=propertyName;
			this.propertyValue=propertyValue;
		}
	}
}
