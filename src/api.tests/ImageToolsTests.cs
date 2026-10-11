using SkiaSharp;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class ImageToolsTests
{
    [Theory]
    [InlineData(1800, 900, 1024, 1024, 512)]
    [InlineData(900, 1800, 1024, 512, 1024)]
    [InlineData(800, 600, 1024, 800, 600)]
    public void FitWithin_shrinks_but_never_enlarges(int w, int h, int max, int expectedW, int expectedH) =>
        Assert.Equal((expectedW, expectedH), ImageTools.FitWithin(w, h, max));

    [Fact]
    public void Transparent_pixels_are_flattened_onto_white_when_encoding_jpeg()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(8, 8, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);

        using var decoded = SKBitmap.Decode(ImageTools.EncodeJpegOnWhite(bitmap, 1024, 90));

        var pixel = decoded.GetPixel(4, 4);
        Assert.True(pixel.Red > 240 && pixel.Green > 240 && pixel.Blue > 240);
    }

    [Theory]
    [InlineData(SKEncodedOrigin.TopRight, 2, 1, 1, 0)]
    [InlineData(SKEncodedOrigin.BottomRight, 2, 1, 1, 0)]
    [InlineData(SKEncodedOrigin.RightTop, 1, 2, 0, 0)]
    [InlineData(SKEncodedOrigin.LeftBottom, 1, 2, 0, 1)]
    public void ApplyOrigin_moves_the_marker_pixel_like_exif_says(
        SKEncodedOrigin origin, int expectedW, int expectedH, int expectedX, int expectedY)
    {
        // 2x1 image: red at (0,0), blue at (1,0)
        var source = new SKBitmap(new SKImageInfo(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul));
        source.SetPixel(0, 0, SKColors.Red);
        source.SetPixel(1, 0, SKColors.Blue);

        using var result = ImageTools.ApplyOrigin(source, origin);

        Assert.Equal(expectedW, result.Width);
        Assert.Equal(expectedH, result.Height);
        Assert.Equal(SKColors.Red, result.GetPixel(expectedX, expectedY));
    }

    [Fact]
    public void ResizeMask_crops_and_scales_single_channel_values()
    {
        // 4x2 mask: left half 0, right half 255; crop the right half and scale up
        byte[] mask = [0, 0, 255, 255, 0, 0, 255, 255];

        var resized = ImageTools.ResizeMask(mask, 4, 2, SKRectI.Create(2, 0, 2, 2), 6, 6);

        Assert.Equal(36, resized.Length);
        Assert.All(resized, v => Assert.True(v > 250));
    }
}
