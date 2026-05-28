// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Actions;
using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace RulesEngine.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class ActionExceptionTests
    {
        [Fact]
        public async Task ActionResult_Exception_ShouldContainRawOriginalException()
        {
            var settings = new ReSettings {
                CustomActions = new Dictionary<string, Func<ActionBase>>
                {
                    { nameof(ThrowInvalidOperationExceptionAction), () => new ThrowInvalidOperationExceptionAction() }
                },
                EnableExceptionAsErrorMessage = true
            };

            var workflow = new Workflow {
                WorkflowName = "TestException",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression,
                        Actions = new RuleActions
                        {
                            OnSuccess = new ActionInfo
                            {
                                Name = nameof(ThrowInvalidOperationExceptionAction),
                                Context = new Dictionary<string, object>()
                            }
                        }
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, settings);

            var results = await engine.ExecuteAllRulesAsync("TestException", new RuleParameter("input1", 1));

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsSuccess);
            Assert.NotNull(results[0].ActionResult?.Exception);
            Assert.IsType<InvalidOperationException>(results[0].ActionResult.Exception);
            Assert.Equal("Test exception message", results[0].ActionResult.Exception.Message);
        }

        [Fact]
        public async Task ActionResult_Exception_ShouldBeExactlyInvalidOperationException()
        {
            var settings = new ReSettings {
                CustomActions = new Dictionary<string, Func<ActionBase>>
                {
                    { nameof(ThrowInvalidOperationExceptionAction), () => new ThrowInvalidOperationExceptionAction() }
                },
                EnableExceptionAsErrorMessage = true
            };

            var workflow = new Workflow {
                WorkflowName = "TestExceptionType",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression,
                        Actions = new RuleActions
                        {
                            OnSuccess = new ActionInfo
                            {
                                Name = nameof(ThrowInvalidOperationExceptionAction),
                                Context = new Dictionary<string, object>()
                            }
                        }
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, settings);

            var results = await engine.ExecuteAllRulesAsync("TestExceptionType", new RuleParameter("input1", 1));

            var exception = results[0].ActionResult?.Exception;
            Assert.NotNull(exception);
            Assert.Equal(typeof(InvalidOperationException), exception.GetType());
        }

        [Fact]
        public async Task ActionResult_Exception_ShouldNotBeWrapped()
        {
            var settings = new ReSettings {
                CustomActions = new Dictionary<string, Func<ActionBase>>
                {
                    { nameof(ThrowInvalidOperationExceptionAction), () => new ThrowInvalidOperationExceptionAction() }
                },
                EnableExceptionAsErrorMessage = true
            };

            var workflow = new Workflow {
                WorkflowName = "TestNotWrapped",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "Rule1",
                        Expression = "1 == 1",
                        RuleExpressionType = RuleExpressionType.LambdaExpression,
                        Actions = new RuleActions
                        {
                            OnSuccess = new ActionInfo
                            {
                                Name = nameof(ThrowInvalidOperationExceptionAction),
                                Context = new Dictionary<string, object>()
                            }
                        }
                    }
                }
            };

            var engine = new RulesEngine(new[] { workflow }, settings);

            var results = await engine.ExecuteAllRulesAsync("TestNotWrapped", new RuleParameter("input1", 1));

            var exception = results[0].ActionResult?.Exception;
            Assert.NotNull(exception);
            Assert.False(exception is AggregateException);
            Assert.False(exception is TargetInvocationException);
        }

        private class ThrowInvalidOperationExceptionAction : ActionBase
        {
            public async override ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Test exception message");
            }
        }
    }
}
