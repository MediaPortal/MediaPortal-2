using System;
using System.Text;
using HttpServer.Exceptions;

namespace HttpServer.Parser
{
  /// <summary>
  /// Parses a HTTP request directly from a stream
  /// </summary>
  public class HttpRequestParser : IHttpRequestParser
  {
    private readonly BodyEventArgs _bodyArgs = new BodyEventArgs();
    private readonly HeaderEventArgs _headerArgs = new HeaderEventArgs();
    private ILogWriter _log;
    private readonly RequestLineEventArgs _requestLineArgs = new RequestLineEventArgs();
    private int _bodyBytesLeft;
    private string _curHeaderName = string.Empty;
    private string _curHeaderValue = string.Empty;

    /// <summary>
    /// Create a new request parser
    /// </summary>
    /// <param name="logWriter">delegate receiving log entries.</param>
    public HttpRequestParser(ILogWriter logWriter)
    {
      _log = logWriter ?? NullLogWriter.Instance;
    }

    /// <summary>
    /// Add a number of bytes to the body
    /// </summary>
    /// <param name="buffer">buffer containing more body bytes.</param>
    /// <param name="offset">starting offset in buffer</param>
    /// <param name="count">number of bytes, from offset, to read.</param>
    /// <returns>offset to continue from.</returns>
    private int AddToBody(byte[] buffer, int offset, int count)
    {
      // got all bytes we need, or just a few of them?
      int bytesUsed = count > _bodyBytesLeft ? _bodyBytesLeft : count;
      _bodyArgs.Buffer = buffer;
      _bodyArgs.Offset = offset;
      _bodyArgs.Count = bytesUsed;
      BodyBytesReceived(this, _bodyArgs);

      _bodyBytesLeft -= bytesUsed;
      if (_bodyBytesLeft == 0)
      {
        // got a complete request.
        _log.Write(this, LogPrio.Trace, "Request parsed successfully.");
        OnRequestCompleted();
        Clear();
      }

      return offset + bytesUsed;
    }

    /// <summary>
    /// Remove all state information for the request.
    /// </summary>
    public void Clear()
    {
      _bodyBytesLeft = 0;
      _curHeaderName = string.Empty;
      _curHeaderValue = string.Empty;
      CurrentState = RequestParserState.FirstLine;
    }

    /// <summary>
    /// Gets or sets the log writer.
    /// </summary>
    public ILogWriter LogWriter
    {
      get { return _log; }
      set { _log = value ?? NullLogWriter.Instance; }
    }

    /// <summary>
    /// Parse request line
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="BadRequestException">If line is incorrect</exception>
    /// <remarks>Expects the following format: "Method SP Request-URI SP HTTP-Version CRLF"</remarks>
    protected void OnFirstLine(string value)
    {
      //
      //todo: In the interest of robustness, servers SHOULD ignore any empty line(s) received where a Request-Line is expected. 
      // In other words, if the server is reading the protocol stream at the beginning of a message and receives a CRLF first, it should ignore the CRLF.
      //
      _log.Write(this, LogPrio.Debug, "Got request: " + value);

      //Request-Line   = Method SP Request-URI SP HTTP-Version CRLF
      int pos = value.IndexOf(' ');
      if (pos == -1 || pos + 1 >= value.Length)
      {
        _log.Write(this, LogPrio.Warning, "Invalid request line, missing Method. Line: " + value);
        throw new BadRequestException("Invalid request line, missing Method. Line: " + value);
      }

      string method = value.Substring(0, pos).ToUpper();
      int oldPos = pos + 1;
      pos = value.IndexOf(' ', oldPos);
      if (pos == -1)
      {
        _log.Write(this, LogPrio.Warning, "Invalid request line, missing URI. Line: " + value);
        throw new BadRequestException("Invalid request line, missing URI. Line: " + value);
      }
      string path = value.Substring(oldPos, pos - oldPos);
      if (path.Length > 4196)
        throw new BadRequestException("Too long URI.");

      if (pos + 1 >= value.Length)
      {
        _log.Write(this, LogPrio.Warning, "Invalid request line, missing HTTP-Version. Line: " + value);
        throw new BadRequestException("Invalid request line, missing HTTP-Version. Line: " + value);
      }
      string version = value.Substring(pos + 1);
      if (version.Length < 4 || string.Compare(version.Substring(0, 4), "HTTP", true) != 0)
      {
        _log.Write(this, LogPrio.Warning, "Invalid HTTP version in request line. Line: " + value);
        throw new BadRequestException("Invalid HTTP version in Request line. Line: " + value);
      }

      _requestLineArgs.HttpMethod = method;
      _requestLineArgs.HttpVersion = version;
      _requestLineArgs.UriPath = path;
      RequestLineReceived(this, _requestLineArgs);
    }

    /// <summary>
    /// We've parsed a new header.
    /// </summary>
    /// <param name="name">Name in lower case</param>
    /// <param name="value">Value, unmodified.</param>
    /// <exception cref="BadRequestException">If content length cannot be parsed.</exception>
    protected void OnHeader(string name, string value)
    {
      _headerArgs.Name = name;
      _headerArgs.Value = value;
      if (string.Compare(name, "content-length", true) == 0)
      {
        if (!int.TryParse(value, out _bodyBytesLeft))
          throw new BadRequestException("Content length is not a number.");
      }

      HeaderReceived(this, _headerArgs);
    }

    private void OnRequestCompleted()
    {
      RequestCompleted(this, EventArgs.Empty);
    }

    #region IHttpRequestParser Members

    /// <summary>
    /// Current state in parser.
    /// </summary>
    public RequestParserState CurrentState { get; private set; }

    /// <summary>
    /// Parse a message
    /// </summary>
    /// <param name="buffer">bytes to parse.</param>
    /// <param name="offset">where in buffer that parsing should start</param>
    /// <param name="count">number of bytes to parse, starting on <paramref name="offset"/>.</param>
    /// <returns>offset (where to start parsing next).</returns>
    /// <exception cref="BadRequestException"><c>BadRequestException</c>.</exception>
    public int Parse(byte[] buffer, int offset, int count)
    {
      // add body bytes
      if (CurrentState == RequestParserState.Body)
      {
        // copy all remaining bytes to the beginning of the buffer.
        //Buffer.BlockCopy(buffer, offset + bytesUsed, buffer, 0, count - bytesUsed);


        return AddToBody(buffer, 0, count);
      }

#if DEBUG
      string temp = Encoding.ASCII.GetString(buffer, offset, count);
      _log.Write(this, LogPrio.Trace, "\r\n\r\n HTTP MESSAGE: " + temp + "\r\n");
#endif

      int currentLine = 1;
      int startPos = -1;

      // set start pos since this is from an partial request
      if (CurrentState == RequestParserState.HeaderValue)
        startPos = 0;

      int endOfBufferPos = offset + count;

      //<summary>
      // Handled bytes are used to keep track of the number of bytes processed.
      // We do this since we can handle partial requests (to be able to check headers and abort
      // invalid requests directly without having to process the whole header / body).
      // </summary>
      int handledBytes = 0;


      for (int currentPos = offset; currentPos < endOfBufferPos; ++currentPos)
      {
        var ch = (char) buffer[currentPos];
        char nextCh = endOfBufferPos > currentPos + 1 ? (char) buffer[currentPos + 1] : char.MinValue;

        if (ch == '\r')
          ++currentLine;

        switch (CurrentState)
        {
          case RequestParserState.FirstLine:
            if (currentPos > 4196)
            {
              _log.Write(this, LogPrio.Warning, "HTTP Request is too large.");
              throw new BadRequestException("Too large request line.");
            }
            if (char.IsLetterOrDigit(ch) && startPos == -1)
              startPos = currentPos;
            if (startPos == -1 && (ch != '\r' || nextCh != '\n'))
            {
              _log.Write(this, LogPrio.Warning, "Request line is not found.");
              throw new BadRequestException("Invalid request line.");
            }
            if (startPos != -1 && (ch == '\r' || ch == '\n'))
            {
              int size = GetLineBreakSize(buffer, currentPos);
              OnFirstLine(Encoding.UTF8.GetString(buffer, startPos, currentPos - startPos));
              CurrentState = CurrentState + 1;
              currentPos += size - 1;
              handledBytes = currentPos + size - 1;
              startPos = -1;
            }
            break;
          case RequestParserState.HeaderName:
            if (ch == '\r' || ch == '\n')
            {
              currentPos += GetLineBreakSize(buffer, currentPos);
              if (_bodyBytesLeft == 0)
              {
                CurrentState = RequestParserState.FirstLine;
                _log.Write(this, LogPrio.Trace, "Request parsed successfully (no content).");
                OnRequestCompleted();
                Clear();
                return currentPos;
              }

              CurrentState = RequestParserState.Body;
              if (currentPos + 1 < endOfBufferPos)
              {
                _log.Write(this, LogPrio.Trace, "Adding bytes to the body");
                return AddToBody(buffer, currentPos, endOfBufferPos - currentPos);
              }

              return currentPos;
            }
            if (char.IsWhiteSpace(ch) || ch == ':')
            {
              if (startPos == -1)
              {
                _log.Write(
                    this, LogPrio.Warning,
                    "Expected header name, got colon on line " + currentLine);
                throw new BadRequestException("Expected header name, got colon on line " + currentLine);
              }
              _curHeaderName = Encoding.UTF8.GetString(buffer, startPos, currentPos - startPos);
              handledBytes = currentPos + 1;
              startPos = -1;
              CurrentState = CurrentState + 1;
              if (ch == ':')
                CurrentState = CurrentState + 1;
            }
            else if (startPos == -1)
              startPos = currentPos;
            else if (!char.IsLetterOrDigit(ch) && ch != '-')
            {
              _log.Write(this, LogPrio.Warning, "Invalid character in header name on line " + currentLine);
              throw new BadRequestException("Invalid character in header name on line " + currentLine);
            }
            if (startPos != -1 && currentPos - startPos > 200)
            {
              _log.Write(this, LogPrio.Warning, "Invalid header name on line " + currentLine);
              throw new BadRequestException("Invalid header name on line " + currentLine);
            }
            break;
          case RequestParserState.AfterName:
            if (ch == ':')
            {
              handledBytes = currentPos + 1;
              CurrentState = CurrentState + 1;
            }
            break;
          case RequestParserState.Between:
            {
              if (ch == ' ' || ch == '\t')
                continue;
              int newLineSize = GetLineBreakSize(buffer, currentPos);
              if (newLineSize > 0 && currentPos + newLineSize < endOfBufferPos &&
                  char.IsWhiteSpace((char) buffer[currentPos + newLineSize]))
              {
                ++currentPos;
                continue;
              }
              startPos = currentPos;
              CurrentState = CurrentState + 1;
              handledBytes = currentPos;
              continue;
            }
          case RequestParserState.HeaderValue:
            {
              if (ch != '\r' && ch != '\n')
                continue;
              int newLineSize = GetLineBreakSize(buffer, currentPos);
              if (startPos == -1)
                continue; // allow new lines before start of value

              if (_curHeaderName == string.Empty)
                throw new BadRequestException("Missing header on line " + currentLine);
              if (startPos == -1)
              {
                _log.Write(this, LogPrio.Warning, "Missing header value for '" + _curHeaderName);
                throw new BadRequestException("Missing header value for '" + _curHeaderName);
              }
              if (currentPos - startPos > 1024)
              {
                _log.Write(this, LogPrio.Warning, "Too large header value on line " + currentLine);
                throw new BadRequestException("Too large header value on line " + currentLine);
              }

              // Header fields can be extended over multiple lines by preceding each extra line with at
              // least one SP or HT.
              if (endOfBufferPos > currentPos + newLineSize
                  && (buffer[currentPos + newLineSize] == ' ' || buffer[currentPos + newLineSize] == buffer['\t']))
              {
                if (startPos != -1)
                  _curHeaderValue = Encoding.UTF8.GetString(buffer, startPos, currentPos - startPos);

                _log.Write(this, LogPrio.Trace, "Header value is on multiple lines.");
                CurrentState = RequestParserState.Between;
                startPos = -1;
                currentPos += newLineSize - 1;
                handledBytes = currentPos + newLineSize - 1;
                continue;
              }

              _curHeaderValue += Encoding.UTF8.GetString(buffer, startPos, currentPos - startPos);
              _log.Write(this, LogPrio.Trace, "Header [" + _curHeaderName + ": " + _curHeaderValue + "]");
              OnHeader(_curHeaderName, _curHeaderValue);

              startPos = -1;
              CurrentState = RequestParserState.HeaderName;
              _curHeaderValue = string.Empty;
              _curHeaderName = string.Empty;
              ++currentPos;
              handledBytes = currentPos + 1;

              // Check if we got a colon so we can cut header name, or crlf for end of header.
              bool canContinue = false;
              for (int j = currentPos; j < endOfBufferPos; ++j)
              {
                if (buffer[j] != ':' && buffer[j] != '\r' && buffer[j] != '\n') continue;
                canContinue = true;
                break;
              }
              if (!canContinue)
              {
                _log.Write(this, LogPrio.Trace, "Cant continue, no colon.");
                return currentPos + 1;
              }
            }
            break;
        }
      }

      return handledBytes;
    }

    private int GetLineBreakSize(byte[] buffer, int offset)
    {
      if (buffer[offset] != '\r' && buffer[offset] != '\n')
        return 0;

      // linux line feed
      if (buffer[offset] == '\n' && (buffer.Length == offset + 1 || buffer[offset + 1] != '\r'))
        return 1;

      // win line feed
      if (buffer[offset] == '\r' && buffer.Length > offset + 1 && buffer[offset + 1] == '\n')
        return 2;

      if (buffer[offset] == '\n' && buffer.Length > offset + 1 && buffer[offset + 1] == '\r')
        return 2;

      throw new BadRequestException("Got invalid linefeed.");
    }

    /// <summary>
    /// A request have been successfully parsed.
    /// </summary>
    public event EventHandler RequestCompleted = delegate { };

    /// <summary>
    /// More body bytes have been received.
    /// </summary>
    public event EventHandler<BodyEventArgs> BodyBytesReceived = delegate { };

    /// <summary>
    /// Request line have been received.
    /// </summary>
    public event EventHandler<RequestLineEventArgs> RequestLineReceived = delegate { };

    /// <summary>
    /// A header have been received.
    /// </summary>
    public event EventHandler<HeaderEventArgs> HeaderReceived = delegate { };

    #endregion
  }
}