using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Spikylin.Features.Gallery.Dto;
using Spikylin.Features.Gallery.Services.Interfaces;
using System.Globalization;
using System.Text;

namespace Spikylin.Features.Gallery.Services;

public sealed class ImageSharpService : IImageSharpService
{
    private const int ThumbnailSize = 600;

    public async Task<ThumbnailResult> CreateThumbnailAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {

        using var image = await Image.LoadAsync(source, cancellationToken).ConfigureAwait(false);

        var exif = image.Metadata.ExifProfile;

        string? cameraModel = GetExifValue(exif, ExifTag.Model, FormatString);
        string? dateTime = GetExifValue(exif, ExifTag.DateTimeOriginal, FormatDateTime);
        string? focalLength = GetExifValue(exif, ExifTag.FocalLength, FormatFocalLength);
        string? aperture = GetExifValue(exif, ExifTag.FNumber, FormatAperture);
        string? iso = GetExifValue(exif, ExifTag.ISOSpeedRatings, FormatIso);
        string? exposureTime = GetExifValue(exif, ExifTag.ExposureTime, FormatExposureTime);

        image.Mutate(context => {
            context.AutoOrient(); 
            context.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailSize, ThumbnailSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            });
        });
        
        await using var output = new MemoryStream();

        await image.SaveAsWebpAsync(
            output,
            new WebpEncoder { Quality = 82 },
            cancellationToken).ConfigureAwait(false);
       
        return new ThumbnailResult(
            output.ToArray(),
            new PhotoMetadata(
                CameraModel: cameraModel?.Trim(),
                DateTime: dateTime,
                FocalLength: focalLength,
                Aperture: aperture,
                Iso: iso,
                ShutterSpeed: exposureTime),
            "image/webp");
    }

    private static TResult? GetExifValue<TValue, TResult>(
        ExifProfile? profile,
        ExifTag<TValue> tag,
        Func<TValue, TResult?> formatter)
    {
        if (profile?.TryGetValue(tag, out var value) == true && value.Value is not null)
        {
            return formatter(value.Value);
        }

        return default;
    }

    private static string? FormatString(string? value) => ToAsciiOnly(value);

    private static string? FormatDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleanText = ToAsciiOnly(value);

        if (DateTime.TryParseExact(
            cleanText,
            "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime))
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return cleanText;
    }

    private static string? FormatFocalLength(SixLabors.ImageSharp.Rational value)
    {
        if (value.Denominator == 0)
            return null;

        var focalLength = value.Numerator / (double)value.Denominator;
        return $"{focalLength:0.#} mm";
    }

    private static string? FormatAperture(SixLabors.ImageSharp.Rational value)
    {
        if (value.Denominator == 0)
            return null;

        var aperture = value.Numerator / (double)value.Denominator;
        return $"f/{aperture:0.#}";
    }

    private static string? FormatIso(ushort[]? value)
    {
        if (value is null || value.Length == 0)
            return null;

        return $"ISO {value[0]}";
    }

    private static string? FormatExposureTime(SixLabors.ImageSharp.Rational value)
    {
        if (value.Denominator == 0)
            return null;

        var seconds = value.Numerator / (double)value.Denominator;

        if (seconds >= 1)
        {
            return $"{seconds:0.##} s";
        }

        var reciprocal = 1 / seconds;
        return $"1/{Math.Round(reciprocal)} s";
    }

    private static string? ToAsciiOnly(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var builder = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (c <= 127) // Standard ASCII range
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}