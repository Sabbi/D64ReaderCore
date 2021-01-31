using D64Reader.Renderers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D64Reader
{
    public class D64ReaderCore
    {
        private readonly byte[] imageData;

        private const int loc180 = 91396;
        private const int loc181 = 91648;

        /// <summary>
        /// all the data of the Image (incl. Type, Directory, Name, Id, Blocks)
        /// </summary>
        public D64Directory Directory { get; private set; }

        /// <summary>
        /// Class to read D64-Images (35 Tracks, 35 Tracks extended and 40 Tracks)
        /// </summary>
        /// <param name="imageData">D64-image as a byte-array</param>
        public D64ReaderCore(byte[] imageData)
        {
            this.imageData = imageData;
            Parse();
        }

        /// <summary>
        /// Class to read D64-Images (35 Tracks, 35 Tracks extended and 40 Tracks)
        /// </summary>
        /// <param name="imageData">D64-image as a stream</param>
        public D64ReaderCore(Stream stream)
        {
            imageData = new byte[stream.Length];
            stream.Read(imageData);
            Parse();
        }

        /// <summary>
        /// Renders out the full directory with a D64Renderer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="renderer">Instance of a ID64Renderer</param>
        /// <returns></returns>
        public T Render<T>(ID64Renderer<T> renderer)
        {
            return renderer.Render(Directory);
        }

        private void Parse()
        {
            if (DiskType == "unknown")
            {
                throw new ArgumentException($"ImageData has an invalid size of {imageData.Length}");
            }

            Directory = new D64Directory()
            {
                DiskType = DiskType,
                DirectoryItems = DirectoryItems,
                DiskId = DiskId,
                DiskName = DiskName,
                FreeBlocks = FreeBlocks,
            };
        }

        /// <summary>
        /// Returns the diskname incl. spaces
        /// </summary>
        private string DiskName
        {
            get
            {
                var sb = new StringBuilder();

                for (var i = loc181 - 0x70; i <= (loc181 - 0x61); i++)
                {
                    sb.Append((char)imageData[i]);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the Id of the images, incl. spaces
        /// </summary>
        private string DiskId
        {
            get
            {
                var sb = new StringBuilder();

                for (var i = loc181 - 0x5E; i <= loc181 - 0x5A; i++)
                {
                    sb.Append((char)imageData[i]);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the Free Blocks
        /// </summary>
        private int FreeBlocks
        {
            get
            {
                var loc = loc180;
                var freeBlocks = 0;

                for (var i = 1; i <= 35; i++)
                {
                    if (i != 18) freeBlocks += imageData[loc];
                    loc += 4;
                }
                return freeBlocks;
            }
        }

        /// <summary>
        /// Returns all the directory-items of the image
        /// </summary>
        private List<DirectoryItem> DirectoryItems
        {
            get
            {
                var dirItems = new List<DirectoryItem>();
                var startOffset = loc181;
                var curDirSector = 1;

                // Iterate thru all sectors
                for (var t = 1; t <= 18; t++)
                {
                    var sectorOffset = 0;

                    // Get next track and sector of the directory
                    var nextDirTrack = imageData[startOffset];
                    var nextDirSector = imageData[startOffset + 1];

                    // loop through each directory sector 8 times
                    for (var s = 0; s < 8; s++)
                    {
                        var dirItem = new DirectoryItem { Name = string.Empty };

                        // parse the filename
                        for (var i = (startOffset + sectorOffset + 0x05); i <= (startOffset + sectorOffset + 0x14); i++)
                        {
                            if (imageData[i] != 160)
                                dirItem.Name += (char)imageData[i];
                        }

                        // get the starting track and sector
                        dirItem.FileStartingTrack = imageData[startOffset + sectorOffset + 0x03];
                        dirItem.FileStartingSector = imageData[startOffset + sectorOffset + 0x04];

                        // get the file size (blocks)
                        var sfilesize1 = (int)imageData[startOffset + sectorOffset + 0x1E];
                        var sfilesize2 = (int)imageData[startOffset + sectorOffset + 0x1F];
                        dirItem.Blocks = sfilesize1 + sfilesize2 * 256;

                        // get the filetype
                        var sfiletype = imageData[startOffset + sectorOffset + 0x02];
                        switch (ParseTypeBits(sfiletype))
                        {
                            case "100":
                                dirItem.Type = "REL";
                                break;

                            case "011":
                                dirItem.Type = "USR";
                                break;

                            case "010":
                                dirItem.Type = "PRG";
                                break;

                            case "001":
                                dirItem.Type = "SEQ";
                                break;

                            case "000":
                                dirItem.Type = "DEL";
                                break;

                            default:
                                dirItem.Type = "???";
                                break;
                        }

                        // Is the file open?
                        if (!IsBitSet(sfiletype, 7))
                        {
                            dirItem.Type += "*";
                            dirItem.IsOpen = true;
                        }

                        // Helps against DirProtects
                        if (dirItem.FileStartingTrack == 0 && dirItem.FileStartingSector == 0 && sfilesize1 == 0 && dirItem.Name == "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0")
                        {
                            return dirItems;
                        }
                        dirItems.Add(dirItem);
                        sectorOffset += 0x20;
                    }

                    // last sector?
                    if (nextDirSector == 0 || nextDirTrack == 0)
                    {
                        break;
                    }
                    startOffset = startOffset + (256 * (nextDirSector - curDirSector));
                    curDirSector = nextDirSector;
                }
                return dirItems;
            }
        }

        /// <summary>
        /// Returns the disktype
        /// </summary>
        private string DiskType
        {
            get
            {
                switch (imageData.Length)
                {
                    case 174848:
                        return "dt35";

                    case 196608:
                        return "dt40";

                    case 175531:
                        return "dt35e";

                    case 197376:
                        return "dt40e";

                    default:
                        return "unknown";
                }
            }
        }

        /// <summary>
        /// Parses the TypeBit into fileType-string
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private string ParseTypeBits(byte b)
        {
            var b2 = IsBitSet(b, 2) ? "1" : "0";
            var b1 = IsBitSet(b, 1) ? "1" : "0";
            var b0 = IsBitSet(b, 0) ? "1" : "0";

            return b2 + b1 + b0;
        }

        private bool IsBitSet(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
    }
}