using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Drone.Interop;

namespace Drone.Utilities.PELoader;

using static Data;

public sealed class FileDescriptorPair
{
    public IntPtr Read { get; set; }

    public IntPtr Write { get; set; }
}

public sealed class FileDescriptorRedirector
{
    private IntPtr _oldGetStdHandleOut;
    private IntPtr _oldGetStdHandleIn;
    private IntPtr _oldGetStdHandleError;

    private FileDescriptorPair _kpStdOutPipes;
    private FileDescriptorPair _kpStdInPipes;
    private Task<string> _readTask;

    public bool RedirectFileDescriptors()
    {
        _oldGetStdHandleOut = GetStdHandleOut();
        _oldGetStdHandleIn = GetStdHandleIn();
        _oldGetStdHandleError = GetStdHandleError();

        _kpStdOutPipes = CreateFileDescriptorPipes();
        
        if (_kpStdOutPipes == null)
            throw new Exception("Unable to create STDOut Pipes");

        _kpStdInPipes = CreateFileDescriptorPipes();
        
        if (_kpStdInPipes == null)
            return false;

        return RedirectDescriptorsToPipes(_kpStdOutPipes.Write, _kpStdInPipes.Write, _kpStdOutPipes.Write);
    }

    public string ReadDescriptorOutput()
    {
        while (!_readTask.IsCompleted)
            Thread.Sleep(2000);

        return _readTask.Result;
    }

    public void ResetFileDescriptors()
    {
        RedirectDescriptorsToPipes(_oldGetStdHandleOut, _oldGetStdHandleIn, _oldGetStdHandleError);
        ClosePipes();
    }

    private static IntPtr GetStdHandleOut()
    {
        return Methods.GetStandardHandle(STD_OUTPUT_HANDLE);
    }

    private static IntPtr GetStdHandleError()
    {
        return Methods.GetStandardHandle(STD_ERROR_HANDLE);
    }

    private void ClosePipes()
    {
        CloseDescriptors(_kpStdOutPipes);
        CloseDescriptors(_kpStdInPipes);
    }

    public void StartReadFromPipe()
    {
        _readTask = Task.Factory.StartNew(() =>
        {
            var output = "";

            var buffer = new byte[BYTES_TO_READ];
            byte[] outBuffer;

            var ok = Methods.ReadFile(
                _kpStdOutPipes.Read,
                buffer,
                BYTES_TO_READ,
                out var bytesRead,
                IntPtr.Zero);

            if (!ok)
                return "";

            if (bytesRead != 0)
            {
                outBuffer = new byte[bytesRead];
                Array.Copy(buffer, outBuffer, bytesRead);
                output += Encoding.Default.GetString(outBuffer);
            }

            while (ok)
            {
                ok = Methods.ReadFile(
                    _kpStdOutPipes.Read,
                    buffer,
                    BYTES_TO_READ,
                    out bytesRead,
                    IntPtr.Zero);

                if (bytesRead != 0)
                {
                    outBuffer = new byte[bytesRead];
                    Array.Copy(buffer, outBuffer, bytesRead);
                    output += Encoding.Default.GetString(outBuffer);
                }
            }

            return output;
        });
    }

    private static IntPtr GetStdHandleIn()
    {
        return Methods.GetStandardHandle(STD_INPUT_HANDLE);
    }

    private static void CloseDescriptors(FileDescriptorPair stdoutDescriptors)
    {
        try
        {
            if (stdoutDescriptors.Write != IntPtr.Zero)
                Methods.CloseHandle(stdoutDescriptors.Write);

            if (stdoutDescriptors.Read != IntPtr.Zero)
                Methods.CloseHandle(stdoutDescriptors.Read);
        }
        catch
        {
            // meh
        }
    }

    private static FileDescriptorPair CreateFileDescriptorPipes()
    {
        var lpSecurityAttributes = new Data.SECURITY_ATTRIBUTES();
        lpSecurityAttributes.nLength = Marshal.SizeOf(lpSecurityAttributes);
        lpSecurityAttributes.bInheritHandle = true;

        var outputStdOut = Methods.CreatePipe(
            out var read,
            out var write,
            ref lpSecurityAttributes,
            0);
        
        if (!outputStdOut)
            return null;

        return new FileDescriptorPair
        {
            Read = read,
            Write = write
        };
    }

    private static bool RedirectDescriptorsToPipes(IntPtr hStdOutPipes, IntPtr hStdInPipes, IntPtr hStdErrPipes)
    {
        var bStdOut = Methods.SetStandardHandle(STD_OUTPUT_HANDLE, hStdOutPipes);
        
        if (!bStdOut)
            return false;

        var bStdError = Methods.SetStandardHandle(STD_ERROR_HANDLE, hStdErrPipes);
        
        if (!bStdError)
            return false;

        var bStdIn = Methods.SetStandardHandle(STD_INPUT_HANDLE, hStdInPipes);
        
        if (!bStdIn)
            return false;

        return true;
    }
}