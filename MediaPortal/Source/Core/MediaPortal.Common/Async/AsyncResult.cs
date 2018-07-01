namespace MediaPortal.Common.Async
{
  /// <summary>
  /// Wrapper class for common async method calls. It contains the <see cref="Success"/> return values and one additional <see cref="Result"/>.
  /// It is used to transform methods with out parameters more easy into async pattern where "out" and "ref" are not possible. 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class AsyncResult<T>
  {
    public AsyncResult() { }

    public AsyncResult(bool success, T result)
    {
      Success = success;
      Result = result;
    }
    /// <summary>
    /// Returns <c>true</c> if successful.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Returns <c>true</c> if successful.
    /// </summary>
    public T Result { get; set; }
  }
}
