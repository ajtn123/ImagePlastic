using ImagePlastic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace ImagePlastic.Utilities;

[SupportedOSPlatform("windows7.0")]
public static partial class ShellPropertyHelper
{
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHGetPropertyStoreFromParsingName(string pszPath, IntPtr pbc, int flags, ref Guid riid, out IPropertyStore ppv);
    [LibraryImport("oleaut32.dll")]
    private static partial void VariantInit(IntPtr pvarg);
    [LibraryImport("propsys.dll")]
    private static partial int PropVariantToVariant(ref PropVariant pPropVar, IntPtr pVar);
    [LibraryImport("ole32.dll")]
    private static partial int PropVariantClear(ref PropVariant pvar);
    [LibraryImport("oleaut32.dll")]
    private static partial void VariantClear(IntPtr pvarg);

    [GeneratedComInterface]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    internal partial interface IPropertyStore
    {
        void GetCount(out uint count);
        void GetAt(uint index, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropVariant
    {
        public ushort vt;
        public IntPtr pointerValue;
    }

    public static List<Prop> IterateFileProperties(string filePath)
    {
        Guid iPropertyStoreGuid = typeof(IPropertyStore).GUID;
        List<Prop> properties = [];

        var errorCode = SHGetPropertyStoreFromParsingName(filePath, IntPtr.Zero, 0, ref iPropertyStoreGuid, out IPropertyStore propertyStore);
        if (errorCode < 0) { Trace.WriteLine("Property iteration error: " + Marshal.GetExceptionForHR(errorCode)?.Message); return properties; }

        propertyStore.GetCount(out uint count);

        for (uint i = 0; i < count; i++)
        {
            IntPtr variantPtr = Marshal.AllocCoTaskMem(24);
            VariantInit(variantPtr);
            propertyStore.GetAt(i, out PropertyKey propertyKey);
            object? value = null;

            try
            {
                propertyStore.GetValue(ref propertyKey, out PropVariant propValue);

                if (PropVariantToVariant(ref propValue, variantPtr) == 0)
                    value = Marshal.GetObjectForNativeVariant(variantPtr);

                PropVariantClear(ref propValue);
            }
            catch (Exception ex) { Trace.WriteLine($"Failed to get value for {propertyKey.fmtid} {propertyKey.pid}: {ex.Message}"); }
            finally { VariantClear(variantPtr); Marshal.FreeCoTaskMem(variantPtr); }

            var info = PropertyInfos.FirstOrDefault(i => i.FmtID.Equals(propertyKey.fmtid) && i.PID == propertyKey.pid);
            string propertyName = info?.Name ?? info?.CanonicalName ?? $"{propertyKey.fmtid:B} {propertyKey.pid}";
            string valueString;

            if (value == null) valueString = "";
            else if (value.GetType().Equals(typeof(string[]))) valueString = ((string[])value).Aggregate((a, b) => $"{a} | {b}");
            else valueString = value.ToString() ?? $"[{value.GetType().Name}]";

            properties.Add(new(propertyName, valueString));
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

            properties.Add(new(pd) { FmtID = pk.fmtid, PID = pk.pid, CanonicalName = Marshal.PtrToStringUni((IntPtr)cname.Value) });

            Marshal.FreeCoTaskMem((IntPtr)cname.Value);
        }
        return properties;
    }
    private class PropertyInfo
    {
        public unsafe PropertyInfo(IPropertyDescription pd)
        {
            PD = pd;
            name = new(() =>
            {
                try
                {
                    pd.GetDisplayName(out var dname);
                    var displayName = Marshal.PtrToStringUni((IntPtr)dname.Value);
                    Marshal.FreeCoTaskMem((nint)dname.Value);
                    return displayName;
                }
                catch (Exception ex) { Trace.WriteLine($"Failed to get display name of {CanonicalName}: {ex.Message}"); }
                return null;
            });
        }

        public IPropertyDescription PD;
        public Guid FmtID { get; set; }
        public uint PID { get; set; }
        public string? CanonicalName { get; set; }
        public string? Name => name.Value;

        private readonly unsafe Lazy<string?> name;
    }
}

[SupportedOSPlatform("windows7.0")]
public class ExplorerPropertiesOpener
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHELLEXECUTEINFO
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

    public static void OpenFileProperties(string filePath)
    {
        SHELLEXECUTEINFO sei = new();
        sei.cbSize = Marshal.SizeOf(sei);
        sei.lpVerb = "properties";
        sei.lpFile = filePath;
        sei.nShow = 0;
        sei.fMask = SEE_MASK_INVOKEIDLIST;

        if (!ShellExecuteEx(ref sei))
            Trace.WriteLine("Failed to open properties window.");
    }
}
