using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Core
{
	public interface IServiceInfo
	{
		/// <summary>
		/// Log Service Informations in case of a Crash 
		/// </summary>
		/// <param name="writer"></param>
		void ServiceInfo(TextWriter writer);
	}
}
