// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RulesEngine.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class ConcurrentWorkflowInjectionTests
    {
        [Fact]
        public async Task ConcurrentWorkflowInjection_ShouldNotDuplicateRules()
        {
            var baseWorkflow = new Workflow {
                WorkflowName = "BaseWorkflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "BaseRule",
                        Expression = "input1.value > 100",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var injectableWorkflow = new Workflow {
                WorkflowName = "InjectableWorkflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "InjectedRule1",
                        Expression = "input1.value > 50",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "InjectedRule2",
                        Expression = "input1.value > 25",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var compositeWorkflow = new Workflow {
                WorkflowName = "CompositeWorkflow",
                WorkflowsToInject = new List<string> { "InjectableWorkflow" },
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "CompositeRule",
                        Expression = "input1.value > 10",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { baseWorkflow, injectableWorkflow, compositeWorkflow }, new ReSettings());

            var input = new {
                value = 200
            };

            var tasks = new List<Task<List<RuleResultTree>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(engine.ExecuteAllRulesAsync("CompositeWorkflow", new RuleParameter("input1", input)).AsTask());
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.Equal(3, result.Count);
                var ruleNames = result.Select(r => r.Rule.RuleName).ToList();
                Assert.Contains("CompositeRule", ruleNames);
                Assert.Contains("InjectedRule1", ruleNames);
                Assert.Contains("InjectedRule2", ruleNames);
                Assert.Equal(3, ruleNames.Distinct().Count());
            }
        }

        [Fact]
        public async Task ParallelWorkflowInjection_ShouldNotDuplicateRules()
        {
            var injectableWorkflow = new Workflow {
                WorkflowName = "InjectableWorkflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "InjectedRule",
                        Expression = "input1.value > 50",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var compositeWorkflow = new Workflow {
                WorkflowName = "CompositeWorkflow",
                WorkflowsToInject = new List<string> { "InjectableWorkflow" },
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "CompositeRule",
                        Expression = "input1.value > 10",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { injectableWorkflow, compositeWorkflow }, new ReSettings());

            var input = new {
                value = 200
            };

            var allRuleCounts = new List<int>();
            var lockObj = new object();

            await Task.Run(() => {
                Parallel.For(0, 10, _ => {
                    var result = engine.ExecuteAllRulesAsync("CompositeWorkflow", new RuleParameter("input1", input)).AsTask().Result;
                    lock (lockObj)
                    {
                        allRuleCounts.Add(result.Count);
                    }
                });
            });

            Assert.All(allRuleCounts, count => Assert.Equal(2, count));
        }
    }
}
