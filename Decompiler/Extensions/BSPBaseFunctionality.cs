using System.IO;
using System.IO.Compression;

namespace Decompiler
{
    abstract class BaseBSP
    {
        protected byte[] ZipEntryToByteArray(string archivePath, string entryName)
        {
            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                Stream zipStream = zip.GetEntry(entryName).Open();

                MemoryStream ms = new MemoryStream();
                zipStream.CopyTo(ms);

                return ms.ToArray();
            }
        }

        protected void ZipEntryExport(string archivePath, string entryName, string exportPath)
        {
            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                // We have to do it the ugly way
                string originalTextureName = "_invalid_";
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string entryLower = entry.FullName.ToLower();
                    if (entryLower == entryName)
                    {
                        originalTextureName = entry.FullName;
                    }
                }

                if (File.Exists(exportPath)) File.Delete(exportPath);

                ZipArchiveEntry foundTexture = zip.GetEntry(originalTextureName);

                string directory = Path.GetDirectoryName(exportPath);
                Directory.CreateDirectory(directory);

                foundTexture.ExtractToFile(exportPath);
            }
        }
    }
}
