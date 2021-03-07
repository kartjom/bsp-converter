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
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                if (File.Exists(exportPath)) File.Delete(exportPath);
                zip.GetEntry(entryName).ExtractToFile(exportPath);
            }
        }
    }
}
