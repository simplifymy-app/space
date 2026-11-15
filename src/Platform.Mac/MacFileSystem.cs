using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SimplifyMySpace.Core.Files;

namespace SimplifyMySpace.Platform.Mac;

public sealed class MacFileSystem : IFileSystem
{
    public bool TryList(string folder, out IReadOnlyList<DirectoryEntry> entries)
    {
        var listed = BulkListing.Read(folder);
        entries = listed ?? [];
        return listed is not null;
    }

    public void Trash(string path) => Native.Trash(path);

    public void Reveal(string path) => Process.Start("open", ["-R", path])?.Dispose();
}

internal static unsafe class BulkListing
{
    private const uint Name = 0x1;
    private const uint ObjectType = 0x8;
    private const uint ModifiedTime = 0x400;
    private const uint Flags = 0x40000;
    private const uint FileId = 0x2000000;
    private const uint Error = 0x20000000;
    private const uint ReturnedAttributes = 0x80000000;
    private const uint LinkCount = 0x1;
    private const uint TotalSize = 0x2;
    private const uint AllocatedSize = 0x4;
    private const uint Hidden = 0x8000;
    private const uint Directory = 2;
    private const uint SymbolicLink = 5;
    private const int BufferSize = 256 * 1024;

    [ThreadStatic]
    private static byte[]? _buffer;

    public static List<DirectoryEntry>? Read(string folder)
    {
        var descriptor = Native.OpenDirectory(folder);
        if (descriptor < 0)
        {
            return null;
        }

        try
        {
            var device = Native.Fstat(descriptor, out var stat) == 0 ? stat.Device : 0;
            var request = new AttributeList
            {
                BitmapCount = 5,
                Common = ReturnedAttributes | Name | Error | ObjectType | ModifiedTime | Flags | FileId,
                File = LinkCount | TotalSize | AllocatedSize
            };

            var buffer = _buffer ??= new byte[BufferSize];
            var entries = new List<DirectoryEntry>();
            fixed (byte* start = buffer)
            {
                while (true)
                {
                    var count = Native.GetAttributeListBulk(descriptor, &request, start, BufferSize, 0);
                    if (count < 0)
                    {
                        return null;
                    }

                    if (count == 0)
                    {
                        return entries;
                    }

                    var cursor = start;
                    for (var index = 0; index < count; index++)
                    {
                        if (Parse(cursor, device) is { } entry)
                        {
                            entries.Add(entry);
                        }

                        cursor += *(uint*)cursor;
                    }
                }
            }
        }
        finally
        {
            Native.Close(descriptor);
        }
    }

    private static DirectoryEntry? Parse(byte* entry, long device)
    {
        var at = entry + sizeof(uint);
        var common = *(uint*)at;
        var file = *(uint*)(at + 12);
        at += 20;

        if ((common & Error) != 0)
        {
            var failed = *(uint*)at;
            at += 4;
            if (failed != 0)
            {
                return null;
            }
        }

        var name = string.Empty;
        if ((common & Name) != 0)
        {
            var offset = *(int*)at;
            var length = *(uint*)(at + 4);
            name = Encoding.UTF8.GetString(at + offset, (int)Math.Max(0, length - 1));
            at += 8;
        }

        uint type = 0;
        if ((common & ObjectType) != 0)
        {
            type = *(uint*)at;
            at += 4;
        }

        var modified = DateTime.MinValue;
        if ((common & ModifiedTime) != 0)
        {
            modified = DateTime.UnixEpoch.AddSeconds(Unsafe.ReadUnaligned<long>(at)).ToLocalTime();
            at += 16;
        }

        uint flags = 0;
        if ((common & Flags) != 0)
        {
            flags = *(uint*)at;
            at += 4;
        }

        ulong inode = 0;
        if ((common & FileId) != 0)
        {
            inode = Unsafe.ReadUnaligned<ulong>(at);
            at += 8;
        }

        uint links = 1;
        if ((file & LinkCount) != 0)
        {
            links = *(uint*)at;
            at += 4;
        }

        long logical = 0;
        if ((file & TotalSize) != 0)
        {
            logical = Unsafe.ReadUnaligned<long>(at);
            at += 8;
        }

        long onDisk = 0;
        if ((file & AllocatedSize) != 0)
        {
            onDisk = Unsafe.ReadUnaligned<long>(at);
        }

        var kind = type switch
        {
            Directory => EntryKind.Directory,
            SymbolicLink => EntryKind.Link,
            _ => EntryKind.File
        };
        var shared = kind == EntryKind.File && links > 1 ? new FileIdentity(device, inode) : (FileIdentity?)null;
        var hidden = name.StartsWith('.') || (flags & Hidden) != 0;
        return new DirectoryEntry(name, kind, logical, onDisk, modified, hidden, shared);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct AttributeList
{
    public ushort BitmapCount;
    public ushort Reserved;
    public uint Common;
    public uint Volume;
    public uint Directory;
    public uint File;
    public uint Fork;
}

[StructLayout(LayoutKind.Explicit, Size = 144)]
internal struct Stat
{
    [FieldOffset(0)] public int Device;
}

internal static unsafe partial class Native
{
    private const string System = "/usr/lib/libSystem.B.dylib";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const string Foundation = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const int ReadOnlyDirectory = 0x100000 | 0x1000000;

    private static readonly Lazy<nint> FoundationHandle = new(() => NativeLibrary.Load(Foundation));

    public static int OpenDirectory(string path) => Open(path, ReadOnlyDirectory);

    public static void Trash(string path)
    {
        _ = FoundationHandle.Value;
        var pool = objc_autoreleasePoolPush();
        try
        {
            var text = SendText(Class("NSString"), Selector("stringWithUTF8String:"), path);
            var url = Send(Class("NSURL"), Selector("fileURLWithPath:"), text);
            var manager = Send(Class("NSFileManager"), Selector("defaultManager"));
            if (!SendTrash(manager, Selector("trashItemAtURL:resultingItemURL:error:"), url, 0, out var error))
            {
                throw new IOException(Describe(error));
            }
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    private static string Describe(nint error)
    {
        if (error == 0)
        {
            return "The item could not be moved to the Trash";
        }

        var description = Send(error, Selector("localizedDescription"));
        return Marshal.PtrToStringUTF8(Send(description, Selector("UTF8String"))) ?? "Unknown error";
    }

    private static nint Class(string name) => objc_getClass(name);

    private static nint Selector(string name) => sel_registerName(name);

    [LibraryImport(System, EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial int Open(string path, int flags);

    [LibraryImport(System, EntryPoint = "close")]
    public static partial int Close(int descriptor);

    [LibraryImport(System, EntryPoint = "fstat")]
    public static partial int Fstat(int descriptor, out Stat stat);

    [LibraryImport(System, EntryPoint = "getattrlistbulk", SetLastError = true)]
    public static partial int GetAttributeListBulk(int descriptor, AttributeList* request, byte* buffer, nuint size,
        ulong options);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string name);

    [LibraryImport(ObjC, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    [LibraryImport(ObjC)]
    private static partial nint objc_autoreleasePoolPush();

    [LibraryImport(ObjC)]
    private static partial void objc_autoreleasePoolPop(nint pool);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint target, nint selector);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    private static partial nint Send(nint target, nint selector, nint argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SendText(nint target, nint selector, string argument);

    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool SendTrash(nint target, nint selector, nint url, nint resulting, out nint error);
}
