using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;

namespace WifiRemote
{
    class NowPlayingDVD : IAdditionalNowPlayingInfo
    {
        string mediaType = "dvd";
        public string MediaType
        {
            get { return mediaType; }
        }

        public string MpExtId
        {
            get { return null; }
        }

        public int MpExtMediaType
        {
            get { return (int)MpExtendedMediaTypes.Movie; }
        }

        public int MpExtProviderId
        {
            get { return -1; }
        }
    }
}
