// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress ConfigureAwait warnings in test projects
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", 
    Justification = "ConfigureAwait(false) should not be used in test projects as per xUnit guidance")]

// Suppress underscore naming in test methods - this is a common convention
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
    Justification = "Test method names conventionally use underscores for readability")]

// Suppress URL parameter type warnings in test projects
[assembly: SuppressMessage("Design", "CA1054:URI parameters should not be strings",
    Justification = "String URLs are more convenient for testing")]

[assembly: SuppressMessage("Reliability", "CA2234:Pass System.Uri objects instead of strings",
    Justification = "String URLs are more convenient for testing")]

// Suppress Metalama version mismatch warnings
[assembly: SuppressMessage("Metalama", "LAMA0618:Analyzer assembly was disabled due to incompatible Roslyn version",
    Justification = "Known version mismatch that doesn't affect test functionality")]

// Suppress string comparison warnings in test assertions
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity",
    Justification = "String comparison culture is not relevant in test assertions")]

// Suppress constant array warnings in test methods
[assembly: SuppressMessage("Performance", "CA1861:Prefer static readonly fields over constant array arguments",
    Justification = "Test readability is more important than performance in test methods")]

// Suppress visible field warnings in test classes
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields",
    Justification = "Fields in test classes don't need to be encapsulated")]

// Suppress unsealed attribute warnings in test classes
[assembly: SuppressMessage("Performance", "CA1813:Avoid unsealed attributes",
    Justification = "Test attributes don't need to be sealed")]

// Suppress class sealing warnings in test classes
[assembly: SuppressMessage("Performance", "CA1852:Seal internal types",
    Justification = "Test classes don't need to be sealed")]

// Suppress null parameter warnings in test methods
[assembly: SuppressMessage("xUnit", "xUnit1012:Null should not be used for type parameter",
    Justification = "Testing null scenarios is important for validation tests")]

// Suppress argument validation warnings in test classes
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods",
    Justification = "Test classes don't need argument validation")]
