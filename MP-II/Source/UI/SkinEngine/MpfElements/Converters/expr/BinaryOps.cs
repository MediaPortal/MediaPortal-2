using System;

namespace Jyc.Expr
{
    #region subclass

    abstract class BooleanOp : BinaryOp
    {
        protected override bool IsEvalableType(Type type)
        {
            if (type == typeof(bool))
                return true;
            return false;
        }
        protected override void ProcessArg(Evaluator evaluater, Result[] argArray)
        {
            base.ProcessArg(evaluater, argArray);
        }
    }

    class BooleanOr : BooleanOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.LogicalOr; }
        }

        public override string ToString()
        {
            return "||";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            return new Result(typeof(bool), (bool)argArray[0].Value || (bool)argArray[1].Value);
        }
    }

    class BooleanAnd : BooleanOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.LogicalAnd; }
        }

        public override string ToString()
        {
            return "&&";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            return new Result(typeof(bool), (bool)argArray[0].Value && (bool)argArray[1].Value);
        }
    }

    [Flags]
    enum CompareResult
    {
        Greater = 1,
        Equal = 2,
        Less = 4
    }

    class CompareOp : BinaryOp
    {
        CompareResult _compareResult;
        public CompareOp(CompareResult compareResult)
        {
            _compareResult = compareResult;
        }

        public override OperatorPriority Priority
        {
            get 
            {
                if (_compareResult == CompareResult.Equal)
                    return OperatorPriority.Equality;
                return OperatorPriority.Relational; 
            }
        }

        protected override bool IsEvalableType(Type type)
        {
            return true;
        }

        public override string ToString()
        {
            return "compare";
        }

        protected override void ProcessArg(Evaluator evaluater, Result[] argArray)
        {
            base.ProcessArg(evaluater, argArray);
            Type type0 = argArray[0].Type;
            Type type1 = argArray[1].Type;
            if (type0 == null && type1 == null)
            {
                return;
            }

            if (type0 == null)
            {
                if (type1.IsValueType)
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type1.Name));
                }
            }
            if (type1 == null)
            {
                if (type0.IsValueType)
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type0.Name));
                }
            }

            int result;
            if (BinaryHelper.ComparePrimitiveType(type0, type1, out result))
            {
                if (result > 0)
                {
                    argArray[1].Value = ConvertHelper.ChangeType(argArray[1].Value, type0);
                }
                else if (result < 0)
                {
                    argArray[0].Value = ConvertHelper.ChangeType(argArray[0].Value, type1);
                }
            }
        }

        Result ProcessResult(int result)
        {
            if (result > 0)
            {
                if ((_compareResult & CompareResult.Greater) != 0)
                    return new Result(typeof(bool), true);
                else
                    return new Result(typeof(bool), false);
            }
            else if (result == 0)
            {
                if ((_compareResult & CompareResult.Equal) != 0)
                    return new Result(typeof(bool), true);
                else
                    return new Result(typeof(bool), false);
            }
            else
            {
                if ((_compareResult & CompareResult.Less) != 0)
                    return new Result(typeof(bool), true);
                else
                    return new Result(typeof(bool), false);
            }
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            int result = 0;
            if (argArray[0].IsNull() && argArray[1].IsNull())
            {
                return ProcessResult(0);
            }
            IComparable cb0 = argArray[0].Value as IComparable;
            IComparable cb1 = argArray[1].Value as IComparable;
            if (cb0 != null)
            {
                result = cb0.CompareTo(argArray[1].Value);
            }
            else if (cb1 != null)
            {
                result = -cb1.CompareTo(argArray[0].Value);
            }
            else
            {
                throw new NotSupportedException();
            }
            return ProcessResult(result);
        }
    }

    abstract class BitwiseOp : BinaryOp
    {
        protected override bool IsEvalableType(Type type)
        {
            return BinaryHelper.IsPrimitiveType(type);
        }
    }

    class BitwiseOr : BitwiseOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.BitwiseOr; }
        }

        public override string ToString()
        {
            return "|";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            int ret = System.Convert.ToInt32(argArray[0].Value) | System.Convert.ToInt32(argArray[1].Value);
            return new Result(typeof(int), ret);
        }
    }

    class BitwiseXor : BitwiseOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.BitwiseOr; }
        }

        public override string ToString()
        {
            return "^";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            int ret = System.Convert.ToInt32(argArray[0].Value) ^ System.Convert.ToInt32(argArray[1].Value);
            return new Result(typeof(int), ret);
        }
    }

    class BitwiseAnd : BitwiseOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.BitwiseAnd; }
        }
        public override string ToString()
        {
            return "&";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            int ret = System.Convert.ToInt32(argArray[0].Value) & System.Convert.ToInt32(argArray[1].Value);
            return new Result(typeof(int), ret);
        }
    }

    class BitwiseShift : BinaryOp
    {
        bool _isleft;
        public BitwiseShift(bool isleft)
        {
            _isleft = isleft;
        }

        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Shift; }
        }
        protected override bool IsEvalableType(Type type)
        {
            return BinaryHelper.IsPrimitiveType(type);
        }
        public override string ToString()
        {
            return "shift";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            int ret = 0;
            if (_isleft)
                ret = System.Convert.ToInt32(argArray[0].Value) << System.Convert.ToInt32(argArray[1].Value);
            else
            {
                ret = System.Convert.ToInt32(argArray[0].Value) >> System.Convert.ToInt32(argArray[1].Value);
            }
            return new Result(typeof(int), ret);
        }
    }

    /// <summary>
    /// add double or string
    /// </summary>
    class AdditiveOp : BinaryOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Additive; }
        }

        public override string ToString()
        {
            return "+";
        }

        protected override bool IsEvalableType(Type type)
        {
            if (type == null)
                return true;
            if (BinaryHelper.IsPrimitiveType(type))
                return true;
            if (type == typeof(string))
                return true;
            return false;
        }

        protected override void ProcessArg(Evaluator evaluater, Result[] argArray)
        {
            base.ProcessArg(evaluater, argArray);
            Type type0 = argArray[0].Type;
            Type type1 = argArray[1].Type;
            int result;

            if (type0 == null && type1 == null)
            {
                return;
            }

            if (type0 == null)
            {
                if (type1.IsValueType)
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type1.Name));
                }

                if (type1 == typeof(string))
                {
                    argArray[0].Type = typeof(string);
                    return;
                }
                else
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type1.Name));
                }
            }

            if (type1 == null)
            {
                if (type0.IsValueType)
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type0.Name));
                }

                if (type0 == typeof(string))
                {
                    argArray[1].Type = typeof(string);
                    return;
                }
                else
                {
                    throw new InvalidCastException(string.Format("from null to {0}", type0.Name));
                }
            }

            if (BinaryHelper.ComparePrimitiveType(type0, type1, out result))
            {
                if (result > 0)
                {
                    argArray[1].Value = ConvertHelper.ChangeType(argArray[1].Value, type0);
                }
                else if (result < 0)
                {
                    argArray[0].Value = ConvertHelper.ChangeType(argArray[0].Value, type1);
                }
            }
            else
            {
                throw new InvalidCastException(string.Format("from {1} to {0}", type0.Name, type1.Name));
            }
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            Type type0 = argArray[0].Type;
            Type type1 = argArray[1].Type;
            if (type0 == null && type1 == null)
            {
                return new Result(null, null);
            }
            else
            {
                if (type0 == typeof(string) || type1 == typeof(string))
                    return new Result( ConvertHelper.ToString(argArray[0].Value) +  ConvertHelper.ToString(argArray[1].Value));
                else
                {
                    double ret = System.Convert.ToDouble(argArray[0].Value) + System.Convert.ToDouble(argArray[1].Value);
                    return new Result(type0, ConvertHelper.ChangeType(ret, type0));
                }
            }
        }
    }

    abstract class ArithmeticOp : BinaryOp
    {
        protected override bool IsEvalableType(Type type)
        {
            if (type == null)
                return true;
            if (BinaryHelper.IsPrimitiveType(type))
                return true;
            return false;
        }

        protected override void ProcessArg(Evaluator evaluater, Result[] argArray)
        {
            base.ProcessArg(evaluater, argArray);
            Type type0 = argArray[0].Type;
            Type type1 = argArray[1].Type;
            int result;

            if (type0 == null || type1 == null)
            {
                throw new InvalidCastException();
            }

            if (BinaryHelper.ComparePrimitiveType(type0, type1, out result))
            {
                if (result > 0)
                {
                    argArray[1].Value = ConvertHelper.ChangeType(argArray[1].Value, type0);
                }
                else if (result < 0)
                {
                    argArray[0].Value = ConvertHelper.ChangeType(argArray[0].Value, type1);
                }
            }
            else
            {
                throw new InvalidCastException(string.Format("from {1} to {0}", type0.Name, type1.Name));
            }
        }
    }

    class ArithmeticSubtract : ArithmeticOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Additive; }
        }

        public override string ToString()
        {
            return "-";
        }


        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            Type type = argArray[0].Type;
            double ret = System.Convert.ToDouble(argArray[0].Value) - System.Convert.ToDouble(argArray[1].Value);
            return new Result(type, ConvertHelper.ChangeType(ret, type));
        }
    }

    class ArithmeticMultiply : ArithmeticOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Multiplicative; }
        }

        public override string ToString()
        {
            return "*";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            Type type = argArray[0].Type;
            double ret = System.Convert.ToDouble(argArray[0].Value) * System.Convert.ToDouble(argArray[1].Value);
            return new Result(type, ConvertHelper.ChangeType(ret, type));
        }
    }

    class ArithmeticDivide : ArithmeticOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Multiplicative; }
        }

        public override string ToString()
        {
            return "/";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            Type type = argArray[0].Type;
            double ret = System.Convert.ToDouble(argArray[0].Value) / System.Convert.ToDouble(argArray[1].Value);
            return new Result(type, ConvertHelper.ChangeType(ret, type));
        }
    }

    class ArithmeticModulus : ArithmeticOp
    {
        public override OperatorPriority Priority
        {
            get { return OperatorPriority.Multiplicative; }
        }

        public override string ToString()
        {
            return "%";
        }

        protected override Result DoEval(Evaluator evaluater, Result[] argArray)
        {
            Type type = typeof(int);
            int ret = System.Convert.ToInt32(argArray[0].Value) % System.Convert.ToInt32(argArray[1].Value);
            return new Result(type, ConvertHelper.ChangeType(ret, type));
        }
    }

    #endregion          
}