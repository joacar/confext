﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Confext
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "()}")]
    internal sealed class ConfigurationTracker
    {
        private readonly LinkedList<string> _keys = new LinkedList<string>();

        private readonly List<Node> _nodes = new List<Node>();
        private readonly string _pattern;
        private readonly ReadOnlyMemory<char> _prefix;
        private readonly Regex _regex;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern">Regex to match against configuration values.</param>
        /// <param name="prefix">Prefix added to configuration keys.</param>
        public ConfigurationTracker(string pattern, string prefix = "")
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Regex pattern can not be null nor empty", nameof(pattern));
            }

            _pattern = pattern;
            _prefix = prefix.AsMemory();
            try
            {
                _regex = new Regex(pattern, RegexOptions.Singleline, TimeSpan.FromMilliseconds(50));
            }
            catch (ArgumentException exception)
            {
                // RegexParseException (RPE) is internal and cause test that expect ArgumentException to fail
                // even though RPE inherit from dito.
                throw new ArgumentException(exception.Message, nameof(pattern), exception);
            }
        }

        // For testing
        internal IReadOnlyList<Node> Nodes => _nodes;

        /// <summary>
        /// Gets the number of matched values.
        /// </summary>
        public int CapturedCount => _nodes.Count;

        /// <summary>
        /// Remove last pushed scope.
        /// </summary>
        public void Pop()
        {
            // In case of JsonReader this won't be called symmetrically with Add since
            // first '{' (StartObject) won't be propagated but the last '}' (EndObject)
            // will. Hence we guard against unbalanced call count.
            if (_keys.Count > 0)
            {
                _keys.RemoveLast();
            }
        }

        /// <summary>
        /// Push <paramref name="name" /> on scope.
        /// </summary>
        /// <remarks>
        /// This should be called for the <c>key (name)</c> part of configuration. For values <see cref="Value" />.
        /// </remarks>
        /// <param name="name"></param>
        public void Push(string name) => Add(name);

        /// <summary>
        /// Add <paramref name="value" /> and current scope if matches pattern.
        /// </summary>
        /// <param name="value">Variable substitution syntax.</param>
        public void Value(string value)
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException($"No key(s) pushed. Call {nameof(Push)} to track key(s).");
            }

            var match = _regex.Match(value);

            void GenerateValue(in string[] strings)
            {
                // TODO: Great area to exercise Span<char>
                value = string.Join(":", strings);
                // The generated value must be enclosed properly in the pattern
                var pattern = match.Groups[0].Value;
                var matched = match.Groups[1];
                var nValue = new char[value.Length + pattern.Length];
                var map = 0;
                for (var p = 0; p < pattern.Length; ++p)
                {
                    if (p == matched.Index)
                    {
                        // Replace empty match group with generated value
                        value.CopyTo(0, nValue, p, value.Length);
                        // Fast forward mapping to end of generated value ready to consume rest of regex
                        map += value.Length;
                    }

                    Debug.Assert(map < nValue.Length);
                    nValue[map++] = pattern[p];
                }

                value = new string(nValue);
            }

            if (match.Success)
            {
                var i = 0;
                var keys = new string[_keys.Count];
                foreach (var key in _keys)
                {
                    keys[i++] = key;
                }
                // If matched groups value is empty string then we generate the value based on the keys
                if (match.Groups.Count == 2 && string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    GenerateValue(keys);
                }
                _nodes.Add(new Node(keys, value));
            }

            // Always pop after value is encountered since one-to-one key value relationship
            // In case of JsonReader this won't be called symmetrically with Add since
            // first '{' (StartObject) won't be propagated but the last '}' (EndObject)
            // will. Hence we guard against unbalanced call count.
            if (_keys.Count > 0)
            {
                _keys.RemoveLast();
            }
        }

        /// <summary>
        /// Write configuration to <paramref name="writer" />.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task WriteTo(TextWriter writer, CancellationToken token = default)
        {
            var written = false;
            foreach (var node in _nodes)
            {
                if (written)
                {
                    await writer.WriteLineAsync().ConfigureAwait(false);
                }

                written = true;
                token.ThrowIfCancellationRequested();
                await writer.WriteAsync(_prefix, token).ConfigureAwait(false);
                await writer.WriteAsync(node, token).ConfigureAwait(false);
            }
        }

        private void Add(string item) => _keys.AddLast(item);

        private string DebuggerDisplay()
        {
            if (_keys.Last is null)
            {
                return $"Pattern = {_pattern}, Depth = {_keys.Count}. Captured count {_nodes.Count}";
            }

            return
                $"Pattern = {_pattern}, Depth = {_keys.Count}, Last seen '{_keys.Last.Value}'. Captured count {_nodes.Count}";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "(),nq}")]
        internal sealed class Node
        {
            private readonly string[] _names;
            private readonly string _pattern;

            public Node(string[] names, string pattern)
            {
                _names = names ?? throw new ArgumentNullException(nameof(names));
                _pattern = pattern;
            }

            // Testing only
            internal string Value => _pattern;

            public override string ToString() => $"{string.Join("__", _names)}={_pattern}";

            public static implicit operator ReadOnlyMemory<char>(Node node) => node.ToString().AsMemory();

            private string DebuggerDisplay() => ToString();
        }
    }
}
