using ImagePlastic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.UI.Shell.PropertiesSystem;

[assembly: SupportedOSPlatform("windows6.0.6000")]

namespace ImagePlastic.Utilities;

public static class ShellPropertyHelper
{
    [DllImport("ole32.dll", PreserveSig = false)]
    private static extern void PropVariantClear(ref PropVariant pvar);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    public static extern void SHGetPropertyStoreFromParsingName(string pszPath, IntPtr pbc, int flags, ref Guid riid, out IntPtr ppv);

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr p; // String pointer (if applicable)
        [FieldOffset(8)] public int iVal; // Integer value (if applicable)
        [FieldOffset(8)] public long lVal; // Large integer (FILETIME, etc.)

        public readonly object? GetValue()
        {
            switch (vt)
            {
                case 0: //VT_EMPTY (empty)
                    return null;

                case 31: // VT_LPWSTR (String)
                    return p != IntPtr.Zero ? Marshal.PtrToStringUni(p) : null;

                case 3:  // VT_I4 (Integer)
                    return iVal;

                //case 5:  // VT_R8 (Double)
                //    return p != IntPtr.Zero ? BitConverter.Int64BitsToDouble(lVal) : null;

                case 11: // VT_BOOL (Boolean)
                    return iVal != 0;

                case 64: // VT_FILETIME (DateTime)
                    return p != IntPtr.Zero ? DateTime.FromFileTime(lVal) : null;

                default:
                    return $"Unsupported type: {vt}";
            }
        }
    }

    public static List<Prop> IterateFileProperties(string filePath)
    {
        IntPtr propertyStorePtr = IntPtr.Zero;
        IPropertyStore? propertyStore = null;
        List<Prop> properties = [];
        try
        {
            Guid iPropertyStoreGuid = typeof(IPropertyStore).GUID;
            SHGetPropertyStoreFromParsingName(filePath, IntPtr.Zero, 0, ref iPropertyStoreGuid, out propertyStorePtr);
            propertyStore = (IPropertyStore)Marshal.GetObjectForIUnknown(propertyStorePtr);
            propertyStore.GetCount(out uint count);
            for (uint i = 0; i < count; i++)
            {
                propertyStore.GetAt(i, out PropertyKey propertyKey);
                var info = PropertyInfos.FirstOrDefault(i => i.FmtID.Equals(propertyKey.fmtid) && i.PID == propertyKey.pid);

                string propertyName = info?.Name ?? info?.CanonicalName ?? $"{propertyKey.fmtid:B} {propertyKey.pid}";
                try
                {
                    propertyStore.GetValue(ref propertyKey, out PropVariant propValue);
                    object? value = propValue.GetValue(); // Read the value safely
                    PropVariantClear(ref propValue); // Free memory after use
                    properties.Add(new(propertyName, value?.ToString() ?? ""));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get value for {propertyKey.fmtid} {propertyKey.pid}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            if (propertyStorePtr != IntPtr.Zero)
                Marshal.Release(propertyStorePtr);
        }
        return properties;
    }


    //https://stackoverflow.com/a/78716810
    private static readonly List<PropertyInfo> PropertyInfos = IterateProperties();
    public static List<Prop> GetMap() => [.. PropertyInfos.Select(p => new Prop(p.CanonicalName ?? "", $"{p.FmtID:B} {p.PID}"))];
    private static unsafe List<PropertyInfo> IterateProperties()
    {
        List<PropertyInfo> properties = [];
        PInvoke.PSEnumeratePropertyDescriptions(PROPDESC_ENUMFILTER.PDEF_ALL, typeof(IPropertyDescriptionList).GUID, out var ppv).ThrowOnFailure();
        var list = (IPropertyDescriptionList)Marshal.GetTypedObjectForIUnknown((nint)ppv, typeof(IPropertyDescriptionList));
        list.GetCount(out var count);
        for (uint i = 0; i < count; i++)
        {
            list.GetAt(i, typeof(IPropertyDescription).GUID, out var obj);
            var pd = (IPropertyDescription)obj;

            pd.GetPropertyKey(out var pk);
            pd.GetCanonicalName(out var cname);
            Marshal.FreeCoTaskMem((nint)cname.Value);

            properties.Add(new(pd) { FmtID = pk.fmtid, PID = pk.pid, CanonicalName = Marshal.PtrToStringUni((IntPtr)cname.Value) });
        }
        return properties;
    }
    private class PropertyInfo(IPropertyDescription pd)
    {
        public IPropertyDescription PD = pd;
        public Guid FmtID { get; set; }
        public uint PID { get; set; }
        public string? CanonicalName { get; set; }
        public string? Name => name.Value;

        private readonly unsafe Lazy<string?> name = new(() =>
        {
            try
            {
                pd.GetDisplayName(out var dname);
                var displayName = Marshal.PtrToStringUni((IntPtr)dname.Value);
                Marshal.FreeCoTaskMem((nint)dname.Value);
                return displayName;
            }
            catch { }
            return null;
        });
    }
}

public class FilePropertiesOpener
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

    public static void OpenFileProperties(string filePath)
    {
        SHELLEXECUTEINFO sei = new();
        sei.cbSize = Marshal.SizeOf(sei);
        sei.lpVerb = "properties"; // Open Properties window
        sei.lpFile = filePath;
        sei.nShow = 0;
        sei.fMask = SEE_MASK_INVOKEIDLIST;

        if (!ShellExecuteEx(ref sei))
        {
            Console.WriteLine("Failed to open properties window.");
        }
    }
}
