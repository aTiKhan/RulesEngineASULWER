// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Exceptions
{
    internal class ActionNotFoundException : Exception
    {
        public string ActionName { get; }
        public ActionNotFoundException(string actionName)
            : base($"Action with name: {actionName} does not exist")
        {
            ActionName = actionName;
        }
    }
}
