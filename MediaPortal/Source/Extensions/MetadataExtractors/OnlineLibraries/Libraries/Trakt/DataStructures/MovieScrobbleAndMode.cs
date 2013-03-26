using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Class to pass scrobbling data and state to background worker
    /// </summary>
    class MovieScrobbleAndMode
    {
        public TraktMovieScrobble MovieScrobble { get; set; }
        public TraktScrobbleStates ScrobbleState { get; set; }
    }
}
