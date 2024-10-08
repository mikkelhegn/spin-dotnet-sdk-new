using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SpinHttpWorld;
using SpinHttpWorld.wit.imports.wasi.io.v0_2_0;

namespace Spin.Http;

public class InputStream : Stream
{
    IStreams.InputStream stream;
    int offset;
    byte[]? buffer;
    bool closed;

    public InputStream(IStreams.InputStream stream)
    {
        this.stream = stream;
    }

    ~InputStream()
    {
        Dispose(false);
    }

    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override bool CanSeek => false;
    public override long Length => throw new NotImplementedException();
    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        stream.Dispose();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void Flush()
    {
        // ignore
    }

    public override void SetLength(long length)
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int length)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int length)
    {
        throw new NotImplementedException();
    }

    public override async Task<int> ReadAsync(
        byte[] bytes,
        int offset,
        int length,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            if (closed)
            {
                return 0;
            }
            else if (this.buffer == null)
            {
                try
                {
                    // TODO: should we add a special case to the bindings generator
                    // to allow passing a buffer to IStreams.InputStream.Read and
                    // avoid the extra copy?
                    var result = stream.Read(16 * 1024);
                    var buffer = result;
                    if (buffer.Length == 0)
                    {
                        await WasiEventLoop.Register(stream.Subscribe()).ConfigureAwait(false);
                    }
                    else
                    {
                        this.buffer = buffer;
                        this.offset = 0;
                    }
                }
                catch (WitException e)
                {
                    if (((IStreams.StreamError)e.Value).Tag == IStreams.StreamError.CLOSED)
                    {
                        closed = true;
                        return 0;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                var min = Math.Min(this.buffer.Length - this.offset, length);
                Array.Copy(this.buffer, this.offset, bytes, offset, min);
                if (min < buffer.Length - this.offset)
                {
                    this.offset += min;
                }
                else
                {
                    this.buffer = null;
                }
                return min;
            }
        }
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: avoid copy when possible and use ArrayPool when not
        var dst = new byte[buffer.Length];
        var result = await ReadAsync(dst, 0, buffer.Length, cancellationToken);
        new ReadOnlySpan<byte>(dst, 0, result).CopyTo(buffer.Span);
        return result;
    }
}
