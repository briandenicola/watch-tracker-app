using SkiaSharp;

namespace WatchTracker.Api.Services;

public static class ImageTools
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    /// <summary>
    /// Decodes the first frame of an image as RGBA with straight (unpremultiplied) alpha.
    /// </summary>
    public static SKBitmap Decode(string path, bool applyExifOrientation = false, int? maxDimension = null)
    {
        using var codec = SKCodec.Create(path)
            ?? throw new InvalidOperationException("Unsupported or corrupt image.");

        var size = codec.Info;
        if (maxDimension is { } max && (size.Width > max || size.Height > max))
            throw new InvalidOperationException(
                $"Image too large ({size.Width}x{size.Height}). Maximum dimension is {max}px.");

        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            bitmap.Dispose();
            throw new InvalidOperationException($"Image could not be decoded ({result}).");
        }

        return applyExifOrientation ? ApplyOrigin(bitmap, codec.EncodedOrigin) : bitmap;
    }

    /// <summary>
    /// Flattens onto white (JPEG has no alpha), shrinks to fit maxEdge when larger, and encodes as JPEG.
    /// Images already within maxEdge are never enlarged.
    /// </summary>
    public static byte[] EncodeJpegOnWhite(SKBitmap source, int maxEdge, int quality)
    {
        var (width, height) = FitWithin(source.Width, source.Height, maxEdge);

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Could not allocate image surface.");
        surface.Canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(source);
        surface.Canvas.DrawImage(image, new SKRect(0, 0, width, height), Sampling);

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, quality)
            ?? throw new InvalidOperationException("JPEG encoding failed.");
        return data.ToArray();
    }

    public static (int Width, int Height) FitWithin(int width, int height, int maxEdge)
    {
        if (width <= maxEdge && height <= maxEdge)
            return (width, height);

        var scale = Math.Min((double)maxEdge / width, (double)maxEdge / height);
        return (Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }

    /// <summary>
    /// Returns a bitmap rotated/flipped so it displays upright for the given EXIF origin.
    /// The source is disposed when a new bitmap is produced.
    /// </summary>
    public static SKBitmap ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return source;

        int w = source.Width, h = source.Height;
        var swapped = origin >= SKEncodedOrigin.LeftTop;
        var target = new SKBitmap(new SKImageInfo(swapped ? h : w, swapped ? w : h, SKColorType.Rgba8888, SKAlphaType.Premul));

        using (var canvas = new SKCanvas(target))
        using (var image = SKImage.FromBitmap(source))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(OriginMatrix(origin, w, h));
            canvas.DrawImage(image, 0, 0);
        }

        source.Dispose();
        return target;
    }

    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, int w, int h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity
    };

    /// <summary>
    /// Draws the source scaled to fit the model square, centered on black, and returns RGBA bytes.
    /// </summary>
    public static byte[] Letterbox(SKBitmap source, int modelSize, int resizedWidth, int resizedHeight)
    {
        using var target = new SKBitmap(new SKImageInfo(modelSize, modelSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(target))
        using (var image = SKImage.FromBitmap(source))
        {
            canvas.Clear(SKColors.Black);
            var padX = (modelSize - resizedWidth) / 2f;
            var padY = (modelSize - resizedHeight) / 2f;
            canvas.DrawImage(image, new SKRect(padX, padY, padX + resizedWidth, padY + resizedHeight), Sampling);
        }

        return target.GetPixelSpan().ToArray();
    }

    /// <summary>
    /// Crops a single-channel mask and scales it to the target size; returns one byte per pixel.
    /// </summary>
    public static byte[] ResizeMask(byte[] mask, int maskWidth, int maskHeight, SKRectI crop, int width, int height)
    {
        using var maskBitmap = new SKBitmap(new SKImageInfo(maskWidth, maskHeight, SKColorType.Rgba8888, SKAlphaType.Opaque));
        var pixels = maskBitmap.GetPixelSpan();
        for (var i = 0; i < mask.Length; i++)
        {
            pixels[i * 4] = pixels[i * 4 + 1] = pixels[i * 4 + 2] = mask[i];
            pixels[i * 4 + 3] = 255;
        }

        using var cropped = new SKBitmap();
        if (!maskBitmap.ExtractSubset(cropped, crop))
            throw new InvalidOperationException("Mask crop is outside the mask.");

        using var scaled = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(scaled))
        using (var image = SKImage.FromBitmap(cropped))
        {
            canvas.DrawImage(image, new SKRect(0, 0, width, height), Sampling);
        }

        var rgba = scaled.GetPixelSpan();
        var result = new byte[width * height];
        for (var i = 0; i < result.Length; i++)
            result[i] = rgba[i * 4];
        return result;
    }
}
