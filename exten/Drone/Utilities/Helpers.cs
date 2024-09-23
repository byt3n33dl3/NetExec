using System;
using System.Linq;
using System.Runtime.InteropServices;

using DInvoke.DynamicInvoke;

using Drone.Interop;
using Native = DInvoke.Data.Native;

namespace Drone.Utilities;

public static class Helpers
{
    public static string GenerateShortGuid()
    {
        return Guid.NewGuid()
            .ToString()
            .Replace("-", "")
            .Substring(0, 10);
    }

    public static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rand = new Random();
        
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[rand.Next(s.Length)])
            .ToArray());
    }

    public static TimeSpan CalculateSleepTime(int interval, int jitter)
    {
        var diff = (int)Math.Round((double)interval / 100 * jitter);

        var min = interval - diff;
        var max = interval + diff;

        var rand = new Random();
        return new TimeSpan(0, 0, rand.Next(min, max));
    }

    internal static byte[] PatchFunction(string dllName, string funcName, byte[] patchBytes)
    {
        if (!dllName.EndsWith(".dll"))
            dllName = $"{dllName}.dll";

        var pFunc = Generic.GetLibraryAddress(dllName, funcName);

        var originalBytes = new byte[patchBytes.Length];
        Marshal.Copy(pFunc, originalBytes, 0, patchBytes.Length);

        var status = Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            pFunc,
            patchBytes.Length,
            Data.MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE,
            out var oldProtect);

        if (status != Native.NTSTATUS.Success)
            return null;

        Marshal.Copy(patchBytes, 0, pFunc, patchBytes.Length);

        Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            pFunc,
            patchBytes.Length,
            oldProtect,
            out _);

        return originalBytes;
    }

    internal static bool PatchAddress(IntPtr pAddress, IntPtr newValue)
    {
        var status = Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            pAddress,
            IntPtr.Size,
            Data.MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE,
            out var oldProtect);

        if (status != Native.NTSTATUS.Success)
            return false;

        Marshal.WriteIntPtr(pAddress, newValue);

        status = Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            pAddress,
            IntPtr.Size,
            oldProtect,
            out _);

        return status == Native.NTSTATUS.Success;
    }

    internal static bool ZeroOutMemory(IntPtr start, int length)
    {
        var status = Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            start,
            length,
            Data.MEMORY_PROTECTION.PAGE_READWRITE,
            out var oldProtect);

        if (status != Native.NTSTATUS.Success)
            return false;

        var zeroes = new byte[length];

        for (var i = 0; i < zeroes.Length; i++)
            zeroes[i] = 0x00;

        Marshal.Copy(zeroes, 0, start, length);

        status = Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            start,
            length,
            oldProtect,
            out _);

        return status == Native.NTSTATUS.Success;
    }

    internal static void FreeMemory(IntPtr address)
    {
        Methods.NtFreeVirtualMemory(
            new IntPtr(-1),
            address,
            0);
    }

    internal static IntPtr GetPointerToPeb()
    {
        var status = Methods.NtQueryInformationProcess(
            new IntPtr(-1),
            Data.PROCESS_INFO_CLASS.ProcessBasicInformation,
            out var processBasicInformation);

        var pPEB = IntPtr.Zero;

        if (status == Native.NTSTATUS.Success)
            pPEB = Marshal.PtrToStructure<Data.PROCESS_BASIC_INFORMATION>(processBasicInformation).PebBaseAddress;

        if (processBasicInformation != IntPtr.Zero)
            Marshal.FreeHGlobal(processBasicInformation);

        return pPEB;
    }

    public static byte[] ReadMemory(IntPtr address, int length)
    {
        var bytes = new byte[length];
        Marshal.Copy(address, bytes, 0, length);
        return bytes;
    }
}