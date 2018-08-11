#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Collections.Generic;
using Deusty.Net;

namespace MediaPortal.Plugins.WifiRemote
{
    public class RemoteClient
    {
        /// <summary>
        /// The socket that handles communication
        /// </summary>
        public AsyncSocket Socket { get; set; }

        /// <summary>
        /// List of properties to which this client has subscribed. If any of these properties changes,
        /// the client will get notified
        /// </summary>
        public List<String> Properties { get; set; }

        /// <summary>
        /// Username for client authentification
        /// </summary>
        public String User { get; set; }

        /// <summary>
        /// Password for client authentification
        /// </summary>
        public String Password { get; set; }

        /// <summary>
        /// Passcode for client authentification
        /// </summary>
        public String PassCode { get; set; }

        /// <summary>
        /// By which method did this client login
        /// </summary>
        public AuthMethod AuthenticatedBy { get; set; }

        /// <summary>
        /// Is the client already authentificated
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Name of the client
        /// </summary>
        public String ClientName { get; set; }

        /// <summary>
        /// Description of the client
        /// </summary>
        public String ClientDescription { get; set; }

        /// <summary>
        /// Name of the client application (CouchPotato, aMPdroid, ...)
        /// </summary>
        public String ApplicationName { get; set; }

        /// <summary>
        /// Version of the client application
        /// </summary>
        public String ApplicationVersion { get; set; }

        public RemoteClient()
        {
            ClientName = "Unknown";
            ClientDescription = String.Empty;
            ApplicationName = String.Empty;
            ApplicationVersion = String.Empty;
        }

        /// <summary>
        /// Custom ToString() method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string output = ClientName;
            if (ApplicationName != String.Empty)
            {
                output += " [" + ApplicationName;
                if (ApplicationVersion != String.Empty)
                {
                    output += " (version " + ApplicationVersion + ")";
                }
                output += "]";
            }

            return output;
        }
    }
}
