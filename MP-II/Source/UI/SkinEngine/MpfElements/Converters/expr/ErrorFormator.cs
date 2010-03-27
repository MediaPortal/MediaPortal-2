namespace Jyc.Expr
{
    static class ErrorFormator 
    {
        internal static string FormatError(Error code, int pos)
        {
            return string.Format(SR.ErrorFormat, code, pos, (int)code);
        }
    }
}
