// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.ExpressionBuilders;
using RulesEngine.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Xunit;

namespace RulesEngine.UnitTest
{
    [ExcludeFromCodeCoverage]
    public class RuleExpressionParserValidationTests
    {
        [Fact]
        public void Parse_NullParametersArray_ShouldThrowArgumentNullException()
        {
            var parser = new RuleExpressionParser(new ReSettings());

            var ex = Assert.Throws<ArgumentNullException>(() => {
                parser.Parse("1 == 1", null, typeof(bool));
            });

            Assert.Equal("parameters", ex.ParamName);
        }

        [Fact]
        public void Parse_ArrayWithNullElement_ShouldThrowArgumentException()
        {
            var parser = new RuleExpressionParser(new ReSettings());

            var parameters = new ParameterExpression[] { null };

            var ex = Assert.Throws<ArgumentException>(() => {
                parser.Parse("1 == 1", parameters, typeof(bool));
            });

            Assert.Contains("null elements", ex.Message);
        }

        [Fact]
        public void Parse_ValidParameters_ShouldSucceed()
        {
            var parser = new RuleExpressionParser(new ReSettings());

            var param = Expression.Parameter(typeof(int), "x");
            var parameters = new[] { param };

            var result = parser.Parse("x > 5", parameters, typeof(bool));

            Assert.NotNull(result);
        }

        [Fact]
        public void Parse_MultipleValidParameters_ShouldSucceed()
        {
            var parser = new RuleExpressionParser(new ReSettings());

            var param1 = Expression.Parameter(typeof(int), "x");
            var param2 = Expression.Parameter(typeof(int), "y");
            var parameters = new[] { param1, param2 };

            var result = parser.Parse("x > y", parameters, typeof(bool));

            Assert.NotNull(result);
        }
    }
}
