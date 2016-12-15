#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;

namespace MediaPortal.Utilities.Network
{
	/// <summary>
	/// Type of share.
	/// </summary>
	[Flags]
	public enum ShareType
	{
		/// <summary>
		/// Disk share.
		/// </summary>
		Disk = 0,

		/// <summary>
		/// Printer share.
		/// </summary>
		Printer = 1,

		/// <summary>
		/// Device share.
		/// </summary>
		Device = 2,

		/// <summary>
		/// IPC share.
		/// </summary>
		IPC = 3,

		/// <summary>
		/// Special share.
		/// </summary>
		Special = -2147483648, // 0x80000000,
	}
	
	/// <summary>
	/// Information about a local share.
	/// </summary>
	public class Share
	{
		#region Protected fields
		
		protected readonly string _server;
		protected readonly string _netName;
		protected readonly string _path;
		protected readonly ShareType _shareType;
		protected readonly string _remark;
		
		#endregion
		
		#region Constructor

	  /// <summary>
	  /// Constructor.
	  /// </summary>
	  /// <param name="server">Server of the new share.</param>
	  /// <param name="netName">Name of the new share.</param>
	  /// <param name="path">Local path of the new share.</param>
	  /// <param name="shareType">Type of the new share.</param>
	  /// <param name="remark">Comment of the new share.</param>
	  public Share(string server, string netName, string path, ShareType shareType, string remark) 
		{
			if (ShareType.Special == shareType && "IPC$" == netName)
				shareType |= ShareType.IPC;

			if (server.StartsWith("\\\\"))
        server = server.Substring(2);
			_server = server;
			_netName = netName;
			_path = path;
			_shareType = shareType;
			_remark = remark;
		}
		
		#endregion
		
		#region Properties

		/// <summary>
		/// The name of the computer that this share belongs to, without leading <c>\\</c> characters.
		/// </summary>
		public string Server 
		{
			get { return _server; }
		}

		/// <summary>
		/// Name of this share.
		/// </summary>
		public string NetName 
		{
			get { return _netName; }
		}

		/// <summary>
		/// Local path of this share.
		/// </summary>
		public string Path 
		{
			get { return _path; }
		}

		/// <summary>
		/// Type of this share.
		/// </summary>
		public ShareType ShareType 
		{
			get { return _shareType; }
		}

		/// <summary>
		/// Comment of this share.
		/// </summary>
		public string Remark 
		{
			get { return _remark; }
		}

	  /// <summary>
	  /// Returns the path to this share.
	  /// </summary>
	  /// <returns>
	  /// UNC path of this share.
	  /// </returns>
	  public string UNCPath
	  {
	    get { return string.Format(@"\\{0}\{1}", string.IsNullOrEmpty(_server) ? Environment.MachineName : _server, _netName); }
	  }

    /// <summary>
		/// Returns true if this is a file system share.
		/// </summary>
		public bool IsFileSystem 
		{
			get 
			{
				// Shared device
				if (0 != (_shareType & ShareType.Device)) return false;
				// IPC share
				if (0 != (_shareType & ShareType.IPC)) return false;
				// Shared printer
				if (0 != (_shareType & ShareType.Printer)) return false;
				
				// Standard disk share
				if (0 == (_shareType & ShareType.Special)) return true;
				
				// Special disk share (e.g. C$)
				return ShareType.Special == _shareType && !string.IsNullOrEmpty(_netName);
			}
		}

		/// <summary>
		/// Get the root of a disk-based share.
		/// </summary>
		public DirectoryInfo Root 
		{
			get
			{
			  if (IsFileSystem)
				{
				  if (string.IsNullOrEmpty(_server))
						return string.IsNullOrEmpty(_path) ? new DirectoryInfo(ToString()) : new DirectoryInfo(_path);
				  return new DirectoryInfo(ToString());
				}
			  return null;
			}
		}
		
		#endregion

		public override string ToString()
		{
		  return UNCPath;
		}
  }
}