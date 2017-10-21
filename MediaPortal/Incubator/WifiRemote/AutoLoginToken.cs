using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WifiRemote
{
    class AutoLoginToken
    {
        public String Key { get; set; }
        public DateTime Issued { get; set; }
        public RemoteClient Client { get; set; }

        public AutoLoginToken(string key, RemoteClient client)
        {
            Key = key;
            Issued = DateTime.Now;
            Client = client;
        }
    }
}
