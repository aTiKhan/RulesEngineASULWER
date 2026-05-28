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
    public class JsonCaseInsensitiveTests
    {
        [Fact]
        public async Task RulesEngine_ShouldAcceptCamelCaseJson()
        {
            var camelCaseJson = @"{
                ""workflowName"": ""TestWorkflow"",
                ""rules"": [
                    {
                        ""ruleName"": ""TestRule"",
                        ""expression"": ""input1 > 100"",
                        ""ruleExpressionType"": ""LambdaExpression""
                    }
                ]
            }";

            var engine = new RulesEngine(new[] { camelCaseJson }, new ReSettings());

            Assert.True(engine.ContainsWorkflow("TestWorkflow"));

            var results = await engine.ExecuteAllRulesAsync("TestWorkflow", 200);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsSuccess);
            Assert.Equal("TestRule", results[0].Rule.RuleName);
        }

        [Fact]
        public async Task RulesEngine_ShouldAcceptMixedCaseJson()
        {
            var mixedCaseJson = @"{
                ""WorkflowName"": ""TestWorkflow"",
                ""rules"": [
                    {
                        ""RuleName"": ""Rule1"",
                        ""expression"": ""input1 > 50"",
                        ""ruleExpressionType"": ""LambdaExpression"",
                        ""enabled"": true
                    }
                ]
            }";

            var engine = new RulesEngine(new[] { mixedCaseJson }, new ReSettings());

            Assert.True(engine.ContainsWorkflow("TestWorkflow"));

            var results = await engine.ExecuteAllRulesAsync("TestWorkflow", 100);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsSuccess);
        }

        [Fact]
        public async Task RulesEngine_ShouldAcceptPascalCaseJson()
        {
            var pascalCaseJson = @"{
                ""WorkflowName"": ""TestWorkflow"",
                ""Rules"": [
                    {
                        ""RuleName"": ""TestRule"",
                        ""Expression"": ""input1 == true"",
                        ""RuleExpressionType"": ""LambdaExpression"",
                        ""Enabled"": true
                    }
                ]
            }";

            var engine = new RulesEngine(new[] { pascalCaseJson }, new ReSettings());

            Assert.True(engine.ContainsWorkflow("TestWorkflow"));

            var results = await engine.ExecuteAllRulesAsync("TestWorkflow", true);

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsSuccess);
        }
    }
}
