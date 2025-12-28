using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeMyRam;

/// <summary>
/// Provides methods to clean and free system memory using Windows API
/// </summary>
public class MemoryCleaner
{
    #region Native Methods

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    #endregion

    #region Constants

    private const int SystemMemoryListInformation = 80;
    private const int MemoryEmptyWorkingSets = 2;
    private const int MemoryFlushModifiedList = 3;
    private const int MemoryPurgeStandbyList = 4;
    private const int MemoryPurgeLowPriorityStandbyList = 5;

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
    private const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    #endregion

    public MemoryCleaner()
    {
        EnablePrivileges();
    }

    private void EnablePrivileges()
    {
        EnablePrivilege(SE_INCREASE_QUOTA_NAME);
        EnablePrivilege(SE_PROFILE_SINGLE_PROCESS_NAME);
    }

    private bool EnablePrivilege(string privilegeName)
    {
        try
        {
            IntPtr currentProcess = GetCurrentProcess();
            if (!OpenProcessToken(currentProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
                return false;

            try
            {
                if (!LookupPrivilegeValue(null, privilegeName, out LUID luid))
                    return false;

                TOKEN_PRIVILEGES tokenPrivileges = new()
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES
                    {
                        Luid = luid,
                        Attributes = SE_PRIVILEGE_ENABLED
                    }
                };

                return AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Empty working sets for all running processes
    /// </summary>
    public void EmptyWorkingSets()
    {
        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                EmptyWorkingSet(process.Handle);
            }
            catch
            {
                // Skip processes we can't access
            }
        }
    }

    /// <summary>
    /// Empty the current process working set (system)
    /// </summary>
    public void EmptySystemWorkingSet()
    {
        try
        {
            IntPtr handle = GetCurrentProcess();
            SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
            
            // Also use NtSetSystemInformation for system-wide effect
            int command = MemoryEmptyWorkingSets;
            IntPtr commandPtr = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(commandPtr, command);
                NtSetSystemInformation(SystemMemoryListInformation, commandPtr, sizeof(int));
            }
            finally
            {
                Marshal.FreeHGlobal(commandPtr);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Empty the modified page list - flushes modified pages to disk
    /// </summary>
    public void EmptyModifiedPageList()
    {
        try
        {
            int command = MemoryFlushModifiedList;
            IntPtr commandPtr = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(commandPtr, command);
                NtSetSystemInformation(SystemMemoryListInformation, commandPtr, sizeof(int));
            }
            finally
            {
                Marshal.FreeHGlobal(commandPtr);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Empty the standby list - frees cached memory
    /// </summary>
    public void EmptyStandbyList()
    {
        try
        {
            int command = MemoryPurgeStandbyList;
            IntPtr commandPtr = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(commandPtr, command);
                NtSetSystemInformation(SystemMemoryListInformation, commandPtr, sizeof(int));
            }
            finally
            {
                Marshal.FreeHGlobal(commandPtr);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Empty only the priority 0 standby list (less aggressive)
    /// </summary>
    public void EmptyPriority0StandbyList()
    {
        try
        {
            int command = MemoryPurgeLowPriorityStandbyList;
            IntPtr commandPtr = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(commandPtr, command);
                NtSetSystemInformation(SystemMemoryListInformation, commandPtr, sizeof(int));
            }
            finally
            {
                Marshal.FreeHGlobal(commandPtr);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Clean temporary files from Windows Temp folders
    /// </summary>
    /// <returns>Tuple containing files deleted count and total bytes cleaned</returns>
    public (int filesDeleted, long bytesFreed) CleanTempFiles()
    {
        int filesDeleted = 0;
        long bytesFreed = 0;

        string[] tempFolders = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "INetCache"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer")
        };

        foreach (string folder in tempFolders)
        {
            if (!Directory.Exists(folder))
                continue;

            try
            {
                // Clean files
                foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        long fileSize = fileInfo.Length;
                        
                        // Skip files that are currently in use (modified in last 5 minutes)
                        if ((DateTime.Now - fileInfo.LastWriteTime).TotalMinutes < 5)
                            continue;

                        fileInfo.Delete();
                        filesDeleted++;
                        bytesFreed += fileSize;
                    }
                    catch
                    {
                        // Skip files that can't be deleted (in use, etc.)
                    }
                }

                // Clean empty directories
                foreach (string dir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (Directory.GetFileSystemEntries(dir).Length == 0)
                        {
                            Directory.Delete(dir, false);
                        }
                    }
                    catch
                    {
                        // Skip directories that can't be deleted
                    }
                }
            }
            catch
            {
                // Skip folders we can't access
            }
        }

        return (filesDeleted, bytesFreed);
    }

    /// <summary>
    /// Empty the Windows Recycle Bin
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public bool EmptyRecycleBin()
    {
        try
        {
            // Empty recycle bin for all drives without confirmation dialog
            int result = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            // S_OK (0) or S_FALSE (1) means success (1 means bin was already empty)
            return result == 0 || result == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the size of files in the Recycle Bin
    /// </summary>
    /// <returns>Total size in bytes</returns>
    public long GetRecycleBinSize()
    {
        long totalSize = 0;
        try
        {
            string recyclePath = @"$Recycle.Bin";
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed)
                    continue;

                string binPath = Path.Combine(drive.RootDirectory.FullName, recyclePath);
                if (Directory.Exists(binPath))
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(binPath, "*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                totalSize += new FileInfo(file).Length;
                            }
                            catch
                            {
                                // Skip files we can't access
                            }
                        }
                    }
                    catch
                    {
                        // Skip directories we can't access
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        return totalSize;
    }
}
