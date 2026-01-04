using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace XiloAdventures.Engine;

/// <summary>
/// Convierte imágenes a arte ASCII para el player de Linux.
/// [RESERVED FOR FUTURE USE - DO NOT DELETE]
/// This class is currently unused but preserved for future ASCII image support.
/// </summary>
public static class AsciiConverter
{
    // Rampa de caracteres ASCII simplificada para mejor contraste visual
    private const string AsciiRamp = "@#*+=-:. ";

    /// <summary>
    /// Convierte una imagen en Base64 a arte ASCII.
    /// </summary>
    /// <param name="base64Image">Imagen en formato Base64 (PNG/JPG).</param>
    /// <param name="width">Ancho en caracteres del resultado (por defecto 160).</param>
    /// <returns>Cadena con el arte ASCII.</returns>
    public static string ConvertFromBase64(string base64Image, int width = 160)
    {
        var bytes = Convert.FromBase64String(base64Image);
        using var stream = new MemoryStream(bytes);
        return ConvertFromStream(stream, width);
    }

    /// <summary>
    /// Convierte una imagen desde un stream a arte ASCII.
    /// </summary>
    /// <param name="imageStream">Stream con la imagen.</param>
    /// <param name="width">Ancho en caracteres del resultado.</param>
    /// <returns>Cadena con el arte ASCII.</returns>
    public static string ConvertFromStream(Stream imageStream, int width = 160)
    {
        using var image = Image.Load<Rgba32>(imageStream);
        return ConvertImage(image, width);
    }

    /// <summary>
    /// Convierte una imagen desde un archivo a arte ASCII.
    /// </summary>
    /// <param name="imagePath">Ruta al archivo de imagen.</param>
    /// <param name="width">Ancho en caracteres del resultado.</param>
    /// <returns>Cadena con el arte ASCII.</returns>
    public static string ConvertFromFile(string imagePath, int width = 160)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        return ConvertImage(image, width);
    }

    /// <summary>
    /// Convierte una imagen cargada a arte ASCII.
    /// </summary>
    private static string ConvertImage(Image<Rgba32> image, int width)
    {
        // Calcular altura proporcional (los caracteres son ~2x más altos que anchos)
        int height = (int)(image.Height / (double)image.Width * width * 0.5);

        // Redimensionar, aplicar blur para suavizar y convertir a escala de grises
        image.Mutate(x => x
            .Resize(width, height)
            .GaussianBlur(1.2f)  // Suavizar para reducir ruido en ASCII
            .Contrast(0.9f)      // Contraste moderado para mejor conversión
            .Grayscale());

        var sb = new StringBuilder();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];

                // Calcular brillo usando fórmula de luminancia perceptual
                float brightness = (0.2126f * pixel.R + 0.7152f * pixel.G + 0.0722f * pixel.B) / 255f;

                // Mapear brillo a índice en la rampa (invertido: oscuro=denso, claro=espacios)
                int index = (int)((AsciiRamp.Length - 1) * (1 - brightness));
                index = Math.Clamp(index, 0, AsciiRamp.Length - 1);

                sb.Append(AsciiRamp[index]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
