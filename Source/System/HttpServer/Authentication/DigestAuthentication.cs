using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HttpServer.Authentication
{
  /// <summary>
  /// Implements HTTP Digest authentication. It's more secure than Basic auth since password is 
  /// encrypted with a "key" from the server. 
  /// </summary>
  /// <remarks>
  /// Keep in mind that the password is encrypted with MD5. Use a combination of SSL and digest auth to be secure.
  /// </remarks>
  public class DigestAuthentication : AuthenticationModule
  {
    private static readonly Dictionary<string, DateTime> _nonces = new Dictionary<string, DateTime>();
    private static Timer _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DigestAuthentication"/> class.
    /// </summary>
    /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
    /// <param name="authenticationRequiredHandler">Delegate used to determine if authentication is required (may be null).</param>
    public DigestAuthentication(
        AuthenticationHandler authenticator, AuthenticationRequiredHandler authenticationRequiredHandler)
        : base(authenticator, authenticationRequiredHandler)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DigestAuthentication"/> class.
    /// </summary>
    /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
    public DigestAuthentication(AuthenticationHandler authenticator)
        : base(authenticator)
    {
    }

    /// <summary>
    /// Used by test classes to be able to use hardcoded values
    /// </summary>
    public static bool DisableNonceCheck;

    /// <summary>
    /// name used in http request.
    /// </summary>
    public override string Name
    {
      get { return "digest"; }
    }

    /// <summary>
    /// An authentication response have been received from the web browser.
    /// Check if it's correct
    /// </summary>
    /// <param name="authenticationHeader">Contents from the Authorization header</param>
    /// <param name="realm">Realm that should be authenticated</param>
    /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
    /// <param name="options">First option: true if username/password is correct but not cnonce</param>
    /// <returns>
    /// Authentication object that is stored for the request. A user class or something like that.
    /// </returns>
    /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
    /// <exception cref="ArgumentNullException">If any of the paramters is empty or null.</exception>
    public override object Authenticate(string authenticationHeader, string realm, string httpVerb, object[] options)
    {
      lock (_nonces)
      {
        if (_timer == null)
          _timer = new Timer(ManageNonces, null, 15000, 15000);
      }

      if (!authenticationHeader.StartsWith("Digest", true, CultureInfo.CurrentCulture))
        return null;

      bool staleNonce;
      if (options.Length > 0)
        staleNonce = (bool) options[0];
      else staleNonce = false;

      NameValueCollection reqInfo = Decode(authenticationHeader, Encoding.UTF8);
      if (!IsValidNonce(reqInfo["nonce"]) && !DisableNonceCheck)
        return null;

      string username = reqInfo["username"];
      string password = string.Empty;
      object state;

      if (!CheckAuthentication(realm, username, ref password, out state))
        return null;

      string HA1;
      if (!TokenIsHA1)
      {
        string A1 = String.Format("{0}:{1}:{2}", username, realm, password);
        HA1 = GetMD5HashBinHex2(A1);
      }
      else
        HA1 = password;

      string A2 = String.Format("{0}:{1}", httpVerb, reqInfo["uri"]);
      string HA2 = GetMD5HashBinHex2(A2);
      string hashedDigest = Encrypt(
          HA1, HA2, reqInfo["qop"],
          reqInfo["nonce"], reqInfo["nc"], reqInfo["cnonce"]);

      if (reqInfo["response"] == hashedDigest && !staleNonce)
        return state;

      return null;
    }

    /// <summary>
    /// Gets or sets whether the token supplied in <see cref="AuthenticationHandler"/> is a
    /// HA1 generated string.
    /// </summary>
    public bool TokenIsHA1 { get; set; }

    /// <summary>
    /// Encrypts parameters into a Digest string
    /// </summary>
    /// <param name="realm">Realm that the user want to log into.</param>
    /// <param name="userName">User logging in</param>
    /// <param name="password">Users password.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="uri">Uri/domain that generated the login prompt.</param>
    /// <param name="qop">Quality of Protection.</param>
    /// <param name="nonce">"Number used ONCE"</param>
    /// <param name="nc">Hexadecimal request counter.</param>
    /// <param name="cnonce">"Client Number used ONCE"</param>
    /// <returns>Digest encrypted string</returns>
    public static string Encrypt(
        string realm, string userName, string password, string method, string uri, string qop, string nonce, string nc,
        string cnonce)
    {
      string A1 = String.Format("{0}:{1}:{2}", userName, realm, password);
      string HA1 = GetMD5HashBinHex2(A1);
      string A2 = String.Format("{0}:{1}", method, uri);
      string HA2 = GetMD5HashBinHex2(A2);

      string unhashedDigest;
      if (qop != null)
      {
        unhashedDigest = String.Format(
            "{0}:{1}:{2}:{3}:{4}:{5}",
            HA1,
            nonce,
            nc,
            cnonce,
            qop,
            HA2);
      }
      else
      {
        unhashedDigest = String.Format(
            "{0}:{1}:{2}",
            HA1,
            nonce,
            HA2);
      }

      return GetMD5HashBinHex2(unhashedDigest);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ha1">Md5 hex encoded "userName:realm:password", without the quotes.</param>
    /// <param name="ha2">Md5 hex encoded "method:uri", without the quotes</param>
    /// <param name="qop">Quality of Protection</param>
    /// <param name="nonce">"Number used ONCE"</param>
    /// <param name="nc">Hexadecimal request counter.</param>
    /// <param name="cnonce">Client number used once</param>
    /// <returns></returns>
    protected virtual string Encrypt(string ha1, string ha2, string qop, string nonce, string nc, string cnonce)
    {
      string unhashedDigest;
      if (qop != null)
      {
        unhashedDigest = String.Format(
            "{0}:{1}:{2}:{3}:{4}:{5}",
            ha1,
            nonce,
            nc,
            cnonce,
            qop,
            ha2);
      }
      else
      {
        unhashedDigest = String.Format(
            "{0}:{1}:{2}",
            ha1,
            nonce,
            ha2);
      }

      return GetMD5HashBinHex2(unhashedDigest);
    }

    private static void ManageNonces(object state)
    {
      lock (_nonces)
      {
        foreach (KeyValuePair<string, DateTime> pair in _nonces)
        {
          if (pair.Value >= DateTime.Now)
            continue;

          _nonces.Remove(pair.Key);
          return;
        }
      }
    }


    /// <summary>
    /// Create a response that can be sent in the WWW-Authenticate header.
    /// </summary>
    /// <param name="realm">Realm that the user should authenticate in</param>
    /// <param name="options">First options specifies if true if username/password is correct but not cnonce.</param>
    /// <returns>A correct auth request.</returns>
    /// <exception cref="ArgumentNullException">If realm is empty or null.</exception>
    public override string CreateResponse(string realm, object[] options)
    {
      string nonce = GetCurrentNonce();

      StringBuilder challenge = new StringBuilder("Digest realm=\"");
      challenge.Append(realm);
      challenge.Append("\"");
      challenge.Append(", nonce=\"");
      challenge.Append(nonce);
      challenge.Append("\"");
      challenge.Append(", opaque=\"" + Guid.NewGuid().ToString().Replace("-", string.Empty) + "\"");
      challenge.Append(", stale=");

      if (options.Length > 0)
        challenge.Append((bool) options[0] ? "true" : "false");
      else
        challenge.Append("false");

      challenge.Append(", algorithm=MD5");
      challenge.Append(", qop=auth");

      return challenge.ToString();
    }

    /// <summary>
    /// Decodes authorization header value
    /// </summary>
    /// <param name="buffer">header value</param>
    /// <param name="encoding">Encoding that the buffer is in</param>
    /// <returns>All headers and their values if successful; otherwise null</returns>
    /// <example>
    /// NameValueCollection header = DigestAuthentication.Decode("response=\"6629fae49393a05397450978507c4ef1\",\r\nc=00001", Encoding.ASCII);
    /// </example>
    /// <remarks>Can handle lots of whitespaces and new lines without failing.</remarks>
    public static NameValueCollection Decode(string buffer, Encoding encoding)
    {
      if (string.Compare(buffer.Substring(0, 7), "Digest ", true) == 0)
        buffer = buffer.Remove(0, 7).Trim(' ');

      NameValueCollection values = new NameValueCollection();
      int step = 0;
      bool inQuote = false;
      string name = string.Empty;
      int start = 0;
      for (int i = start; i < buffer.Length; ++i)
      {
        char ch = buffer[i];
        if (ch == '"')
          inQuote = !inQuote;

        //find start of name
        switch (step)
        {
          case 0:
            if (!char.IsWhiteSpace(ch))
            {
              if (!char.IsLetterOrDigit(ch) && ch != '"')
                return null;
              start = i;
              ++step;
            }
            break;
          case 1:
            if (char.IsWhiteSpace(ch) || ch == '=')
            {
              if (start == -1)
                return null;
              name = buffer.Substring(start, i - start);
              start = -1;
              ++step;
            }
            else if (!char.IsLetterOrDigit(ch) && ch != '"')
              return null;
            break;
          case 2:
            if (!char.IsWhiteSpace(ch) && ch != '=')
            {
              start = i;
              ++step;
            }
            break;
        }
        // find end of value
        if (step == 3)
        {
          if (inQuote)
            continue;

          if (ch == ',' || char.IsWhiteSpace(ch) || i == buffer.Length - 1)
          {
            if (start == -1)
              return null;

            int stop = i;
            if (buffer[start] == '"')
            {
              ++start;
              --stop;
            }
            if (i == buffer.Length - 1 || (i == buffer.Length - 2 && buffer[buffer.Length - 1] == '"'))
              ++stop;

            values.Add(name.ToLower(), buffer.Substring(start, stop - start));
            name = string.Empty;
            start = -1;
            step = 0;
          }
        }
      }

      return values.Count == 0 ? null : values;
    }

    /// <summary>
    /// Gets the current nonce.
    /// </summary>
    /// <returns></returns>
    protected virtual string GetCurrentNonce()
    {
      string nonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
      lock (_nonces)
        _nonces.Add(nonce, DateTime.Now.AddSeconds(30));

      return nonce;
    }

    /// <summary>
    /// Gets the Md5 hash bin hex2.
    /// </summary>
    /// <param name="toBeHashed">To be hashed.</param>
    /// <returns></returns>
    public static string GetMD5HashBinHex2(string toBeHashed)
    {
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] result = md5.ComputeHash(Encoding.ASCII.GetBytes(toBeHashed));

      StringBuilder sb = new StringBuilder();
      foreach (byte b in result)
        sb.Append(b.ToString("x2"));
      return sb.ToString();
    }

    /// <summary>
    /// determines if the nonce is valid or has expired.
    /// </summary>
    /// <param name="nonce">nonce value (check wikipedia for info)</param>
    /// <returns>true if the nonce has not expired.</returns>
    protected virtual bool IsValidNonce(string nonce)
    {
      lock (_nonces)
      {
        if (_nonces.ContainsKey(nonce))
        {
          if (_nonces[nonce] < DateTime.Now)
          {
            _nonces.Remove(nonce);
            return false;
          }

          return true;
        }
      }

      return false;
    }
  }
}