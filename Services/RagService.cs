using Azure;
using Azure.AI.Inference;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UglyToad.PdfPig;

namespace RagService
{
    public class RagService : IRagService
    {
        private readonly EmbeddingsClient _embeddingsClient;

        public RagService(string endpoint, string apiKey)
        {
            _embeddingsClient = new EmbeddingsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public string ExtractTextFromPdf(string filePath)
        {
            var text = new StringBuilder();
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    text.AppendLine(page.Text);
                }
            }
            return text.ToString();
        }

        public List<string> ChunkText(string text, int chunkSize = 200)
        {
            var words = text.Split(' ');
            var chunks = new List<string>();
            for (int i = 0; i < words.Length; i += chunkSize)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
                chunks.Add(chunk);
            }
            return chunks;
        }

        public async Task<float[]> GetEmbeddingAsync(string input)
        {
            var options = new EmbeddingsOptions(new[] { input })
            {
                Model = "text-embedding-3-small" // or your deployed model
            };

            var response = await _embeddingsClient.EmbedAsync(options);
            var bytes = response.Value.Data[0].Embedding.ToArray();
            if (bytes == null || bytes.Length % 4 != 0)
                throw new InvalidOperationException("Unexpected embedding byte array length.");

            var floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }
    }
}