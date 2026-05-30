// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.HelperFunctions;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace RulesEngine
{
    /// <summary>
    /// Provides custom types to System.Linq.Dynamic.Core for use in dynamic rule expressions.
    /// </summary>
    public class CustomTypeProvider : DefaultDynamicLinqCustomTypeProvider
    {
        private HashSet<Type> _types;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTypeProvider"/> class.
        /// </summary>
        /// <param name="types">An array of custom types to make available in dynamic expressions.</param>
        public CustomTypeProvider(Type[] types) : base(ParsingConfig.Default)
        {
            _types = new HashSet<Type>(types ?? new Type[] { }) {
                typeof(Object),
                typeof(ExpressionUtils)
            };
        }

        /// <summary>
        /// Gets the set of custom types available in dynamic expressions.
        /// </summary>
        /// <returns>A hash set of custom types.</returns>
        public override HashSet<Type> GetCustomTypes()
        {
            return _types;
        }
    }
}
