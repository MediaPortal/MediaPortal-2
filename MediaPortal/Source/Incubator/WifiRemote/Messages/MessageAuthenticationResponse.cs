using System;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
    public class MessageAuthenticationResponse : IMessage
    {
        public MessageAuthenticationResponse(bool success)
        {
            this.Success = success;
        }
        
        public String Type
        {
            get { return "authenticationresponse"; }
        }

        /// <summary>
        /// Indicator if authentification was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error messsage in case authentification failed
        /// </summary>
        public String ErrorMessage { get; set; }
        
        /// <summary>
        /// Key used to autologin
        /// </summary>
        public String AutologinKey { get; set; }

    }
}
