/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;

namespace MyXaml.Core
{
	/// <summary>
	/// This attribute can be applied to a public or non-public field in a class
	/// instantiated by the parser.  Any fields marked with this attribute are
	/// expected to be initialized by an object instantiated during markup processing
	/// and having a "def" prefix.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple=false, Inherited=false)]
	public sealed class MyXamlAutoInitializeAttribute : Attribute
	{
		/// <summary>
		/// The alias to use instead of the field name.
		/// </summary>
		private string alias;

		/// <summary>
		/// Instead of using the field name, an optional alias can be used that
		/// matches the name of the object instantiated during markup processing.
		/// </summary>
		public string Alias
		{
			get {return alias;}
			set {alias=value;}
		}

		/// <summary>
		/// Constructor.  The instance assigned to this field must be named using
		/// the "def" prefix with the same name.
		/// </summary>
		public MyXamlAutoInitializeAttribute()
		{
			alias=null;
		}

		/// <summary>
		/// Constructor.  The instances assigned to this field must use the supplied
		/// alias name in the "def:Name=" markup.
		/// </summary>
		/// <param name="alias">The alias to use instead of the field name.</param>
		public MyXamlAutoInitializeAttribute(string alias)
		{
			this.alias=alias;
		}
	}
}
