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
	/// Diagnostic class that can be inserted in the markup to force the debugger
	/// to break.
	/// </summary>
	public class Break
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public Break()
		{
			System.Diagnostics.Debugger.Break();
		}
	}
}
