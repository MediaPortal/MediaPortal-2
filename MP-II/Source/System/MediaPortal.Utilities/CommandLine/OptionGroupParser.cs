#region License
//
// Command Line Library: OptionGroupParser.cs
//
// Author:
//   Giacomo Stelluti Scala (gsscoder@ymail.com)
//
// Copyright (C) 2005 - 2009 Giacomo Stelluti Scala
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace CommandLine
{
    sealed class OptionGroupParser : ArgumentParser
    {
        public sealed override ParserState Parse(IArgumentEnumerator argumentEnumerator, OptionMap map, object options)
        {
            IArgumentEnumerator group = new OneCharStringEnumerator(argumentEnumerator.Current.Substring(1));
            while (group.MoveNext())
            {
                var option = map[group.Current];
                if (option == null)
                    return ParserState.Failure;

                option.IsDefined = true;

                ArgumentParser.EnsureOptionArrayAttributeIsNotBoundToScalar(option);

                if (!option.IsBoolean)
                {
                    if (argumentEnumerator.IsLast && group.IsLast)
                        return ParserState.Failure;

                    if (!group.IsLast)
                    {
                        if (!option.IsArray)
                            return ArgumentParser.BooleanToParserState(option.SetValue(group.GetRemainingFromNext(), options));
                        else
                        {
                            ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                            var items = ArgumentParser.GetNextInputValues(argumentEnumerator);
                            items.Insert(0, group.GetRemainingFromNext());
                            return ArgumentParser.BooleanToParserState(option.SetValue(items, options), true);
                        }
                    }

                    if (!argumentEnumerator.IsLast && !ArgumentParser.IsInputValue(argumentEnumerator.Next))
                        return ParserState.Failure;
                    else
                    {
                        if (!option.IsArray)
                            return ArgumentParser.BooleanToParserState(option.SetValue(argumentEnumerator.Next, options), true);
                        else
                        {
                            ArgumentParser.EnsureOptionAttributeIsArrayCompatible(option);

                            var items = ArgumentParser.GetNextInputValues(argumentEnumerator);
                            return ArgumentParser.BooleanToParserState(option.SetValue(items, options));
                        }
                    }
                }
                else
                {
                    if (!group.IsLast && map[group.Next] == null)
                        return ParserState.Failure;

                    if (!option.SetValue(true, options))
                        return ParserState.Failure;
                }
            }

            return ParserState.Success;
        }
    }
}