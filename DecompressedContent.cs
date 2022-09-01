
// Taken from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/DecompressionHandler.cs
// The original code is Copyright © .NET Foundation and Contributors. All rights reserved. Licensed under the MIT License (MIT).
using System.Diagnostics;
using System.Net;
namespace ZstdHttpClient;

public abstract class DecompressedContent : HttpContent
{
    private readonly HttpContent _originalContent;
    private bool _contentConsumed;

    public DecompressedContent(HttpContent originalContent)
    {
        _originalContent = originalContent;
        _contentConsumed = false;

        // Copy original response headers, but with the following changes:
        //   Content-Length is removed, since it no longer applies to the decompressed content
        //   The last Content-Encoding is removed, since we are processing that here.
        foreach (var (h, v) in originalContent.Headers)
        {
            Headers.Add(h, v);
        }

        Headers.ContentLength = null;
        Headers.ContentEncoding.Clear();
        string? prevEncoding = null;
        foreach (string encoding in originalContent.Headers.ContentEncoding)
        {
            if (prevEncoding != null)
            {
                Headers.ContentEncoding.Add(prevEncoding);
            }

            prevEncoding = encoding;
        }
    }

    protected abstract Stream GetDecompressedStream(Stream originalStream);

    protected override void SerializeToStream(Stream stream, TransportContext? context,
        CancellationToken cancellationToken)
    {
        using Stream decompressedStream = CreateContentReadStream(cancellationToken);
        decompressedStream.CopyTo(stream);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context,
        CancellationToken cancellationToken)
    {
        using Stream decompressedStream = await CreateContentReadStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        await decompressedStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
    {
        ValueTask<Stream> task = CreateContentReadStreamAsyncCore(async: false, cancellationToken);
        Debug.Assert(task.IsCompleted);
        return task.GetAwaiter().GetResult();
    }

    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
        CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask();

    private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async,
        CancellationToken cancellationToken)
    {
        if (_contentConsumed)
        {
            throw new InvalidOperationException("Stream already read");
        }

        _contentConsumed = true;

        Stream originalStream;
        if (async)
        {
            originalStream = await _originalContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            originalStream = _originalContent.ReadAsStream(cancellationToken);
        }

        return GetDecompressedStream(originalStream);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _originalContent.Dispose();
        }

        base.Dispose(disposing);
    }
}
