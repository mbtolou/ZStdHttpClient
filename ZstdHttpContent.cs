using ZstdSharp;

namespace ZstdHttpClient;
public sealed class ZStdHttpContent : DecompressedContent
{
    public ZStdHttpContent(HttpContent originalContent) : base(originalContent)
    {
    }

    protected override Stream GetDecompressedStream(Stream originalStream)
    {
        return new DecompressionStream(originalStream);
    }
}