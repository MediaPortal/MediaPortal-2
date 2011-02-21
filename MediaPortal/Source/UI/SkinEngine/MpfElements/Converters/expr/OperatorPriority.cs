namespace Jyc.Expr
{
    public enum OperatorPriority:int
    {
        Lowest = 0,
        Conditional,
        LogicalOr,
        LogicalAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseAnd,
        Equality,
        Relational,
        Shift,
        Additive,
        Multiplicative,
        Unary,

        //Paren, 
        Indexer,
        Function,        
        //Comma,

        Member,

    }
}