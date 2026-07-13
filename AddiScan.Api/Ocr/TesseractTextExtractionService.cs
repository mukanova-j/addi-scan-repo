using Tesseract;

namespace AddiScan.Api.Ocr;

/// <summary>
/// Extracts ingredient text from label images using Tesseract's LSTM OCR engine. Registered as
/// a singleton because constructing a TesseractEngine loads the language model from disk, which
/// is expensive to repeat per request; a semaphore serializes access since the engine itself is
/// not safe for concurrent use. OCR stays synchronous request/response, matching the app's
/// current scope.
/// </summary>
public sealed class TesseractTextExtractionService : ITextExtractionService, IDisposable
{
    private readonly ILogger<TesseractTextExtractionService> _logger;
    private readonly TesseractEngine _engine;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TesseractTextExtractionService(IConfiguration configuration, IHostEnvironment environment, ILogger<TesseractTextExtractionService> logger)
    {
        _logger = logger;

        var tessDataPath = configuration["Ocr:TessDataPath"]
            ?? Path.Combine(environment.ContentRootPath, "tessdata");

        _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
    }

    public async Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await imageStream.CopyToAsync(buffer, cancellationToken);
        var imageBytes = buffer.ToArray();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var image = Pix.LoadFromMemory(imageBytes);
            using var page = _engine.Process(image);
            var text = page.GetText().Trim();

            return string.IsNullOrWhiteSpace(text)
                ? OcrResult.Failed("No readable text was found in the image.")
                : OcrResult.Ok(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR text extraction failed.");
            return OcrResult.Failed("Text extraction failed. Try a clearer photo of the label.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
        _engine.Dispose();
    }
}
