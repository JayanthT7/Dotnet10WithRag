using Microsoft.AspNetCore.Mvc;
using RagService;

public class RagController : Controller
{

    private readonly ILogger<RagController> _logger;
    private readonly IRagService _ragService;
    private static Dictionary<string, float[]> _embeddingsStore = new();
    public RagController(IRagService ragService,ILogger<RagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if(file!=null)
        {
            var path = Path.Combine("uploads",file.FileName);
            using(var stream = new FileStream(path,FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var text= _ragService.ExtractTextFromPdf(path);

            var chunks = _ragService.ChunkText(text);

            foreach(var chunk in chunks)
            {
                var vector = await _ragService.GetEmbeddingAsync(chunk);
                _embeddingsStore[chunk] = vector;
            }
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AskQuestion(string question)
    {
        var queryVector = await _ragService.GetEmbeddingAsync(question);

        var bestMatch = _embeddingsStore.OrderByDescending(e=>CosineSimilarity(queryVector,e.Value))
                        .First().Key;
        ViewBag.Answer = "$Best match from your PDF: {bestMatch}";
        return View("Index");
    }

    private double CosineSimilarity(float[] v1,float[] v2)
    {
        double dot = 0.0, mag1 = 0.0, mag2 = 0.0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += Math.Pow(v1[i], 2);
            mag2 += Math.Pow(v2[i], 2);
        }
        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }


}