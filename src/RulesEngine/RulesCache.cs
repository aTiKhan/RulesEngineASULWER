// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.HelperFunctions;
using RulesEngine.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RulesEngine
{
    /// <summary>
    /// Provides caching for workflow definitions and compiled rule delegates.
    /// </summary>
    internal class RulesCache
    {
        /// <summary>The compile rules</summary>
        private readonly MemCache _compileRules;

        /// <summary>The workflow rules</summary>
        private readonly ConcurrentDictionary<string, (Workflow, long)> _workflow = new ConcurrentDictionary<string, (Workflow, long)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RulesCache"/> class.
        /// </summary>
        /// <param name="reSettings">The rules engine settings, including cache configuration.</param>
        public RulesCache(ReSettings reSettings)
        {
            _compileRules = new MemCache(reSettings.CacheConfig);
        }

        /// <summary>
        /// Determines whether a workflow with the specified name is registered.
        /// </summary>
        /// <param name="workflowName">The name of the workflow to check.</param>
        /// <returns><c>true</c> if the workflow exists; otherwise, <c>false</c>.</returns>
        public bool ContainsWorkflows(string workflowName)
        {
            return _workflow.ContainsKey(workflowName);
        }

        /// <summary>
        /// Gets a list of all registered workflow names.
        /// </summary>
        /// <returns>A list of workflow names.</returns>
        public List<string> GetAllWorkflowNames()
        {
            return _workflow.Keys.ToList();
        }

        /// <summary>
        /// Adds or updates a workflow definition.
        /// </summary>
        /// <param name="workflowName">The name of the workflow.</param>
        /// <param name="rules">The workflow definition to store.</param>
        public void AddOrUpdateWorkflows(string workflowName, Workflow rules)
        {
            long ticks = DateTime.UtcNow.Ticks;
            _workflow.AddOrUpdate(workflowName, (rules, ticks), (k, v) => (rules, ticks));
        }

        /// <summary>
        /// Adds or updates compiled rules for a given cache key.
        /// </summary>
        /// <param name="compiledRuleKey">The compiled rule cache key.</param>
        /// <param name="compiledRule">The compiled rules dictionary.</param>
        public void AddOrUpdateCompiledRule(string compiledRuleKey, IDictionary<string, RuleFunc<RuleResultTree>> compiledRule)
        {
            long ticks = DateTime.UtcNow.Ticks;
            _compileRules.Set(compiledRuleKey, (compiledRule, ticks));
        }

        /// <summary>
        /// Checks whether the compiled rules for a cache key are up-to-date with the workflow.
        /// </summary>
        /// <param name="compiledRuleKey">The compiled rule cache key.</param>
        /// <param name="workflowName">The workflow name to compare against.</param>
        /// <returns><c>true</c> if compiled rules are newer or equal to the workflow; otherwise, <c>false</c>.</returns>
        public bool AreCompiledRulesUpToDate(string compiledRuleKey, string workflowName)
        {
            if (_compileRules.TryGetValue(compiledRuleKey, out (IDictionary<string, RuleFunc<RuleResultTree>> rules, long tick) compiledRulesObj))
            {
                if (_workflow.TryGetValue(workflowName, out (Workflow rules, long tick) WorkflowsObj))
                {
                    return compiledRulesObj.tick >= WorkflowsObj.tick;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears all cached workflows and compiled rules.
        /// </summary>
        public void Clear()
        {
            _workflow.Clear();
            _compileRules.Clear();
        }

        /// <summary>
        /// Gets the workflow definition, optionally merging injected workflows.
        /// </summary>
        /// <param name="workflowName">The name of the workflow to retrieve.</param>
        /// <returns>The workflow definition, or <c>null</c> if not found.</returns>
        /// <exception cref="Exception">Thrown when an injected workflow cannot be found.</exception>
        public Workflow GetWorkflow(string workflowName)
        {
            if (_workflow.TryGetValue(workflowName, out (Workflow rules, long tick) WorkflowsObj))
            {
                var baseWorkflow = WorkflowsObj.rules;

                // If no injection needed, return as-is (or still clone to be safe)
                if (baseWorkflow.WorkflowsToInject?.Any() != true)
                {
                    return baseWorkflow;
                }

                // Create a shallow copy to avoid mutating the cache
                var workflow = new Workflow {
                    WorkflowName = baseWorkflow.WorkflowName,
                    GlobalParams = baseWorkflow.GlobalParams,
                    RuleExpressionType = baseWorkflow.RuleExpressionType,
                    WorkflowsToInject = baseWorkflow.WorkflowsToInject,
                    Rules = baseWorkflow.Rules?.ToList() ?? new List<Rule>()
                };

                foreach (string wfname in baseWorkflow.WorkflowsToInject)
                {
                    var injectedWorkflow = GetWorkflow(wfname);
                    if (injectedWorkflow == null)
                    {
                        throw new Exception($"Could not find injected Workflow: {wfname}");
                    }

                    workflow.Rules = workflow.Rules.Concat(injectedWorkflow.Rules).ToList();
                }

                return workflow;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets compiled rules for the specified cache key.
        /// </summary>
        /// <param name="compiledRulesKey">The compiled rules cache key.</param>
        /// <returns>The compiled rules dictionary, or <c>null</c> if not found.</returns>
        public IDictionary<string, RuleFunc<RuleResultTree>> GetCompiledRules(string compiledRulesKey)
        {
            return _compileRules.Get<(IDictionary<string, RuleFunc<RuleResultTree>> rules, long tick)>(compiledRulesKey).rules;
        }

        /// <summary>
        /// Removes a workflow and its associated compiled rules from the cache.
        /// </summary>
        /// <param name="workflowName">The name of the workflow to remove.</param>
        public void Remove(string workflowName)
        {
            if (_workflow.TryRemove(workflowName, out var workflowObj))
            {
                var compiledKeysToRemove = _compileRules.GetKeys().Where(key => key.StartsWith(workflowName));
                foreach (var key in compiledKeysToRemove)
                {
                    _compileRules.Remove(key);
                }
            }
        }
    }
}
