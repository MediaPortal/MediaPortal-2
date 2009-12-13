#region License
//
// Command Line Library: OptionMap.cs
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
#region Using Directives
using System;
using System.Collections.Generic;
#endregion

namespace CommandLine
{
    sealed class OptionMap
    {
        private readonly CommandLineParserSettings _settings;
        private Dictionary<string, string> _names;
        private Dictionary<string, OptionInfo> _map;
        private Dictionary<string, int> _mutuallyExclusiveSetMap;

        public OptionMap(int capacity, CommandLineParserSettings settings)
        {
            _settings = settings;

            IEqualityComparer<string> comparer;
            if (_settings.CaseSensitive)
                comparer = StringComparer.Ordinal;
            else
                comparer = StringComparer.OrdinalIgnoreCase;

            _names = new Dictionary<string, string>(capacity, comparer);
            _map = new Dictionary<string, OptionInfo>(capacity * 2, comparer);

            if (_settings.MutuallyExclusive)
                _mutuallyExclusiveSetMap = new Dictionary<string, int>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        public OptionInfo this[string key]
        {
            get
            {
                OptionInfo option = null;

                if (_map.ContainsKey(key))
                    option = _map[key];
                else
                {
                    string optionKey = null;
                    if (_names.ContainsKey(key))
                    {
                        optionKey = _names[key];
                        option = _map[optionKey];
                    }
                }

                return option;
            }
            set
            {
                _map[key] = value;

                if (value.HasBothNames)
                    _names[value.LongName] = value.ShortName;
            }
        }

        public bool EnforceRules()
        {
            return EnforceMutuallyExclusiveMap() && EnforceRequiredRule();
        }

        private bool EnforceRequiredRule()
        {
            foreach (OptionInfo option in _map.Values)
            {
                if (option.Required && !option.IsDefined)
                    return false;
            }
            return true;
        }

        private bool EnforceMutuallyExclusiveMap()
        {
            if (!_settings.MutuallyExclusive)
                return true;

            foreach (OptionInfo option in _map.Values)
            {
                if (option.IsDefined && option.MutuallyExclusiveSet != null)
                    BuildMutuallyExclusiveMap(option);
            }

            foreach (int occurrence in _mutuallyExclusiveSetMap.Values)
            {
                if (occurrence > 1)
                    return false;
            }

            return true;
        }

        private void BuildMutuallyExclusiveMap(OptionInfo option)
        {
            var setName = option.MutuallyExclusiveSet;

            if (!_mutuallyExclusiveSetMap.ContainsKey(setName))
                _mutuallyExclusiveSetMap.Add(setName, 0);

            _mutuallyExclusiveSetMap[setName]++;
        }
    }
}