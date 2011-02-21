namespace HttpServer.Rendering.Haml.Rules
{
    /// <summary>
    /// Rules are used during preparse
    /// </summary>
    public abstract class Rule
    {
        /// <summary>
        /// Determines if this node spans over multiple lines.
        /// </summary>
        /// <param name="line">contains line information (and text)</param>
        /// <param name="isContinued">true if rule have previously inited a multiline.</param>
        /// <returns>true if this line continues onto the next.</returns>/// 
        public abstract bool IsMultiLine(LineInfo line, bool isContinued);
    }
}