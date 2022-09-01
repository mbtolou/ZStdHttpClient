
using System.Net.Http.Headers;
namespace ZstdHttpClient;

public static class HttpExtensions
{
    private static readonly StringWithQualityHeaderValue ZStdHeader = new StringWithQualityHeaderValue("zstd", 1);

    public static async Task<HttpResponseMessage> GetAsync(
        this HttpClient client,
        string url,
        CompressionType cp
        )
    {
        if (cp == CompressionType.Zstd)
            client.DefaultRequestHeaders.AcceptEncoding.Add(ZStdHeader);

        var response = await client.GetAsync(url);

        if (cp == CompressionType.Zstd)
        {
            if (response.Content.Headers.ContentEncoding.LastOrDefault() == "zstd")

            {
                response.Content = new ZStdHttpContent(response.Content);
            }
        }

        return response;
    }
}
