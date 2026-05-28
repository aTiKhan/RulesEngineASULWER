// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RulesEngine.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class DisabledRuleErrorMessageTests
    {
        [Fact]
        public async Task ExecuteActionWorkflowAsync_DisabledRule_ShouldReturnAccurateError()
        {
            var workflow = new Workflow
            {
                WorkflowName = "TestDisabled",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "DisabledRule",
                        Enabled = false,
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    },
                    new Rule
                    {
                        RuleName = "EnabledRule",
                        Enabled = true,
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow });

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await engine.ExecuteActionWorkflowAsync("TestDisabled", "DisabledRule", Array.Empty<RuleParameter>()));

            Assert.Contains("does not contain any rule named `DisabledRule`", ex.Message);
        }

        [Fact]
        public async Task ExecuteActionWorkflowAsync_EnabledRule_ShouldSucceed()
        {
            var workflow = new Workflow
            {
                WorkflowName = "TestEnabled",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "EnabledRule",
                        Enabled = true,
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow });

            var result = await engine.ExecuteActionWorkflowAsync("TestEnabled", "EnabledRule", Array.Empty<RuleParameter>());

            Assert.NotNull(result);
            Assert.True(result.Results?.All(r => r.IsSuccess) ?? false);
        }
    }
}
