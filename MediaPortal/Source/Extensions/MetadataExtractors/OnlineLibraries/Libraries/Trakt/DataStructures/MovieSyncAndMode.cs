using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Class used to pass syncdata and sync mode to background worker
    /// </summary>
    class MovieSyncAndMode
    {
        public TraktMovieSync SyncData { get; set; }
        public TraktSyncModes Mode { get; set; }
    }
}
