using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FFmpeg.AutoGen.Native
{
    public delegate PlatformID GetPlatformId();

    public delegate string GetNativeLibraryName(string libraryName, int version);

    internal static class NativeLibraryLoader
    {
        internal static void SetDllMap()
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                NativeLibrary.SetDllImportResolver(assembly, MapWindows);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                NativeLibrary.SetDllImportResolver(assembly, MapLinux);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                NativeLibrary.SetDllImportResolver(assembly, MapMac);
        }

        static (string, string) GetNameVer(string name)
        {
            return (name.Substring(0, name.LastIndexOf('.')), name.Substring(name.LastIndexOf('.') + 1));
        }

        static IntPtr MapWindows(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            string ver;
            (libraryName, ver) = GetNameVer(libraryName);
            libraryName += "-" + ver + ".dll";
            return NativeLibrary.Load(libraryName, assembly, dllImportSearchPath);
        }

        static IntPtr MapLinux(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            string ver;
            (libraryName, ver) = GetNameVer(libraryName);
            libraryName += ".so." + ver;
            return NativeLibrary.Load(libraryName, assembly, dllImportSearchPath);
        }

        static IntPtr MapMac(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
        {
            string ver;
            (libraryName, ver) = GetNameVer(libraryName);
            libraryName += "." + ver + ".dylib";
            return NativeLibrary.Load(libraryName, assembly, dllImportSearchPath);
        }
    }

    public static class PlatformInfo
    {
        static PlatformInfo()
        {
            GetPlatformId = () =>
            {
#if NET45 || NET40
                return Environment.OSVersion.Platform;
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return PlatformID.Win32NT;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return PlatformID.Unix;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return PlatformID.MacOSX;
                throw new PlatformNotSupportedException();
#endif
            };

            switch (GetPlatformId())
            {
                case PlatformID.MacOSX:
                    GetNativeLibraryName = (libraryName, version) => $"lib{libraryName}.{version}.dylib";
                    break;
                case PlatformID.Unix:
                    GetNativeLibraryName = (libraryName, version) => $"lib{libraryName}.so.{version}";
                    break;
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    GetNativeLibraryName = (libraryName, version) => $"{libraryName}-{version}.dll";
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static GetPlatformId GetPlatformId;

        public static GetNativeLibraryName GetNativeLibraryName;
    }
}