using System;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Database;
using MediaPortal.Database.Provider;

namespace Components.Database.FireBird
{
  class FireBirdDatabaseConnection : IDatabaseConnection, IDisposable
  {
    private FbConnection _connection;

    #region IDatabaseConnection Members

    /// <summary>
    /// Opens the database
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public void Open(string connectionString)
    {
      FbConnectionStringBuilder csb = new FbConnectionStringBuilder(connectionString);

      // Create the Database, if it doesn't exist yet
      if (!File.Exists(csb.Database))
      {
        FbConnection.CreateDatabase(csb.ToString());
      }

      _connection = new FbConnection(csb.ToString());
      _connection.Open();
    }

    /// <summary>
    /// Opens the database
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="systemDatabase">Do we need to open the Systen Database</param>
    public void Open(string connectionString, bool systemDatabase)
    {
      Open(connectionString);
    }

    /// <summary>
    /// Closes the database.
    /// </summary>
    public void Close()
    {
      if (_connection != null)
      {
        _connection.Close();
        _connection.Dispose();
        _connection = null;
      }
    }

    /// <summary>
    /// Gets the underlying connection.
    /// </summary>
    /// <value>The underlying connection.</value>
    public object UnderlyingConnection
    {
      get { return _connection; }
    }

    #endregion

    #region IDisposable Members

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
      if (_connection != null)
      {
        _connection.Dispose();
        _connection = null;
      }
    }
    #endregion
  }
}
