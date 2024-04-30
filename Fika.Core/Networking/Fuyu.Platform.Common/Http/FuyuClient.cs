/*  NOTES
    - Using Aki zlib (de-)compression
*/

using Aki.Common.Utils;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fuyu.Platform.Common.Http
{
    // NOTE: Don't dispose this, keep a reference for the lifetime of the
    //       application.
    public class FuyuClient : IDisposable
    {
        protected HttpClient Httpv;
        protected string Address;
        protected string Cookie;
        protected int Retries;

        public FuyuClient(string address, string sessionId = "", int retries = 3)
        {
            Address = address;
            Cookie = $"PHPSESSID={sessionId}";
            Retries = retries;

            var handler = new HttpClientHandler
            {
                // set cookies in header instead
                UseCookies = false
            };

            Httpv = new HttpClient(handler);
        }

        protected HttpRequestMessage GetNewRequest(HttpMethod method, string path)
        {
            return new HttpRequestMessage()
            {
                Method = method,
                RequestUri = new Uri(Address + path),
                Headers = {
                    { "Cookie", Cookie }
                }
            };
        }

        protected async Task<byte[]> SendAsync(HttpMethod method, string path, byte[] data, bool zipped = true)
        {
            HttpResponseMessage response = null;

            using (var request = GetNewRequest(method, path))
            {
                if (data != null)
                {
                    // add payload to request
                    if (zipped)
                    {
                        data = Zlib.Compress(data, ZlibCompression.Maximum);
                    }

                    request.Content = new ByteArrayContent(data);
                }

                // send request
                response = await Httpv.SendAsync(request);
            }

            if (!response.IsSuccessStatusCode)
            {
                // response error
                throw new Exception($"Code {response.StatusCode}");
            }

            using (var ms = new MemoryStream())
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    // grap response payload
                    await stream.CopyToAsync(ms);
                    var body = ms.ToArray();

                    if (Zlib.IsCompressed(body))
                    {
                        body = Zlib.Decompress(body);
                    }

                    if (body == null)
                    {
                        // payload doesn't contains data
                        var code = response.StatusCode.ToString();
                        body = Encoding.UTF8.GetBytes(code);
                    }

                    return body;
                }
            }
        }

        protected async Task<byte[]> SendWithRetriesAsync(HttpMethod method, string path, byte[] data, bool zipped = true)
        {
            var error = new Exception("Internal error");

            // NOTE: <= is intentional. 0 is send, 1/2/3 is retry
            for (var i = 0; i <= Retries; ++i)
            {
                try
                {
                    return await SendAsync(method, path, data, zipped);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }

            throw error;
        }

        public async Task<byte[]> GetAsync(string path)
        {
            return await SendWithRetriesAsync(HttpMethod.Get, path, null);
        }

        public byte[] Get(string path)
        {
            return Task.Run(() => GetAsync(path)).Result;
        }

        public async Task<byte[]> PostAsync(string path, byte[] data, bool zipped = true)
        {
            return await SendWithRetriesAsync(HttpMethod.Post, path, data, zipped);
        }

        public byte[] Post(string path, byte[] data, bool zipped = true)
        {
            return Task.Run(() => PostAsync(path, data, zipped)).Result;
        }

        // NOTE: returns status code as bytes
        public async Task<byte[]> PutAsync(string path, byte[] data, bool zipped = true)
        {
            return await SendWithRetriesAsync(HttpMethod.Post, path, data, zipped);
        }

        // NOTE: returns status code as bytes
        public byte[] Put(string path, byte[] data, bool zipped = true)
        {
            return Task.Run(() => PutAsync(path, data, zipped)).Result;
        }

        public void Dispose()
        {
            Httpv.Dispose();
        }
    }
}