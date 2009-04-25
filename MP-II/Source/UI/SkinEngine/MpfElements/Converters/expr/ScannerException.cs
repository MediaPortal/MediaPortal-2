using System; 
using System.Text;

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
