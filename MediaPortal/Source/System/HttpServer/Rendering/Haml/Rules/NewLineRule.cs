namespace HttpServer.Rendering.Haml.Rules
{
    /// <summary>
    /// IRule that says that something :)
    /// </summary>
    public class NewLineRule : Rule
    {
        /// <summary>
        /// Determines if this node spans over multiple lines.
        /// </summary>
        /// <param name="line">contains line information (and text)</param>
        /// <param name="isContinued">true if the previous line was continued.</param>
        /// <returns>true if this line continues onto the next.</returns>
        public override bool IsMultiLine(LineInfo line, bool isContinued)
        {
            string trimmed = line.Data.TrimEnd();
            if (trimmed.Length == 0)
                return false;

            if (trimmed.EndsWith("|"))
            {
                line.TrimRight(1);
                return true;
            }

            return false;
        }
    }
}
