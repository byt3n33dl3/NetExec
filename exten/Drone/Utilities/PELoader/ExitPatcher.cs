using System;
using System.Collections.Generic;

using DInvoke.DynamicInvoke;

namespace Drone.Utilities.PELoader;

public sealed class ExitPatcher
{
    private byte[] _terminateProcessOriginalBytes;
    private byte[] _ntTerminateProcessOriginalBytes;
    private byte[] _rtlExitUserProcessOriginalBytes;
    private byte[] _corExitProcessOriginalBytes;

    public bool PatchExit()
    {
        var pExitThreadFunc = Generic.GetLibraryAddress("kernelbase.dll", "ExitThread");
        var exitThreadPatchBytes = new List<byte> { 0x48, 0xC7, 0xC1, 0x00, 0x00, 0x00, 0x00, 0x48, 0xB8 };
        var pointerBytes = BitConverter.GetBytes(pExitThreadFunc.ToInt64());

        exitThreadPatchBytes.AddRange(pointerBytes);
        exitThreadPatchBytes.Add(0x50);
        exitThreadPatchBytes.Add(0xC3);

        _terminateProcessOriginalBytes =
            Helpers.PatchFunction("kernelbase.dll", "TerminateProcess", exitThreadPatchBytes.ToArray());

        if (_terminateProcessOriginalBytes == null)
            return false;

        _corExitProcessOriginalBytes =
            Helpers.PatchFunction("mscoree.dll", "CorExitProcess", exitThreadPatchBytes.ToArray());

        if (_corExitProcessOriginalBytes == null)
            return false;

        _ntTerminateProcessOriginalBytes =
            Helpers.PatchFunction("ntdll.dll", "NtTerminateProcess", exitThreadPatchBytes.ToArray());

        if (_ntTerminateProcessOriginalBytes == null)
            return false;

        _rtlExitUserProcessOriginalBytes =
            Helpers.PatchFunction("ntdll.dll", "RtlExitUserProcess", exitThreadPatchBytes.ToArray());

        if (_rtlExitUserProcessOriginalBytes == null)
            return false;

        return true;
    }

    public void ResetExitFunctions()
    {
        Helpers.PatchFunction("kernelbase.dll", "TerminateProcess", _terminateProcessOriginalBytes);
        Helpers.PatchFunction("mscoree.dll", "CorExitProcess", _corExitProcessOriginalBytes);
        Helpers.PatchFunction("ntdll.dll", "NtTerminateProcess", _ntTerminateProcessOriginalBytes);
        Helpers.PatchFunction("ntdll.dll", "RtlExitUserProcess", _rtlExitUserProcessOriginalBytes);
    }
}