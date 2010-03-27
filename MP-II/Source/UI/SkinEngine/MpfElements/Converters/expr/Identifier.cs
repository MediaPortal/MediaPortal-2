namespace Jyc.Expr
{
    abstract class Identifier : Operand
    {
        string _name; 

        protected Identifier(string name)
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
