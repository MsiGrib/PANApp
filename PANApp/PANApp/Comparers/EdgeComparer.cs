using System;
using System.Collections.Generic;

namespace PANApp.Comparers;

public sealed class EdgeComparer : IEqualityComparer<(string Source, string Target)>
{
    public bool Equals((string Source, string Target) x, (string Source, string Target) y)
        => string.Equals(x.Source, y.Source, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Target, y.Target, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string Source, string Target) obj)
        => HashCode.Combine(
            obj.Source.ToLowerInvariant(),
            obj.Target.ToLowerInvariant());
}