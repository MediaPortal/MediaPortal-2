using System; 
using System.Text;

namespace Jyc.Expr
{
    abstract class Identifier : Operand
    {
        string _name; 

        public Identifier(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }  
    }
}
