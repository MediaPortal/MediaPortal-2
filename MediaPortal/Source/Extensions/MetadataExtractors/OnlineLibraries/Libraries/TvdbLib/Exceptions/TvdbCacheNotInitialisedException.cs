using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TvdbLib.Exceptions
{
  /// <summary>
  /// A function has been called that needs an initialised cache but the InitCache function
  /// hasn't been called yet
  /// </summary>
  public class TvdbCacheNotInitialisedException : TvdbException
  {
    /// <summary>
    /// TvdbCacheNotInitialisedException constructor
    /// </summary>
    /// <param name="_msg">Message</param>
    public TvdbCacheNotInitialisedException(String _msg)
      : base(_msg)
    {

    }

    /// <summary>
    /// TvdbCacheNotInitialisedException constructor
    /// </summary>
    public TvdbCacheNotInitialisedException()
      : base()
    {

    }
  }
}
