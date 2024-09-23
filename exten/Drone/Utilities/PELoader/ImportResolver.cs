using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using DInvoke.DynamicInvoke;
using Drone.Interop;

namespace Drone.Utilities.PELoader;

using static Data;

public sealed class ImportResolver
{
    private readonly List<string> _originalModules = new();

    public void ResolveImports(PeLoader pe, long currentBase)
    {
        using var currentProcess = Process.GetCurrentProcess();
        foreach (ProcessModule module in currentProcess.Modules)
            _originalModules.Add(module.ModuleName);

        var pIDT = (IntPtr)(currentBase + pe.OptionalHeader64.ImportTable.VirtualAddress);
        var dllIterator = 0;
        
        while (true)
        {
            var pDLLImportTableEntry = (IntPtr)(pIDT.ToInt64() + IDT_SINGLE_ENTRY_LENGTH * dllIterator);

            var iatRVA = Marshal.ReadInt32((IntPtr)(pDLLImportTableEntry.ToInt64() + IDT_IAT_OFFSET));
            var pIAT = (IntPtr)(currentBase + iatRVA);

            var dllNameRVA = Marshal.ReadInt32((IntPtr)(pDLLImportTableEntry.ToInt64() + IDT_DLL_NAME_OFFSET));
            var pDLLName = (IntPtr)(currentBase + dllNameRVA);
            var dllName = Marshal.PtrToStringAnsi(pDLLName);

            if (string.IsNullOrWhiteSpace(dllName))
                break;

            var handle = Generic.GetLoadedModuleAddress(dllName);
            
            if (handle == IntPtr.Zero)
                handle = Generic.LoadModuleFromDisk(dllName);

            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pCurrentIATEntry = pIAT;
            
            while (true)
            {
                var pDLLFuncName = (IntPtr)(currentBase + Marshal.ReadInt32(pCurrentIATEntry) + ILT_HINT_LENGTH);
                var dllFuncName = Marshal.PtrToStringAnsi(pDLLFuncName);

                if (string.IsNullOrWhiteSpace(dllFuncName))
                    break;

                var pRealFunction = Generic.GetNativeExportAddress(handle, dllFuncName);
                
                if (pRealFunction == IntPtr.Zero)
                    throw new Exception($"Unable to find procedure {dllName}!{dllFuncName}");

                Marshal.WriteInt64(pCurrentIATEntry, pRealFunction.ToInt64());

                pCurrentIATEntry = (IntPtr)(pCurrentIATEntry.ToInt64() + IntPtr.Size);
            }

            dllIterator++;
        }

    }

    public void ResetImports()
    {
        using var currentProcess = Process.GetCurrentProcess();
        foreach (ProcessModule module in currentProcess.Modules)
        {
            if (_originalModules.Contains(module.ModuleName))
                continue;

            Methods.FreeLibrary(module.BaseAddress);
        }
    }
}