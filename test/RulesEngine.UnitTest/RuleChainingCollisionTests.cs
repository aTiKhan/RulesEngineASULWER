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
    public class RuleChainingCollisionTests
    {
        [Fact]
        public async Task RuleChaining_RuleName_ShouldNotMatchSubstring()
        {
            var workflow = new Workflow {
                WorkflowName = "TestChainingCollision",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Test",
                        SuccessEvent = "TestPassed",
                        ErrorMessage = "Test not met.",
                        Expression = "input1.value > 1000",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "Test2",
                        SuccessEvent = "Test2Passed",
                        ErrorMessage = "Test2 not met.",
                        Expression = "input1.value > 500",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "DependentRule",
                        SuccessEvent = "DependentPassed",
                        ErrorMessage = "Dependent failed.",
                        Expression = "@Test && input1.value > 2000",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings { EnableScopedParams = true });

            var input = new {
                value = 2500
            };

            var results = await engine.ExecuteAllRulesAsync("TestChainingCollision", new RuleParameter("input1", input));

            Assert.Equal(3, results.Count);
            Assert.True(results[0].IsSuccess, "Test should pass");
            Assert.True(results[1].IsSuccess, "Test2 should pass");
            Assert.True(results[2].IsSuccess, "DependentRule should pass because @Test matched, not @Test2");
        }

        [Fact]
        public async Task RuleChaining_SuccessEvent_ShouldNotMatchSubstring()
        {
            var workflow = new Workflow {
                WorkflowName = "TestEventCollision",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "FirstRule",
                        SuccessEvent = "Test",
                        ErrorMessage = "FirstRule not met.",
                        Expression = "input1.value > 1000",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "SecondRule",
                        SuccessEvent = "MyTest",
                        ErrorMessage = "SecondRule not met.",
                        Expression = "input1.value > 500",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "DependentRule",
                        SuccessEvent = "DependentPassed",
                        ErrorMessage = "Dependent failed.",
                        Expression = "Test",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings { EnableScopedParams = true });

            var input = new {
                value = 1500
            };

            var results = await engine.ExecuteAllRulesAsync("TestEventCollision", new RuleParameter("input1", input));

            Assert.Equal(3, results.Count);
            Assert.True(results[0].IsSuccess, "FirstRule should pass");
            Assert.True(results[1].IsSuccess, "SecondRule should pass");
            Assert.True(results[2].IsSuccess, "DependentRule should pass because 'Test' matched exactly, not 'MyTest'");
        }

        [Fact]
        public async Task RuleChaining_RuleNameInExpression_ShouldNotCorruptExpression()
        {
            var workflow = new Workflow {
                WorkflowName = "TestExpressionCorruption",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Count",
                        SuccessEvent = "CountPassed",
                        ErrorMessage = "Count not met.",
                        Expression = "input1.value > 1000",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "DependentRule",
                        SuccessEvent = "DependentPassed",
                        ErrorMessage = "Dependent failed.",
                        Expression = "@Count && input1.DiscountAmount > 50",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings { EnableScopedParams = true });

            var input = new {
                value = 1500,
                DiscountAmount = 75
            };

            var results = await engine.ExecuteAllRulesAsync("TestExpressionCorruption", new RuleParameter("input1", input));

            Assert.Equal(2, results.Count);
            Assert.True(results[0].IsSuccess, "Count should pass");
            Assert.True(results[1].IsSuccess, "DependentRule should pass - DiscountAmount should not be corrupted by stripping @Count");
        }

        [Fact]
        public async Task RuleChaining_SuccessEventTest2_ShouldNotMatchTest()
        {
            var workflow = new Workflow {
                WorkflowName = "TestEventTest2Collision",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "FirstRule",
                        SuccessEvent = "Test2",
                        ErrorMessage = "FirstRule not met.",
                        Expression = "input1.value > 1000",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "DependentRule",
                        SuccessEvent = "DependentPassed",
                        ErrorMessage = "Dependent failed.",
                        Expression = "Test2",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, new ReSettings { EnableScopedParams = true });

            var input = new {
                value = 1500
            };

            var results = await engine.ExecuteAllRulesAsync("TestEventTest2Collision", new RuleParameter("input1", input));

            Assert.Equal(2, results.Count);
            Assert.True(results[0].IsSuccess, "FirstRule should pass");
            Assert.True(results[1].IsSuccess, "DependentRule should pass because 'Test2' matched exactly");
        }
    }
}
