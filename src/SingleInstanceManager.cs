using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMyRam;

/// <summary>
/// Manages single instance behavior for the application.
/// Uses a Mutex to ensure only one instance runs, and Named Pipes to communicate
/// between the new instance and the running instance.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    private const string MutexName = "FreeMyRam_SingleInstance_Mutex";
    private const string PipeName = "FreeMyRam_SingleInstance_Pipe";
    
    private Mutex? _mutex;
    private CancellationTokenSource? _pipeServerCts;
    private Task? _pipeServerTask;
    
    /// <summary>
    /// Event raised when another instance tries to start.
    /// The running instance should show its window when this event is raised.
    /// </summary>
    public event Action? SecondInstanceStarted;
    
    /// <summary>
    /// Attempts to acquire single instance lock.
    /// </summary>
    /// <returns>True if this is the first instance, false if another instance is already running.</returns>
    public bool TryAcquireLock()
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            
            if (createdNew)
            {
                // This is the first instance - start listening for other instances
                StartPipeServer();
                return true;
            }
            else
            {
                // Another instance is already running - signal it and exit
                SignalExistingInstance();
                return false;
            }
        }
        catch
        {
            return true; // If mutex fails, allow app to run anyway
        }
    }
    
    /// <summary>
    /// Starts the named pipe server to listen for signals from other instances.
    /// </summary>
    private void StartPipeServer()
    {
        _pipeServerCts = new CancellationTokenSource();
        var token = _pipeServerCts.Token;
        
        _pipeServerTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    
                    await server.WaitForConnectionAsync(token);
                    
                    // Read the signal (just a single byte to confirm connection)
                    byte[] buffer = new byte[1];
                    await server.ReadAsync(buffer, 0, 1, token);
                    
                    // Raise event on UI thread
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        SecondInstanceStarted?.Invoke();
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ignore pipe errors and continue listening
                    await Task.Delay(100, token);
                }
            }
        }, token);
    }
    
    /// <summary>
    /// Signals the existing instance to show its window.
    /// </summary>
    private static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000); // 2 second timeout
            
            // Send a signal byte
            client.WriteByte(1);
            client.Flush();
        }
        catch
        {
            // If signaling fails, the user may need to manually bring up the window
        }
    }
    
    /// <summary>
    /// Releases the mutex and stops the pipe server.
    /// </summary>
    public void Dispose()
    {
        _pipeServerCts?.Cancel();
        
        try
        {
            _pipeServerTask?.Wait(1000);
        }
        catch
        {
            // Ignore timeout
        }
        
        _pipeServerCts?.Dispose();
        _pipeServerCts = null;
        
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // Ignore if already released
        }
        
        _mutex?.Dispose();
        _mutex = null;
    }
}
