using System.IO;
using System.Runtime.InteropServices;

namespace Drone.Utilities.PELoader;

using static Interop.Data;

public sealed class PeLoader
{
    /// The file header
    public IMAGE_FILE_HEADER ImageFileHeader { get; }

    /// Optional 32 bit file header 
    public IMAGE_OPTIONAL_HEADER32 OptionalHeader32 { get; }

    /// Optional 64 bit file header 
    public IMAGE_OPTIONAL_HEADER64 OptionalHeader64 { get; }

    /// Image Section headers. Number of sections is in the file header.
    public IMAGE_SECTION_HEADER[] ImageSectionHeaders { get; }

    public byte[] RawBytes { get; }

    public PeLoader(byte[] fileBytes)
    {
        // Read in the DLL or EXE and get the timestamp
        using var stream = new MemoryStream(fileBytes, 0, fileBytes.Length);
        using var reader = new BinaryReader(stream);
        
        var dosHeader = FromBinaryReader<IMAGE_DOS_HEADER>(reader);

        // Add 4 bytes to the offset
        stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);

        ImageFileHeader = FromBinaryReader<IMAGE_FILE_HEADER>(reader);
        
        if (Is32BitHeader) OptionalHeader32 = FromBinaryReader<IMAGE_OPTIONAL_HEADER32>(reader);
        else OptionalHeader64 = FromBinaryReader<IMAGE_OPTIONAL_HEADER64>(reader);

        ImageSectionHeaders = new IMAGE_SECTION_HEADER[ImageFileHeader.NumberOfSections];
        
        for (var headerNo = 0; headerNo < ImageSectionHeaders.Length; ++headerNo)
            ImageSectionHeaders[headerNo] = FromBinaryReader<IMAGE_SECTION_HEADER>(reader);

        RawBytes = fileBytes;
    }

    private static T FromBinaryReader<T>(BinaryReader reader)
    {
        // Read in a byte array
        var bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

        // Pin the managed memory while, copy it out the data, then unpin it
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var structure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());

        handle.Free();

        return structure;
    }

    private bool Is32BitHeader
    {
        get
        {
            ushort IMAGE_FILE_32BIT_MACHINE = 0x0100;
            return (IMAGE_FILE_32BIT_MACHINE & ImageFileHeader.Characteristics) == IMAGE_FILE_32BIT_MACHINE;
        }
    }
}