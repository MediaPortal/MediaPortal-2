using System;

namespace HttpServer.Sessions
{
    /// <summary>
    /// The session store is used to manage sessions, the store itself
    /// can save it to any place it like as a database, memory or something else.
    /// </summary>
    public interface HttpSessionStore
    {
        /// <summary>
        /// Load a session from the store
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns>null if session is not found.</returns>
        HttpSession this[string sessionId] { get; }

        /// <summary>
        /// Number of minutes before a session expires.
        /// Default is 20 minutes.
        /// </summary>
        int ExpireTime { get; set; }

        /// <summary>
        /// Creates a new http session
        /// </summary>
        /// <returns>A HttpSession object</returns>
        HttpSession Create();

        /// <summary>
        /// Creates a new http session with a specific id
        /// </summary>
        /// <returns>A HttpSession object.</returns>
        HttpSession Create(string id);

        /// <summary>
        /// Load an existing session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns>A session if found; otherwise null.</returns>
        HttpSession Load(string sessionId);

        /// <summary>
        /// Save an updated session to the store.
        /// </summary>
        /// <param name="session"></param>
        /// <exception cref="ArgumentException">If Id property have not been specified.</exception>
        void Save(HttpSession session);

        /// <summary>
        /// We use the flyweight pattern which reuses small objects
        /// instead of creating new each time.
        /// </summary>
        /// <param name="session">EmptyLanguageNode (unused) session that should be reused next time Create is called.</param>
        void AddUnused(HttpSession session);

        /// <summary>
        /// Remove expired sessions
        /// </summary>
        void Cleanup();
    }
}