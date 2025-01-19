using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace ImagePlastic.Utilities;

//https://stackoverflow.com/a/54812587
[SuppressUnmanagedCodeSecurity]
internal static class SafeNativeMethods
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    internal static extern Int32 StrCmpLogicalW(string psz1, string psz2);
}

public class IntuitiveStringComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) return -1;
        else return SafeNativeMethods.StrCmpLogicalW(x, y);
    }
}