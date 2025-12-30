using System.Runtime.InteropServices;
using System.Diagnostics;

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

    // Static counters for Standby Cache (matches Task Manager's "Cached")
    private static PerformanceCounter? _standbyCoreBytesCounter;
    private static PerformanceCounter? _standbyNormalBytesCounter;
    private static PerformanceCounter? _standbyReserveBytesCounter;
    private static bool _countersInitialized;

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

    /// <summary>
    /// Gets the amount of cached/standby memory in bytes (matches Task Manager's "Cached")
    /// </summary>
    public static ulong GetCachedBytes()
    {
        try
        {
            // Initialize counters on first call
            if (!_countersInitialized)
            {
                _standbyCoreBytesCounter = new PerformanceCounter("Memory", "Standby Cache Core Bytes", true);
                _standbyNormalBytesCounter = new PerformanceCounter("Memory", "Standby Cache Normal Priority Bytes", true);
                _standbyReserveBytesCounter = new PerformanceCounter("Memory", "Standby Cache Reserve Bytes", true);
                
                // Prime the counters (first call returns 0)
                _standbyCoreBytesCounter.NextValue();
                _standbyNormalBytesCounter.NextValue();
                _standbyReserveBytesCounter.NextValue();
                _countersInitialized = true;
            }
            
            // Sum all standby cache components = Task Manager's "Cached"
            float coreBytes = _standbyCoreBytesCounter?.NextValue() ?? 0;
            float normalBytes = _standbyNormalBytesCounter?.NextValue() ?? 0;
            float reserveBytes = _standbyReserveBytesCounter?.NextValue() ?? 0;
            
            return (ulong)(coreBytes + normalBytes + reserveBytes);
        }
        catch
        {
            // Fallback: return 0 if performance counters fail
            return 0;
        }
    }
}

