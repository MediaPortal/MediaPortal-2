using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deusty.Net;

namespace WifiRemote
{
    /// <summary>
    /// Extends the AsyncSocket class so additional properties can
    /// be added to the class AsyncSocket without changing the code of
    /// this class (so we can update the class later on)
    /// </summary>
    public static class AsyncSocketExtension
    {
        /// <summary>
        /// Get the remote client associated with the socket
        /// </summary>
        /// <param name="socket">socket</param>
        /// <returns>remote client</returns>
        public static RemoteClient GetRemoteClient(this AsyncSocket socket)
        {
            return remoteClient;
        }

        /// <summary>
        /// Sets the remote client associated with the socket
        /// </summary>
        /// <param name="socket">socket</param>
        /// <param name="client">remote clien</param>
        public static void SetRemoteClient(this AsyncSocket socket, RemoteClient client)
        {
            remoteClient = client;
        }

        /// <summary>
        /// Remote client associated with this socket
        /// </summary>
        public static RemoteClient remoteClient { get; set; }
    }
}
