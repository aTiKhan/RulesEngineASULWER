// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RulesEngine.HelperFunctions
{
    /// <summary>
    /// Formats error messages for rule results by replacing parameter placeholders with actual values.
    /// </summary>
    internal class ErrorMessageFormatter
    {
        /// <summary>
        /// Regex pattern to match parameter placeholders in the format $(ParameterName).
        /// </summary>
        private const string ParamParseRegex = @"\$\(([^)]+)\)";

        private readonly ReSettings _reSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessageFormatter"/> class.
        /// </summary>
        /// <param name="reSettings">The rules engine settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="reSettings"/> is null.</exception>
        public ErrorMessageFormatter(ReSettings reSettings)
        {
            _reSettings = reSettings ?? throw new ArgumentNullException(nameof(reSettings));
        }

        /// <summary>
        /// Formats error messages for the specified rule results by replacing parameter placeholders with actual values.
        /// </summary>
        /// <param name="ruleResultList">The collection of rule results to format.</param>
        public void FormatErrorMessages(IEnumerable<RuleResultTree> ruleResultList)
        {
            if (!_reSettings.EnableFormattedErrorMessage)
            {
                return;
            }

            foreach (var ruleResult in ruleResultList?.Where(r => !r.IsSuccess))
            {
                var errorMessage = ruleResult?.Rule?.ErrorMessage;
                if (string.IsNullOrWhiteSpace(ruleResult.ExceptionMessage) && errorMessage != null)
                {
                    ruleResult.ExceptionMessage = BuildErrorMessage(errorMessage, ruleResult.Inputs);
                }
            }
        }

        /// <summary>
        /// Builds an error message by replacing parameter placeholders with values from the inputs.
        /// </summary>
        /// <param name="errorMessage">The error message template containing placeholders.</param>
        /// <param name="inputs">The input values to substitute into the placeholders.</param>
        /// <returns>The formatted error message with placeholders replaced by actual values.</returns>
        private static string BuildErrorMessage(string errorMessage, IDictionary<string, object> inputs)
        {
            var errorParameters = Regex.Matches(errorMessage, ParamParseRegex);

            foreach (var param in errorParameters)
            {
                var paramVal = param?.ToString();
                var property = paramVal?.Substring(2, paramVal.Length - 3);

                if (string.IsNullOrEmpty(property))
                {
                    continue;
                }

                if (property.Split('.').Length > 1)
                {
                    var parts = property.Split(new[] { '.' }, 2);
                    var typeName = parts[0];
                    var propertyName = parts[1];
                    errorMessage = UpdateErrorMessage(errorMessage, inputs, property, typeName, propertyName);
                }
                else
                {
                    var model = inputs?.FirstOrDefault(c => string.Equals(c.Key, property)).Value;
                    var value = model != null ? JsonSerializer.Serialize(model) : null;
                    errorMessage = errorMessage.Replace($"$({property})", value ?? $"$({property})");
                }
            }

            return errorMessage;
        }

        /// <summary>
        /// Updates an error message by replacing a specific parameter placeholder with a JSON property value.
        /// </summary>
        /// <param name="errorMessage">The error message template.</param>
        /// <param name="inputs">The input values dictionary.</param>
        /// <param name="property">The full property placeholder (e.g., "TypeName.PropertyName").</param>
        /// <param name="typeName">The type name to look up in inputs.</param>
        /// <param name="propertyName">The JSON property name to extract.</param>
        /// <returns>The updated error message with the placeholder replaced.</returns>
        private static string UpdateErrorMessage(string errorMessage, IDictionary<string, object> inputs, string property, string typeName, string propertyName)
        {
            var model = inputs?.FirstOrDefault(c => string.Equals(c.Key, typeName)).Value;

            if (model != null)
            {
                using (var jDoc = JsonSerializer.SerializeToDocument(model))
                {
                    errorMessage = jDoc.RootElement.TryGetProperty(propertyName, out var jElement) ?
                        errorMessage.Replace($"$({property})", jElement.GetRawText() ?? $"({property})") :
                        errorMessage.Replace($"$({property})", $"({property})");
                }
            }

            return errorMessage;
        }
    }
}
