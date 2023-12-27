namespace Cinema.OnlineLibraries
{
  /// <summary>
  ///     Delegate for Exceptions
  /// </summary>
  /// <param name="sender">Sender</param>
  /// <param name="exception">The received Exception/></param>
  public delegate void ExceptionHandlerEvent(object sender, System.Exception exception);

  public class ExceptionHandler
  {
    private static ExceptionHandler _instance;

    /// <summary>
    ///     Current Instance
    /// </summary>
    public static ExceptionHandler Instance => _instance ?? (_instance = new ExceptionHandler());

    /// <summary>
    ///     A new Exception is received
    /// </summary>
    public event ExceptionHandlerEvent ExceptionReceived;

    /// <summary>
    ///     Creates a new ExceptionHandler
    /// </summary>
    public ExceptionHandler()
    {
      _instance = this;
    }

    /// <summary>
    ///     Receive a new Exception
    /// </summary>
    /// <param name="ex">Exception</param>
    public void NewExceptionReceived(System.Exception ex)
    {
      ExceptionReceived?.Invoke(new object(), ex);
    }
  }

  public delegate void EventhandlerEvent(object sender, string message);
  public class Eventhandler
  {
    public static Eventhandler _instance; 

    public static Eventhandler Instance => _instance ?? (_instance = new Eventhandler());

    public event EventhandlerEvent MessageReceived;

    public Eventhandler()
    {
      _instance = this;
    }

    public void NewMessageReceived(string message)
    {
      MessageReceived?.Invoke(new object(), message);
    }
  }
}
