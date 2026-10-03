public interface IRagService
{
    string ExtractTextFromPdf(string filePath);

    List<string> ChunkText(string text, int chunkSize = 200);

    Task<float[]> GetEmbeddingAsync(string input);
}