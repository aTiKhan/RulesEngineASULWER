// Copyright (c) Microsoft Corporation.
//  Licensed under the MIT License.

using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using static FastExpressionCompiler.ImTools.SmallMap;

namespace RulesEngine.Actions
{
    public class ActionContext
    {
        private readonly IDictionary<string, string> _context;
        private readonly CancellationToken _token = CancellationToken.None;
        private readonly RuleResultTree _parentResult;

        public ActionContext(IDictionary<string, object> context, RuleResultTree parentResult, CancellationToken cancellationToken) : this(context, parentResult)
        {
           _token = cancellationToken;
        }

        public ActionContext(IDictionary<string, object> context, RuleResultTree parentResult)
        {
            _context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in context)
            {
                if (kv.Value == null)
                    continue;
                else if (kv.Value is string || kv.Value is JsonElement)
                    _context.Add(kv.Key, kv.Value.ToString());
                else
                    _context.Add(kv.Key, JsonSerializer.Serialize(kv.Value));
            }
            _parentResult = parentResult;
        }

        public RuleResultTree GetParentRuleResult()
        {
            return _parentResult;
        }

        public bool TryGetContext<T>(string name,out T output)
        {
            try
            {
                output =  GetContext<T>(name);
                return true;
            }
            catch(ArgumentException)
            {
                output = default(T);
                return false;
            }
        }

        public T GetContext<T>(string name)
        {
            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)_context[name];
                
                return JsonSerializer.Deserialize<T>(_context[name]);
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"Argument `{name}` was not found in the action context");
            }
            catch (JsonException)
            {
                throw new ArgumentException($"Failed to convert argument `{name}` to type `{typeof(T).Name}` in the action context");
            }
        }

        public CancellationToken GetCancellationToken()
        {
            return _token;
        }
    }
}
