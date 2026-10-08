using System.Net.Http.Headers;

namespace OpenUtauDRPC {

    public static class Litterbox {

        private const string LitterboxApi = "https://litterbox.catbox.moe/resources/internals/api.php";

        public static async Task<string> Upload(Stream stream) {
            if (stream.CanSeek) {
                stream.Position = 0;
            }

            using var formContent = new MultipartFormDataContent();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            formContent.Add(streamContent, "fileToUpload", "image.png");

            formContent.Add(new StringContent("fileupload"), "reqtype");
            formContent.Add(new StringContent("72h"), "time");

            HttpResponseMessage response = await Http.Client.PostAsync(LitterboxApi, formContent);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return responseBody;
        }
    }

}