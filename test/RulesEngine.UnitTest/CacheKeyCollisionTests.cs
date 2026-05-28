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
    public class CacheKeyCollisionTests
    {
        [Fact]
        public async Task CacheKey_DifferentGenericTypes_ShouldBeDifferent()
        {
            var workflow = new Workflow {
                WorkflowName = "TestCacheKeyTypes",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "input1.Count > 0",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings());

            var listString = new List<string> { "hello" };
            var listInt = new List<int> { 1 };

            var result1 = await engine.ExecuteAllRulesAsync("TestCacheKeyTypes", new[] { new RuleParameter("input1", listString) }, TestContext.Current.CancellationToken);
            var result2 = await engine.ExecuteAllRulesAsync("TestCacheKeyTypes", new[] { new RuleParameter("input1", listInt) }, TestContext.Current.CancellationToken);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1[0].IsSuccess);
            Assert.True(result2[0].IsSuccess);
        }

        [Fact]
        public async Task CacheKey_SameParamNamesSameTypes_ShouldBeSame()
        {
            var workflow = new Workflow {
                WorkflowName = "TestCacheKeySame",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "input1 > 0",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings());

            var result1 = await engine.ExecuteAllRulesAsync("TestCacheKeySame", new[] { new RuleParameter("input1", 5) }, TestContext.Current.CancellationToken);
            var result2 = await engine.ExecuteAllRulesAsync("TestCacheKeySame", new[] { new RuleParameter("input1", 10) }, TestContext.Current.CancellationToken);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1[0].IsSuccess);
            Assert.True(result2[0].IsSuccess);
        }
    }
}
