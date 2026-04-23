using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WatchTracker.Api.Services;

public class BackgroundRemovalService : IBackgroundRemovalService, IDisposable
{
    private const int ModelSize = 320;
    private const int MaxInputDimension = 4096;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly ILogger<BackgroundRemovalService> _logger;
    private readonly Lazy<InferenceSession?> _session;
    private readonly string _modelPath;

    public BackgroundRemovalService(ILogger<BackgroundRemovalService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _modelPath = configuration["BackgroundRemoval:ModelPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "models", "u2net.onnx");

        _session = new Lazy<InferenceSession?>(() =>
        {
            try
            {
                if (!File.Exists(_modelPath))
                {
                    logger.LogWarning("U2Net model not found at {Path}", _modelPath);
                    return null;
                }
                var options = new Microsoft.ML.OnnxRuntime.SessionOptions { EnableMemoryPattern = true };
                options.AppendExecutionProvider_CPU();
                var session = new InferenceSession(_modelPath, options);
                logger.LogInformation("U2Net model loaded from {Path}", _modelPath);
                return session;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load U2Net model");
                return null;
            }
        });
    }

    public bool IsAvailable => _session.Value is not null;

    public async Task<string> RemoveBackgroundAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Source image not found.", inputPath);

        var session = _session.Value
            ?? throw new InvalidOperationException("Background removal model is not available.");

        var dir = Path.GetDirectoryName(inputPath)!;
        var outputFileName = $"{Guid.NewGuid()}_nobg.png";
        var outputPath = Path.Combine(dir, outputFileName);

        await Gate.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(() => ProcessImage(session, inputPath, outputPath), cancellationToken);

            if (!File.Exists(outputPath))
                throw new InvalidOperationException("Background removal produced no output.");

            return outputFileName;
        }
        catch
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    private void ProcessImage(InferenceSession session, string inputPath, string outputPath)
    {
        using var image = Image.Load<Rgba32>(inputPath);

        if (image.Width > MaxInputDimension || image.Height > MaxInputDimension)
            throw new InvalidOperationException(
                $"Image too large ({image.Width}x{image.Height}). Maximum dimension is {MaxInputDimension}px.");

        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Letterbox to 320x320 preserving aspect ratio
        var (resizedW, resizedH, padX, padY) = ComputeLetterbox(originalWidth, originalHeight);

        using var letterboxed = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(resizedW, resizedH),
                Mode = ResizeMode.Max
            });
            // Pad to ModelSize x ModelSize
            ctx.Pad(ModelSize, ModelSize, Color.Black);
        });

        // Prepare input tensor: NCHW format, normalized to [0,1] then per-channel normalization
        var input = new DenseTensor<float>([1, 3, ModelSize, ModelSize]);
        float[] mean = [0.485f, 0.456f, 0.406f];
        float[] std = [0.229f, 0.224f, 0.225f];

        letterboxed.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < ModelSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < ModelSize; x++)
                {
                    var pixel = row[x];
                    input[0, 0, y, x] = (pixel.R / 255f - mean[0]) / std[0];
                    input[0, 1, y, x] = (pixel.G / 255f - mean[1]) / std[1];
                    input[0, 2, y, x] = (pixel.B / 255f - mean[2]) / std[2];
                }
            }
        });

        // Run inference
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(session.InputNames[0], input)
        };

        using var results = session.Run(inputs);

        // U2Net outputs multiple maps; first is the finest segmentation mask
        var output = results.First().AsTensor<float>();
        var maskShape = output.Dimensions;
        int maskH = maskShape[2];
        int maskW = maskShape[3];

        // Convert mask to image, unpad, resize to original dimensions
        using var maskImage = new Image<L8>(maskW, maskH);

        // Normalize mask to [0, 255]
        float maskMin = float.MaxValue, maskMax = float.MinValue;
        for (int y = 0; y < maskH; y++)
            for (int x = 0; x < maskW; x++)
            {
                var v = output[0, 0, y, x];
                if (v < maskMin) maskMin = v;
                if (v > maskMax) maskMax = v;
            }
        float range = maskMax - maskMin;
        if (range < 1e-6f) range = 1f;

        maskImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < maskH; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < maskW; x++)
                {
                    var normalized = (output[0, 0, y, x] - maskMin) / range;
                    row[x] = new L8((byte)(Math.Clamp(normalized, 0f, 1f) * 255f));
                }
            }
        });

        // Remove letterbox padding: crop the mask to the resized content area
        int cropX = (maskW - resizedW) / 2;
        int cropY = (maskH - resizedH) / 2;
        maskImage.Mutate(ctx =>
        {
            ctx.Crop(new Rectangle(cropX, cropY, resizedW, resizedH));
            ctx.Resize(originalWidth, originalHeight);
        });

        // Apply mask as alpha channel to original image
        image.ProcessPixelRows(maskImage, (imgAccessor, maskAccessor) =>
        {
            for (int y = 0; y < originalHeight; y++)
            {
                var imgRow = imgAccessor.GetRowSpan(y);
                var maskRow = maskAccessor.GetRowSpan(y);
                for (int x = 0; x < originalWidth; x++)
                {
                    imgRow[x].A = maskRow[x].PackedValue;
                }
            }
        });

        image.SaveAsPng(outputPath);
        _logger.LogInformation("Background removed: {Input} → {Output}", inputPath, outputPath);
    }

    private static (int resizedW, int resizedH, int padX, int padY) ComputeLetterbox(int origW, int origH)
    {
        float scale = Math.Min((float)ModelSize / origW, (float)ModelSize / origH);
        int resizedW = (int)(origW * scale);
        int resizedH = (int)(origH * scale);
        int padX = (ModelSize - resizedW) / 2;
        int padY = (ModelSize - resizedH) / 2;
        return (resizedW, resizedH, padX, padY);
    }

    public void Dispose()
    {
        if (_session.IsValueCreated)
            _session.Value?.Dispose();
    }
}
