using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Pooling;
public static class WriterPoolManager
{
    static WriterPoolManager()
    {
        _writers.Push(new());
    }

    private static readonly Stack<EFTWriterClass> _writers = new(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnWriter(EFTWriterClass writer)
    {
        _writers.Push(writer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EFTWriterClass GetWriter()
    {
        if (_writers.Count == 0)
        {
            return new();
        }

        EFTWriterClass writer = _writers.Pop();
        writer.Reset();
        return writer;
    }
}
