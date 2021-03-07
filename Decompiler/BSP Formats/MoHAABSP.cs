using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using PrimitiveTypeExtensions;

namespace Decompiler
{
    partial class MoHAABSP : MoHAAExtension, IBSP
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileDirectory { get; set; }
        public string OutputFolder { get; set; }
        public bool GameDirectoryValid { get; set; }

        MoHAAMaterial[] Textures { get; set; }
        MoHAASurface[] Faces { get; set; }
        MoHAAVertex[] Vertices { get; set; }
        MeshVerts[] MeshVerts { get; set; }

        List<string> GameArchives = new List<string>();

        Dictionary<string, string> TexturesDictionary = new Dictionary<string, string>();
        Dictionary<string, string> TexturesExtensions = new Dictionary<string, string>();

        enum SurfaceType
        {
            MST_BAD,
            MST_PLANAR,
            MST_PATCH,
            MST_TRIANGLE_SOUP,
            MST_FLARE
        };

        public MoHAABSP(string path)
        {
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
            FileExt = Path.GetExtension(path);
            FileDirectory = Path.GetDirectoryName(path);
            OutputFolder = Path.Combine(FileDirectory, FileName);

            string mohaaDir = AppConfig.Settings().MoHAARootDirectory;
            if (Directory.Exists( Path.Combine(mohaaDir, "main") ))
            {
                GameDirectoryValid = true;
                GetGameArchives();

                Console.WriteLine($"INFO: Game directory found: {mohaaDir}");
            }
            else
            {
                GameDirectoryValid = false;
                Console.WriteLine("WARNING: MoHAA directory not found. No textures will be exported (see config.json)");
            }
        }

        public void PrintData() {}

        public void LoadData()
        {
            using (BinaryReader stream = new BinaryReader(File.Open(FilePath, FileMode.Open)))
            {
                Lumps = ReadLumpList(stream);

                Textures = ReadLump<MoHAAMaterial>(stream, 0);
                Faces = ReadLump<MoHAASurface>(stream, 3);
                Vertices = ReadLump<MoHAAVertex>(stream, 4);
                MeshVerts = ReadLump<MeshVerts>(stream, 5);
            }

            if (GameDirectoryValid)
            {
                GetTexturesPathsAndExts();
            }
        }

        private void GetGameArchives()
        {
            string mohaaDir = AppConfig.Settings().MoHAARootDirectory;

            string[] codMain = Directory.GetFiles($@"{mohaaDir}\main", "*.pk3");
            GameArchives.AddRange(codMain);

            if (Directory.Exists($@"{mohaaDir}\mainta"))
            {
                string[] shMain = Directory.GetFiles($@"{mohaaDir}\mainta", "*.pk3");
                GameArchives.AddRange(shMain);
            }
            
            if (Directory.Exists($@"{mohaaDir}\maintt"))
            {
                string[] btMain = Directory.GetFiles($@"{mohaaDir}\maintt", "*.pk3");
                GameArchives.AddRange(btMain);
            }
        }

        private void GetTexturesPathsAndExts()
        {
            /* Texture Path => Path to it's PK3 file */
            foreach (string file in GameArchives)
            {
                using (ZipArchive zip = ZipFile.OpenRead(file))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (!entry.FullName.StartsWith(@"textures/")) continue;
                        if (entry.FullName.EndsWith(@"/")) continue;
                        TexturesDictionary[entry.FullName] = file;
                    }
                }
            }

            /* Texture Path (without extension) => Texture Extension */
            foreach (var (fileName, _) in TexturesDictionary)
            {
                string filePath = Path.GetDirectoryName(fileName).Replace("\\", "/") + "/";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);

                TexturesExtensions[filePath + fileNameWithoutExtension] = fileExtension;
            }
        }

        public void ExtractTextures()
        {
            /* Export textures from the game files one by one */
            int matIndex = 1;
            foreach (var texture in Textures)
            {
                try
                {
                    string texName = texture.Name.String();

                    string filePathWithExtension = $"{texName}{TexturesExtensions[texName]}";
                    string pk3File = TexturesDictionary[filePathWithExtension];

                    string exportPath = $"{OutputFolder}/{filePathWithExtension}";
                    ZipEntryExport(pk3File, filePathWithExtension, exportPath);

                    Console.WriteLine($"({matIndex++}/{Textures.Length}) {filePathWithExtension}");
                }
                catch (Exception)
                {
                    Console.WriteLine($"({matIndex++}/{Textures.Length}) ERROR: Couldn't find {texture.Name.String()}");
                }
            }
        }

        public void CreateMaterials()
        {
            using (StreamWriter fs = new StreamWriter($@"{OutputFolder}/{FileName}.mtl"))
            {
                foreach (MoHAAMaterial tex in Textures)
                {
                    /* Example tex.Name:     textures/common/clip_nosight_metal (no extension)*/
                    string texName = tex.Name.String();
                    string filePathWithExtension;

                    try
                    {
                        /* Find texture's extension */
                        filePathWithExtension = $"{texName}{TexturesExtensions[texName]}";
                    }
                    catch (Exception)
                    {
                        /* Use tex.Name instead if extension can't be found */
                        filePathWithExtension = texName;
                    }

                    fs.WriteLine($"newmtl {texName}"); /* Name of the material, doesn't really matter how we name it */
                    fs.WriteLine("Ns 225");
                    fs.WriteLine("Ka 1 1 1");
                    fs.WriteLine("Kd 0.8 0.8 0.8");
                    fs.WriteLine("Ks 0.5 0.5 0.5");
                    fs.WriteLine("Ke 0 0 0");
                    fs.WriteLine("Ni 1.45");
                    fs.WriteLine("d 1");
                    fs.WriteLine("illum 2");
                    fs.WriteLine(@$"map_Kd {filePathWithExtension}"); /* Texture's path */
                }
            }
        }

        public void WriteFile()
        {
            /* map.bsp => ../map/map.obj */
            Directory.CreateDirectory(OutputFolder);

            int obj_index = 0;
            using (StreamWriter fs = new StreamWriter($@"{OutputFolder}/{FileName}.obj"))
            {
                /* Include material file */
                fs.WriteLine($"mtllib {FileName}.mtl");

                /* Exporting vertices */
                for (int i = 0; i < Vertices.Length; i++)
                {
                    float x = Vertices[i].Position[0];
                    float y = Vertices[i].Position[1];
                    float z = Vertices[i].Position[2];

                    float u = Vertices[i].UV[0];
                    float v = Vertices[i].UV[1];

                    float nx = Vertices[i].Normal[0];
                    float ny = Vertices[i].Normal[1];
                    float nz = Vertices[i].Normal[2];

                    fs.WriteLine($"v {x} {y} {z}");
                    fs.WriteLine($"vt {u} {-v}");
                    fs.WriteLine($"vn {nx} {ny} {nz}");
                }

                /* Exporting geometry */
                char objectGroupChar = AppConfig.Settings().ShouldSplitObjects ? 'o' : 'g';

                foreach (MoHAASurface f in Faces)
                {
                    string matName = Textures[f.MaterialID].Name.String().Trim('\0');
                    int surfType = f.surfaceType;

                    // o - separate objects, g - grouped into one
                    fs.WriteLine($"{objectGroupChar} {FileName}_{obj_index++}");
                    fs.WriteLine($"usemtl {matName}");

                   // TODO
                }
            }
        }
    }
}
