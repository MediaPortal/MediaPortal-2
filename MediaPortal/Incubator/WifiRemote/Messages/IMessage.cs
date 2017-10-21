using System;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
    interface IMessage
    {
        /// <summary>
        /// Type is a required attribute for all messages. 
        /// The client decides by this attribute what message was sent.
        /// </summary>
        String Type
        {
            get;
        }

    }
}
