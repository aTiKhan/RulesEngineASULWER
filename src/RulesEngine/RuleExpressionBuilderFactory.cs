// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.ExpressionBuilders;
using RulesEngine.Models;
using System;

namespace RulesEngine
{
    /// <summary>
    /// A factory that provides the appropriate expression builder for a given rule expression type.
    /// </summary>
    internal class RuleExpressionBuilderFactory
    {
        private readonly ReSettings _reSettings;
        private readonly LambdaExpressionBuilder _lambdaExpressionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleExpressionBuilderFactory"/> class.
        /// </summary>
        /// <param name="reSettings">The rules engine settings.</param>
        /// <param name="expressionParser">The expression parser used by expression builders.</param>
        public RuleExpressionBuilderFactory(ReSettings reSettings, RuleExpressionParser expressionParser)
        {
            _reSettings = reSettings;
            _lambdaExpressionBuilder = new LambdaExpressionBuilder(_reSettings, expressionParser);
        }

        /// <summary>
        /// Gets the expression builder for the specified rule expression type.
        /// </summary>
        /// <param name="ruleExpressionType">The type of rule expression.</param>
        /// <returns>The expression builder capable of building expressions for the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the expression type is not supported.</exception>
        public RuleExpressionBuilderBase RuleGetExpressionBuilder(RuleExpressionType ruleExpressionType)
        {
            switch (ruleExpressionType)
            {
                case RuleExpressionType.LambdaExpression:
                    return _lambdaExpressionBuilder;
                default:
                    throw new InvalidOperationException($"{nameof(ruleExpressionType)} has not been supported yet.");
            }
        }
    }
}
