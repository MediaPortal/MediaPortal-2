/*
 * Copyright (c) 2004, 2005 MyXaml
 * All Rights Reserved
 * 
 * Licensed under the terms of the GNU General Public License
 * http://www.gnu.org/licenses/licenses.html#GPL
*/

using System;

namespace MyXaml
{
	public interface IMyXaml
	{
		string Name
		{
			get;
			set;
		}

		object Tag
		{
			get;
			set;
		}
	}
}

namespace MyXaml.Core
{
	/// <summary>
	/// Provides an interface to plug-ins that extend the core parser's functionality.
	/// </summary>
	public interface IMyXamlExtender
	{
		/// <summary>
		/// Registers a plug-in.  Called when the parser is constructed.
		/// </summary>
		/// <param name="parser">The parser instance.</param>
		void Register(Parser parser);

		/// <summary>
		/// Unregisters a plug-in.  Called when the parser is destroyed.
		/// </summary>
		/// <param name="parser">The parser instance.</param>
		void Unregister(Parser parser);
	}
}
