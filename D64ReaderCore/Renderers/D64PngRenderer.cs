using D64ReaderCore.Properties;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace D64Reader.Renderers
{
    /// <summary>
    /// Renders a directory as a PNG
    /// </summary>
    public class D64PngRenderer : ID64Renderer<byte[]>
    {
        /// <summary>
        /// Render-Method for a D64Directory-Item
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public virtual byte[] Render(D64Directory directory)
        {
            var directoryContents = DirectoryContents(directory);
            using (var image = new Bitmap(240, 8 * directoryContents.Count()))
            {
                var chars = Resources._base;

                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.FromArgb(80, 69, 155));

                    var current = 0;

                    foreach (var line in directoryContents)
                    {
                        for (var i = 0; i < line.Length; i++)
                        {
                            var source = PetsciiToScreenCode((byte)line[i]);

                            var srcRectangle = new Rectangle(8 * (source % 16), 8 * (source / 16), 8, 8);
                            var dstRectangle = new Rectangle(8 * i, 8 * current, 8, 8);

                            if (current == 0 && i > 1)
                                srcRectangle.Y += 64;

                            Bitmap whatChars = chars;

                            graphics.DrawImage(whatChars, dstRectangle, srcRectangle, GraphicsUnit.Pixel);
                        }
                        current++;
                    }
                }

                using (var stream = new MemoryStream())
                {
                    chars.Dispose();
                    image.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        private byte PetsciiToScreenCode(byte source)
        {
            if (source < 32) source += 128;
            else if (source >= 64 && source < 96) source -= 64;
            else if (source >= 96 && source < 128) source -= 32;
            else if (source >= 128 && source < 160) source += 64;
            else if (source >= 160 && source < 192) source -= 64;
            else if (source >= 192 && source < 255) source -= 128;
            else if (source == 255) source = 94;

            return source;
        }

        private IEnumerable<string> DirectoryContents(D64Directory directory)
        {
            var retVal = new List<string>();
            retVal.Add($"0 \"{directory.DiskName}\" {directory.DiskId}");

            foreach (var item in directory.DirectoryItems)
            {
                var entry = (item.Blocks.ToString().PadRight(5) + $"\"{item.Name}\"").PadRight(24) + $"{item.Type}";
                retVal.Add(entry);
            }

            retVal.Add($"{directory.FreeBlocks} BLOCKS FREE.");

            return retVal;
        }
    }
}