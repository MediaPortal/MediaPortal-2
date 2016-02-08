using MediaPortal.Plugins.WifiRemote.Utils;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
    /// <summary>
    /// Sends a screenshot to the client that requested it with the
    /// screenshot command.
    /// </summary>
    class MessageScreenshot : IMessage
    {
        public string Type
        {
            get { return "screenshot"; }
        }

        byte[] screenshot = new byte[0];
        /// <summary>
        /// The requested screenshot as byte array
        /// </summary>
        public byte[] Screenshot
        {
            get;
            set;
        }

        public ImageHelperError Error { get; set; }
    }
}
