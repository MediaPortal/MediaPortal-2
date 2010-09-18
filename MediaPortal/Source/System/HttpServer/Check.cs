using System;

namespace HttpServer
{
  /// <summary>
  /// Small design by contract implementation.
  /// </summary>
  public static class Check
  {
    /// <summary>
    /// Check whether a parameter is empty.
    /// </summary>
    /// <param name="value">Parameter value</param>
    /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
    /// <exception cref="ArgumentException">value is empty.</exception>
    public static void NotEmpty(string value, string parameterOrErrorMessage)
    {
      if (!string.IsNullOrEmpty(value))
        return;

      if (parameterOrErrorMessage.IndexOf(' ') == -1)
        throw new ArgumentException("'" + parameterOrErrorMessage + "' cannot be empty.", parameterOrErrorMessage);

      throw new ArgumentException(parameterOrErrorMessage);
    }

    /// <summary>
    /// Checks whether a parameter is null.
    /// </summary>
    /// <param name="value">Parameter value</param>
    /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
    /// <exception cref="ArgumentNullException">value is null.</exception>
    public static void Require(object value, string parameterOrErrorMessage)
    {
      if (value != null)
        return;

      if (parameterOrErrorMessage.IndexOf(' ') == -1)
        throw new ArgumentNullException("'" + parameterOrErrorMessage + "' cannot be null.", parameterOrErrorMessage);

      throw new ArgumentNullException(parameterOrErrorMessage);
    }


    // Fixed by Albert, Team MediaPortal
    ///// <summary>
    ///// Checks whether a parameter is null.
    ///// </summary>
    ///// <param name="minValue"></param>
    ///// <param name="value">Parameter value</param>
    ///// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
    ///// <exception cref="ArgumentException">value is null.</exception>    //public static void Min(int minValue, object value, string parameterOrErrorMessage)
    //{
    //  if (value != null)
    //    return;

    //  if (parameterOrErrorMessage.IndexOf(' ') == -1)
    //    throw new ArgumentException(
    //        "'" + parameterOrErrorMessage + "' must be at least " + minValue + ".", parameterOrErrorMessage);

    //  throw new ArgumentException(parameterOrErrorMessage);
    //}
    /// <summary>
    /// Checks whether a parameter smaller than the given <paramref name="minValue"/>.
    /// </summary>
    /// <param name="minValue">Minimum value.</param>
    /// <param name="value">Parameter value.</param>
    /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is smaller than <paramref name="minValue"/>.</exception>
    public static void Min(int minValue, int value, string parameterOrErrorMessage)
    {
      if (value >= minValue)
        return;

      if (parameterOrErrorMessage.IndexOf(' ') == -1)
        throw new ArgumentException(
            "'" + parameterOrErrorMessage + "' must be at least " + minValue + ".", parameterOrErrorMessage);

      throw new ArgumentException(parameterOrErrorMessage);
    }
  }
}