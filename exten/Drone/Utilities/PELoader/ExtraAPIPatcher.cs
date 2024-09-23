using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Drone.Interop;
using DInvoke.DynamicInvoke;

namespace Drone.Utilities.PELoader;

public sealed class ExtraAPIPatcher
{
    private byte[] _originalGetModuleHandleBytes;
    private string _getModuleHandleFuncName;
    private IntPtr _newFuncAlloc;
    private int _newFuncBytesCount;

    public bool PatchAPIs(IntPtr baseAddress)
    {
        _getModuleHandleFuncName = Encoding.UTF8.Equals(Encoding.ASCII) ? "GetModuleHandleA" : "GetModuleHandleW";

        var getModuleHandleFuncAddress = Generic.GetLibraryAddress("kernelbase.dll", _getModuleHandleFuncName);

        var patchLength = CalculatePatchLength(getModuleHandleFuncAddress);
        WriteNewFuncToMemory(baseAddress, getModuleHandleFuncAddress, patchLength);

        return PatchAPIToJmpToNewFunc(patchLength);
    }

    private bool PatchAPIToJmpToNewFunc(int patchLength)
    {
        var pointerBytes = BitConverter.GetBytes(_newFuncAlloc.ToInt64());

        var patchBytes = new List<byte> { 0x48, 0xB8 };
        patchBytes.AddRange(pointerBytes);

        patchBytes.Add(0xFF);
        patchBytes.Add(0xE0);

        if (patchBytes.Count > patchLength)
            throw new Exception(
                $"Patch length ({patchBytes.Count})is greater than calculated space available ({patchLength})");

        if (patchBytes.Count < patchLength)
            patchBytes.AddRange(Enumerable.Range(0, patchLength - patchBytes.Count).Select(x => (byte)0x90));

        _originalGetModuleHandleBytes =
            Helpers.PatchFunction("kernelbase.dll", _getModuleHandleFuncName, patchBytes.ToArray());
        
        return _originalGetModuleHandleBytes != null;
    }

    private IntPtr WriteNewFuncToMemory(IntPtr baseAddress, IntPtr getModuleHandleFuncAddress, int patchLength)
    {
        var newFuncBytes = new List<byte>
        {
            0x48, 0x85, 0xc9, 0x75, 0x0b,
            0x48,
            0xB8
        };

        var baseAddressPointerBytes = BitConverter.GetBytes(baseAddress.ToInt64());

        newFuncBytes.AddRange(baseAddressPointerBytes);
        newFuncBytes.Add(0xC3);
        newFuncBytes.Add(0x48);
        newFuncBytes.Add(0xB8);

        var pointerBytes = BitConverter.GetBytes(getModuleHandleFuncAddress.ToInt64() + patchLength);

        newFuncBytes.AddRange(pointerBytes);

        var originalInstructions = new byte[patchLength];
        Marshal.Copy(getModuleHandleFuncAddress, originalInstructions, 0, patchLength);

        newFuncBytes.AddRange(originalInstructions);

        newFuncBytes.Add(0xFF);
        newFuncBytes.Add(0xE0);

        Methods.NtAllocateVirtualMemory(
            new IntPtr(-1),
            newFuncBytes.Count,
            Data.MEMORY_ALLOCATION.MEM_COMMIT,
            Data.MEMORY_PROTECTION.PAGE_READWRITE,
            ref _newFuncAlloc);

        Marshal.Copy(newFuncBytes.ToArray(), 0, _newFuncAlloc, newFuncBytes.Count);
        _newFuncBytesCount = newFuncBytes.Count;

        Methods.NtProtectVirtualMemory(
            new IntPtr(-1),
            _newFuncAlloc,
            newFuncBytes.Count,
            Data.MEMORY_PROTECTION.PAGE_EXECUTE_READ,
            out _);

        return _newFuncAlloc;
    }

    private static int CalculatePatchLength(IntPtr funcAddress)
    {
        var bytes = Helpers.ReadMemory(funcAddress, 40);
        var searcher = new BoyerMoore(new byte[] { 0x48, 0x8d, 0x4c });
        var length = searcher.Search(bytes).FirstOrDefault();

        if (length == 0)
        {
            searcher = new BoyerMoore(new byte[] { 0x4c, 0x8d, 0x44 });
            length = searcher.Search(bytes).FirstOrDefault();

            if (length == 0)
                throw new Exception(
                    "Unable to calculate patch length, the function may have changed to a point it is is no longer recognised and this code needs to be updated");
        }

        return length;
    }

    public bool RevertAPIs()
    {
        Helpers.PatchFunction("kernelbase.dll", _getModuleHandleFuncName, _originalGetModuleHandleBytes);
        Helpers.ZeroOutMemory(_newFuncAlloc, _newFuncBytesCount);
        Helpers.FreeMemory(_newFuncAlloc);

        return true;
    }
}

public sealed class BoyerMoore
{
    private readonly byte[] _needle;
    private readonly int[] _charTable;
    private readonly int[] _offsetTable;

    public BoyerMoore(byte[] needle)
    {
        _needle = needle;
        _charTable = MakeByteTable(needle);
        _offsetTable = MakeOffsetTable(needle);
    }

    public IEnumerable<int> Search(byte[] haystack)
    {
        if (_needle.Length == 0)
            yield break;

        for (var i = _needle.Length - 1; i < haystack.Length;)
        {
            int j;

            for (j = _needle.Length - 1; _needle[j] == haystack[i]; --i, --j)
            {
                if (j != 0)
                    continue;

                yield return i;
                i += _needle.Length - 1;
                break;
            }

            i += Math.Max(_offsetTable[_needle.Length - 1 - j], _charTable[haystack[i]]);
        }
    }

    private static int[] MakeByteTable(IList<byte> needle)
    {
        const int alphabetSize = 256;
        var table = new int[alphabetSize];

        for (var i = 0; i < table.Length; ++i)
            table[i] = needle.Count;

        for (var i = 0; i < needle.Count - 1; ++i)
            table[needle[i]] = needle.Count - 1 - i;

        return table;
    }

    private static int[] MakeOffsetTable(IList<byte> needle)
    {
        var table = new int[needle.Count];
        var lastPrefixPosition = needle.Count;

        for (var i = needle.Count - 1; i >= 0; --i)
        {
            if (IsPrefix(needle, i + 1))
                lastPrefixPosition = i + 1;

            table[needle.Count - 1 - i] = lastPrefixPosition - i + needle.Count - 1;
        }

        for (var i = 0; i < needle.Count - 1; ++i)
        {
            var suffixLength = SuffixLength(needle, i);
            table[suffixLength] = needle.Count - 1 - i + suffixLength;
        }

        return table;
    }

    private static bool IsPrefix(IList<byte> needle, int p)
    {
        for (int i = p, j = 0; i < needle.Count; ++i, ++j)
            if (needle[i] != needle[j])
                return false;

        return true;
    }

    private static int SuffixLength(IList<byte> needle, int p)
    {
        var len = 0;

        for (int i = p, j = needle.Count - 1; i >= 0 && needle[i] == needle[j]; --i, --j)
            ++len;

        return len;
    }
}