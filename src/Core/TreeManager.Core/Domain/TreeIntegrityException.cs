using System;

namespace TreeManager.Core.Domain;

/// <summary>Thrown when the person tree contains structural corruption that makes lineage generation unsafe.</summary>
public sealed class TreeIntegrityException : Exception
{
    public TreeIntegrityException(string message) : base(message) { }
    public TreeIntegrityException(string message, Exception inner) : base(message, inner) { }
}
