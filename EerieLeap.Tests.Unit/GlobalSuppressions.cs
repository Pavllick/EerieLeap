// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress xUnit1012 warnings about null string parameters in test methods
[assembly: SuppressMessage("xUnit", "xUnit1012:Null should not be used for type parameter",
    Justification = "Testing null string scenarios is a valid test case in our unit tests")]
