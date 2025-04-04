﻿using System.Diagnostics;

namespace Sstv.DomainExceptions.DebugViewer;

/// <summary>
/// All configured domain exception details.
/// </summary>
[DebuggerDisplay("Count = {ErrorCodes.Count}")]
public class DomainExceptionDebugVm
{
    /// <summary>
    /// Configured error codes.
    /// </summary>
    public IReadOnlyCollection<DomainExceptionCodeDebugVm> ErrorCodes { get; set; } = [];
}