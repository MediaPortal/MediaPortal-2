namespace Jyc.Expr
{
    public enum Error
    {
        NoError=0,
        InternalError,

        UnrecogniseChar,
        NotHexChar ,// Not a hexadcimal char
        ErrorFloatFormat,
        ErrorIntFormat,
        ErrorDoubleFormat,
        ErrorDateTimeFormat,
        PoundExpected,
        EmptyCharacterLiteral,
        StringNotEnded,
        ParameterAccessNotEnded,
        IdentfierConstUnaryOrLeftParenExpected,//Identfier,Const,Unary Or LeftParen Expected
        BinaryCommaMemberParenIndexerExpected,
        BinaryCommaRightParenRightIndexerExpected,
        ParenNotMatch,
        IndentiferExpected,
        NoParenBefore,
        NotSupportGlobalFunction,


        VariableNotExist,

        // scanner
        IllegalChar, 
        IllegalEscapeChar,
        IllegalHexCharInString,

        CharNotEnd,
        IllegalHexCharInChar,

        // parser
        IllegalTockenSeq,
        ParenthesisRightExpected,
        IndexingEndExpected,
        BinaryOpNotEnd,
        MemberNameExpected,
    }
}
