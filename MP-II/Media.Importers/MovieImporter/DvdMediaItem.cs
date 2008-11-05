using System;
using System.Collections.Generic;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MetaData;

namespace Media.Importers.MovieImporter
{
  public class DvdMediaItem : IMediaItem
  {
    private IRootContainer _parent;
    private readonly Dictionary<string, object> _metaData;
    private readonly Uri _uri;
    private string _title;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileContainer"/> class.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="parent">The parent.</param>
    public DvdMediaItem(string folder, string title, IRootContainer parent)
    {
      _uri = new Uri(folder + @"\video_ts\video_ts.ifo");
      _title = title;
      _parent = parent;
      _metaData = new Dictionary<string, object>();
    }

    #region IMediaItem Members
    public IMetaDataMappingCollection Mapping
    {
      get
      {
        if (_parent != null)
          return _parent.Mapping;
        return null;
      }
      set
      {
      }
    }
    /// <summary>
    /// the media container in which this media item resides
    /// </summary>
    /// <value></value>
    public IRootContainer Parent
    {
      get { return _parent; }
      set
      {
        _parent = value;
      }
    }
    /// <summary>
    /// Gets a value indicating whether this item is located locally or remote
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this item is located locally; otherwise, <c>false</c>.
    /// </value>
    public bool IsLocal
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Returns the metadata of the media item.
    /// </summary>
    /// <value></value>
    public IDictionary<string, object> MetaData
    {
      get { return _metaData; }
    }

    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get
      {
        return _title;
      }
      set
      {
        _title = value;
      }
    }

    /// <summary>
    /// gets the content uri for this media item
    /// </summary>
    /// <value></value>
    public Uri ContentUri
    {
      get { return _uri; }
    }

    /// <summary>
    /// Gets or sets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath
    {
      get
      {
        return _uri.ToString();
      }
      set { }
    }
    #endregion
  }
}
