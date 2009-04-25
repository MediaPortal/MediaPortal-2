using System; 
using System.Text;
using System.Collections;

namespace Jyc.Expr
{
    public class Tree
    {
        internal class StackNode
        {
            internal ExprNode root;
            internal ExprNode top;
            internal char charPushed;
        }

        Stack  _nodeStack;
        internal ExprNode _root; 
        internal ExprNode _top;
        internal bool _isStart;

        public Tree()
        {
            _root = new ExprNode(null); 
            _top = _root;
            _nodeStack = new Stack();
            _isStart = true;
        }

        internal ExprNode Root
        {
            get { return _root; }
            set { _root = value; }
        }

        internal void Push(char ch)
        {
            StackNode data = new StackNode();
            data.root = _root;
            data.top = _top;
            data.charPushed = ch;
            _nodeStack.Push(data);
            _root = new ExprNode(null);
            _top = _root;
            _isStart = true;
        }

        internal void Pop(char ch)
        {
            _isStart = false;
            if (_nodeStack.Count == 0)
            {
                if (ch == ',')
                    throw ParserException.NoParenBefore("'( or ']'");
                else
                    throw ParserException.NoParenBefore(ch.ToString());
            }

            StackNode data = (StackNode)_nodeStack.Pop();
            if (ch != ',' && data.charPushed != ',')
            {
                if (ch != data.charPushed)
                {
                    throw ParserException.ParenNotMatch(ch);
                }
            }
            if (this._root.Expression == null)
            {
                if (this._root.OperandCount > 0)
                {
                    foreach (ExprNode node in this._root.Operands)
                    {
                        node._parent = data.top;
                    }
                    data.top.Operands.AddRange(this._root.Operands);
                }
            }
            else
            {
                data.top.Add(this._root);
            }             
            this._root = data.root;
            this._top = data.top;
        }

        internal void AddOperand(Operand expr)
        {
            _isStart = false;
            ExprNode argNode = new ExprNode(expr); 
            _top.Add(argNode);
        }
 
        internal void AddOperator(Operator expr )
        {
            _isStart = false;
            ExprNode newNode = new ExprNode(expr);
            ExprNode lastOperand = _top.LastOperand; 
            if (_top.Expression == null)
            { 
                _top.Expression = expr;
            }
            else
            {  
                Operator op = _top.Expression as Operator;
                if (op == null)
                {
                    throw ParserException.InternalError(); 
                }
                else
                { 
                    if (expr.Priority > op.Priority)
                    {
                        if (lastOperand != null)
                        {                                 
                            _top.Remove(lastOperand);
                            newNode.Add(lastOperand);
                            _top.Add(newNode); 
                            _top = newNode;                                 
                        }
                        else
                        {
                            throw ParserException.InternalError(); 
                        }                             
                    }
                    else
                    {
                        if (_top.Parent != null)
                        { 
                            newNode.Add(_root);
                            _root = newNode;
                            _top = newNode; 
                             
                        }
                        else
                        {
                            newNode.Add(_top);
                            _top = newNode;
                            _root = _top;
                        } 
                    } 
                
                } 
            }
        }

        internal void Complete()
        {
            if (_nodeStack.Count != 0)
            {
                StackNode data = (StackNode)_nodeStack.Pop();
                throw ParserException.ParenNotMatch(data.charPushed);
            }
            _nodeStack = null;
            // fix null
            if (_root.Expression == null)
            {
                if (_root.OperandCount == 0)
                {
                    _root = null; 
                }
                else if (_root.OperandCount > 1)
                {
                    throw ParserException.InternalError(); 
                }
                else
                {
                    _root = (ExprNode)_root.Operands[0];
                    _root._parent = null;
                }
            }

        }

        static void PrintIndent(System.IO.TextWriter w, int indent)
        {
            for (int i = 0; i < indent; i++)
                w.Write("  ");
        }

        static void PrintNode(ExprNode node,System.IO.TextWriter w,int indent)
        {
            PrintIndent(w, indent);
            w.Write(node.Expression.ToString());
            if (node.Expression is Operator)
                w.Write(':');
            w.WriteLine();

            if (node.OperandCount != 0)
            {
                foreach (ExprNode child in node.Operands)
                {
                    PrintNode(child, w, indent+1);
                }
            } 
        }

        public void Print(System.IO.TextWriter w)
        {
            if (this._root == null)
                return; 
            if (this._root.Expression == null)
                return;
            PrintNode(this._root, w, 0); 
        }
    }

    public class ExprNode
    {
        internal ExprNode(Expression expression)
        {
            _expression = expression;
        }

        internal ExprNode _parent;

        internal ExprNode Parent
        {
            get { return _parent; }
        }

        Expression _expression;

        internal Expression Expression
        {
            get { return _expression; }
            set { _expression = value; }
        }

        ArrayList _operands;

        internal void Add(ExprNode node)
        {
            if (node._parent != null)
            {
                throw new ArgumentException();
            }
            node._parent = this;
            this.Operands.Add(node);
        }

        internal ExprNode RemoveAt(int index)
        {
            ExprNode node = (ExprNode)this.Operands[index];
            node._parent = null;
            this.Operands.RemoveAt(index);
            return node;
        }

        internal void Remove(ExprNode node)
        {
            if (node.Parent == this)
            {
                node._parent = null;
                this.Operands.Remove(node);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        internal int OperandCount
        {
            get
            {
                if (_operands == null)
                    return 0;
                return _operands.Count;
            }
        }

        internal ArrayList Operands
        {
            get
            {
                if (_operands == null)
                {
                    _operands = new ArrayList(2);
                }
                return _operands;
            }
        }

        internal ExprNode LastOperand
        {
            get
            {
                if (_operands == null)
                    return null;
                if (_operands.Count == 0)
                    return null;
                return (ExprNode)_operands[_operands.Count - 1];
            }
        }
    }

 
}

