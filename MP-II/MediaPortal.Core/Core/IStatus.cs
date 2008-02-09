using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Core
{
	public interface IStatus
	{
		/// <summary>
		/// Show the actual status of the service 
		/// </summary>
		/// <returns></returns>
		List<string> GetStatus();
	}
}
