using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace ImagePlastic.Utilities;

//https://stackoverflow.com/a/54812587
public partial class IntuitiveStringComparer : IComparer<string>
{
    [SuppressUnmanagedCodeSecurity]
    [SupportedOSPlatform("windows7.0")]
    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int StrCmpLogicalW(string psz1, string psz2);
    public int Compare(string? x, string? y)
    {
        if (x == null) return y == null ? 0 : -1;
        if (y == null) return 1;
        return StrCmpLogicalW(x, y);
    }
}