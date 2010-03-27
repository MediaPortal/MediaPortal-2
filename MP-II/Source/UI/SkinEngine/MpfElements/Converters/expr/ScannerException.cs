using System;

namespace Jyc.Expr
{
    public class ScannerException:Exception
    {
        public ScannerException(Error errorCode ) 
        {
        }

        public ScannerException(Error errorCode,int pos)
        {
        } 
    }
}
