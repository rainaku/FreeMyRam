using System.Runtime.InteropServices;

namespace FreeMyRam;

/// <summary>
/// Provides system memory information
/// </summary>
public static class MemoryInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint MemoryLoad;
        public ulong TotalPhysicalMemory;
        public ulong AvailablePhysicalMemory;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtualMemory;
        public ulong AvailableVirtualMemory;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>
    /// Gets current system memory status
    /// </summary>
    public static MEMORYSTATUSEX GetMemoryStatus()
    {
        MEMORYSTATUSEX memStatus = new()
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };
        
        GlobalMemoryStatusEx(ref memStatus);
        return memStatus;
    }
}
