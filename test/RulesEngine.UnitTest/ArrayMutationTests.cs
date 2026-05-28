// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace RulesEngine.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class ArrayMutationTests
    {
        [Fact]
        public async Task ExecuteAllRulesAsync_ShouldNotMutateRuleParameterArrayOrder()
        {
            var workflow = new Workflow {
                WorkflowName = "TestArrayMutation",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "z > 0 && a > 0 && m > 0",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings());

            var originalParams = new[]
            {
                new RuleParameter("z", 10),
                new RuleParameter("a", 20),
                new RuleParameter("m", 30)
            };

            var originalOrder = new[] { originalParams[0].Name, originalParams[1].Name, originalParams[2].Name };

            var results = await engine.ExecuteAllRulesAsync("TestArrayMutation", originalParams);

            Assert.True(results[0].IsSuccess);
            Assert.Equal(originalOrder[0], originalParams[0].Name);
            Assert.Equal(originalOrder[1], originalParams[1].Name);
            Assert.Equal(originalOrder[2], originalParams[2].Name);
        }

        [Fact]
        public async Task ExecuteAllRulesAsync_ShouldNotMutateInputArray()
        {
            var workflow = new Workflow {
                WorkflowName = "TestInputArrayMutation",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "input1 > 0",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "Rule2",
                        Expression = "input2 > 0",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings());

            // Pass as separate object arguments, not an int array
            var results = await engine.ExecuteAllRulesAsync("TestInputArrayMutation", 5, 3);

            Assert.Equal(2, results.Count);
            Assert.True(results[0].IsSuccess);
            Assert.True(results[1].IsSuccess);
        }

        [Fact]
        public async Task ExecuteAllRulesAsync_ShouldNotModifyParameterCount()
        {
            var workflow = new Workflow {
                WorkflowName = "TestParamCount",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "input1 == \"test\"",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings());

            var parameters = new[]
            {
                new RuleParameter("input1", "test"),
                new RuleParameter("input2", 123),
                new RuleParameter("input3", true)
            };

            var originalLength = parameters.Length;

            var results = await engine.ExecuteAllRulesAsync("TestParamCount", parameters);

            Assert.Equal(originalLength, parameters.Length);
        }
    }
}
