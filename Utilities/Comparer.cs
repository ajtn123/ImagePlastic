using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace ImagePlastic.Utilities;

//https://stackoverflow.com/a/54812587
[SuppressUnmanagedCodeSecurity]
public partial class IntuitiveStringComparer : IComparer<string>
{
    [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int StrCmpLogicalW(string psz1, string psz2);
    public int Compare(string? x, string? y)
        => (x == null || y == null) ? 0 : StrCmpLogicalW(x, y);
}