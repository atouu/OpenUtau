
namespace OpenUtauDRPC {
    
    public static class Http {

        public static readonly HttpClient Client = new() {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static async Task<bool> IsValid(string url) {
            try {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await Client.SendAsync(request);

                return response.IsSuccessStatusCode;
            } catch {
                return false;
            }
        }
    }
}