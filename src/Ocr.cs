using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

// Reads the text in a picture with the text recognition of Windows (Windows.Media.Ocr): on this PC, without the
// network, in any Windows 10 or 11. Unlike Packaged, its Windows Runtime types work also outside the package.
static class Ocr
{
    // White around the picture. The text of a screen, small and drawn with ClearType, is read much better with it:
    // of 240 pictures of two lines of screen text, 86 were read exactly as they were, with 400 pixels 144.
    const int Margin = 400;

    // The text of the picture, a line of text per line; empty when there is none. Null when Windows recognizes
    // none of the languages it could be in. Slow on a large picture: not for the thread of the window.
    // A picture is read at its own size, as it was copied: enlarged, the text of a screen is read worse (tried).
    public static string Read(byte[] png)
    {
        OcrEngine engine = Engine();
        if (engine == null) return null;
        using (MemoryStream stream = new MemoryStream(png))
        using (Image picture = Image.FromStream(stream))
        {
            // Only as large as the recognition takes, and on white: a transparent picture is opaque here.
            int side = Math.Max(picture.Width, picture.Height);
            double shrink = Math.Min(1, (double)(OcrEngine.MaxImageDimension - 2 * Margin) / side);
            int width = Math.Max(1, (int)(picture.Width * shrink)), height = Math.Max(1, (int)(picture.Height * shrink));
            using (Bitmap canvas = new Bitmap(width + 2 * Margin, height + 2 * Margin, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(canvas))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(picture, Margin, Margin, width, height);
                }
                StringBuilder text = new StringBuilder();
                foreach (OcrLine line in Packaged.Wait(engine.RecognizeAsync(Pixels(canvas))).Lines)
                {
                    if (text.Length > 0) text.Append("\r\n");
                    text.Append(line.Text);
                }
                return text.ToString();
            }
        }
    }

    // The recognition of the language of the program, which reads its accents (that of English reads "Città" as
    // "Citt?"); without it, the first language of the user that Windows can read.
    static OcrEngine Engine()
    {
        foreach (Language language in OcrEngine.AvailableRecognizerLanguages)
            if (language.LanguageTag.Split('-')[0].Equals(Strings.Code, StringComparison.OrdinalIgnoreCase))
                return OcrEngine.TryCreateFromLanguage(language);
        return OcrEngine.TryCreateFromUserProfileLanguages();
    }

    // The picture as Windows takes it: its pixels, blue-green-red-alpha. A buffer made by CryptographicBuffer,
    // because AsBuffer would need the metadata of the Windows SDK, like AsTask (see Packaged.Wait).
    static SoftwareBitmap Pixels(Bitmap canvas)
    {
        BitmapData data = canvas.LockBits(new Rectangle(Point.Empty, canvas.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        byte[] pixels = new byte[data.Stride * canvas.Height];   // a stride of 32-bit pixels is always the width × 4
        try
        {
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        }
        finally
        {
            canvas.UnlockBits(data);
        }
        return SoftwareBitmap.CreateCopyFromBuffer(CryptographicBuffer.CreateFromByteArray(pixels),
            BitmapPixelFormat.Bgra8, canvas.Width, canvas.Height, BitmapAlphaMode.Premultiplied);
    }
}
