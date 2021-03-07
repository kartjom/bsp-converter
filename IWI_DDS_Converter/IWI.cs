using System.IO;
using BinaryFileReader;

namespace IWI_DDS_Converter
{
    public static class IWI
    {
        public static void ConvertToDDS(string filePath, string fullExportPath)
        {
            using (BinaryReader stream = new BinaryReader( File.Open(filePath, FileMode.Open) ))
            {
                StartConversion(stream, fullExportPath);
            }
        }

        public static void ConvertToDDSFromByteStream(byte[] fileData, string fullExportPath)
        {
            using (MemoryStream ms = new MemoryStream(fileData))
            {
                using (BinaryReader stream = new BinaryReader(ms))
                {
                    StartConversion(stream, fullExportPath);
                }
            }
        }

        private static void StartConversion(BinaryReader stream, string fullExportPath)
        {
            IWIHeader header;
            byte[] mipMap2, mipMap1, texture;

            header = BinLib.ReadBinary<IWIHeader>(stream);

            int lowestMipMapSize = (int)header.MipMap1Offset - (int)header.MipMap2Offset;

            BinLib.SetStreamOffset(stream, header.MipMap2Offset);
            mipMap2 = stream.ReadBytes(lowestMipMapSize);

            BinLib.SetStreamOffset(stream, header.MipMap1Offset);
            mipMap1 = stream.ReadBytes(lowestMipMapSize * 4);

            BinLib.SetStreamOffset(stream, header.TextureOffset);
            texture = stream.ReadBytes(lowestMipMapSize * 16);

            Directory.CreateDirectory(Path.GetDirectoryName(fullExportPath));
            using (BinaryWriter fs = new BinaryWriter(File.Open(fullExportPath, FileMode.OpenOrCreate)))
            {
                fs.Write(new char[] { 'D', 'D', 'S', ' ' }); // magic number
                fs.Write(124); // dwSize
                fs.Write(0x1 | 0x2 | 0x4 | 0x1000 | 0x80000); //dwFlags
                fs.Write((uint)header.Height); //dwHeight
                fs.Write((uint)header.Width); //dwWidth
                fs.Write(header.FileSize - header.TextureOffset); //dwPitchOrLinearSize
                fs.Write(0); // dwDepth
                fs.Write(0); // dwMipMapCount
                fs.Write(new byte[44]); // dwReserved1[11]

                // DDS_PIXELFORMAT
                fs.Write(32); // dwSize
                fs.Write(0x4); // dwFlags
                fs.Write(new char[] { 'D', 'X', 'T', (header.DXT == 11 ? '1' : '5') }); // dwFourCC
                fs.Write(0); // dwRGBBitCount
                fs.Write(0); // dwRBitMask
                fs.Write(0); // dwGBitMask
                fs.Write(0); // dwBBitMask
                fs.Write(0); // dwABitMask
                //

                fs.Write(0x1000); // dwCaps
                fs.Write(0); // dwCaps2
                fs.Write(0); // dwCaps3
                fs.Write(0); // dwCaps4
                fs.Write(0); // dwReserved2

                /* Actual image data */
                fs.Write(texture);
                fs.Write(mipMap1);
                fs.Write(mipMap2);
            }
        }

        public static string ExtractTextureNameFromByteStream(byte[] fileData)
        {
            using (MemoryStream stream = new MemoryStream(fileData))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Seek(4, 0);
                    uint texOffset = reader.ReadUInt32();

                    reader.BaseStream.Seek(68, 0);
                    uint texLength = reader.ReadUInt32() - texOffset;

                    reader.BaseStream.Seek(texOffset, 0);
                    string texName = System.Text.Encoding.UTF8.GetString(reader.ReadBytes((int)texLength));

                    return texName;
                }
            }
        }
    }
}
