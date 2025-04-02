using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace ImagePlastic.Utilities;

//https://stackoverflow.com/a/54812587
[SuppressUnmanagedCodeSecurity]
public class IntuitiveStringComparer : IComparer<string>
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);
    public int Compare(string? x, string? y)
        => (x == null || y == null) ? 0 : StrCmpLogicalW(x, y);
}