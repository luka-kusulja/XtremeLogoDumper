namespace XtremeLogoDumper
{
    internal static class XtremeClient
    {
        public static string AgentHeader = "";

        private static async Task<HttpResponseMessage> Get(Uri url)
        {
            HttpClient httpClient = new HttpClient();
            if (AgentHeader != "")
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", AgentHeader);
            }

            var response = await httpClient.GetAsync(url);

            if(response.IsSuccessStatusCode == false)
            {
                throw new Exception(response.StatusCode.ToString());
            }

            return response;
        }

        public static async Task<string?> GetString(Uri url)
        {
            var response = await Get(url);

            var data = await response.Content.ReadAsStringAsync();

            return data;
        }

        public static async Task<byte[]> GetByte(Uri url)
        {
            var response = await Get(url);

            var data = await response.Content.ReadAsByteArrayAsync();

            return data;
        }
    }
}
