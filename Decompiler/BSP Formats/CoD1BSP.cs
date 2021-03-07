using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using PrimitiveTypeExtensions;

namespace Decompiler
{
    partial class CoD1BSP : CoDExtension, IBSP
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileDirectory { get; set; }
        public string OutputFolder { get; set; }
        public bool GameDirectoryValid { get; set; }

        CoDMaterial[] Textures { get; set; }
        TriangleSoups[] Faces { get; set; }
        CoD1Vertex[] Vertices { get; set; }
        MeshVerts[] MeshVerts { get; set; }

        List<string> GameArchives = new List<string>();

        Dictionary<string, string> TexturesDictionary = new Dictionary<string, string>();
        Dictionary<string, string> TexturesExtensions = new Dictionary<string, string>();
        Dictionary<string, string> Shaders = new Dictionary<string, string>();

        public CoD1BSP(string path)
        {
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
            FileExt = Path.GetExtension(path);
            FileDirectory = Path.GetDirectoryName(path);
            OutputFolder = Path.Combine(FileDirectory, FileName);

            string cod1Dir = AppConfig.Settings().CoD1RootDirectory;
            if (Directory.Exists( Path.Combine(cod1Dir, "main") ))
            {
                GameDirectoryValid = true;
                GetGameArchives();
                FindAndParseShaderFiles();

                Console.WriteLine($"INFO: Game directory found: {cod1Dir}");
            }
            else
            {
                GameDirectoryValid = false;
                Console.WriteLine("WARNING: CoD 1 directory not found. No textures will be exported (see config.json)");
            }
        }

        public void PrintData() {}

        public void LoadData()
        {
            using (BinaryReader stream = new BinaryReader(File.Open(FilePath, FileMode.Open)))
            {
                Lumps = ReadLumpList(stream);

                Textures = ReadLump<CoDMaterial>(stream, 0);
                Faces = ReadLump<TriangleSoups>(stream, 6);
                Vertices = ReadLump<CoD1Vertex>(stream, 7);
                MeshVerts = ReadLump<MeshVerts>(stream, 8);
            }

            if (GameDirectoryValid)
            {
                FixTextureNaming();
                GetTexturesPathsAndExts();
            }
        }

        private void GetGameArchives()
        {
            string cod1Dir = AppConfig.Settings().CoD1RootDirectory;

            string[] codMain = Directory.GetFiles($@"{cod1Dir}\Main", "*.pk3");
            GameArchives.AddRange(codMain);

            if (Directory.Exists($@"{cod1Dir}\uo"))
            {
                string[] coduoMain = Directory.GetFiles($@"{cod1Dir}\uo", "*.pk3");
                GameArchives.AddRange(coduoMain);
            }
        }

        private void FixTextureNaming()
        {
            /* Some textures have slash instead of backslash */
            for (int i = 0; i < Textures.Length; i++)
            {
                string texName = Textures[i].Name.String().Replace(@"\", @"/");
                if (Shaders.ContainsKey(texName))
                {
                    texName = Shaders[texName];
                }

                Textures[i].Name = texName.ToCharArray();
            }
        }

        void FindAndParseShaderFiles()
        {
            foreach (string archive in GameArchives)
            {
                using (ZipArchive zip = ZipFile.OpenRead(archive))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (entry.FullName.StartsWith("scripts") && entry.FullName.EndsWith(".shader"))
                        {
                            using (StreamReader reader = new StreamReader(entry.Open()))
                            {
                                string fileContent = reader.ReadToEnd();
                                ParseShader(fileContent);
                            }
                        }
                    }
                }
            }
        }

        void ParseShader(string content)
        {
            string[] contentList = content.Replace("\t", "").Replace(@"\", @"/").Split("\n");

            for (int i = 0; i < contentList.Length; i++)
            {
                if (contentList[i].Trim().StartsWith("textures"))
                {
                    string shaderName = contentList[i].Trim();
                    string textureName = null;

                    int j = i + 1;
                    while (j < contentList.Length)
                    {
                        if (contentList[j].Trim().StartsWith("textures")) break;

                        if (contentList[j].Trim().StartsWith("map "))
                        {
                            textureName = contentList[j].Split(" ")[^1].Split('.')[0];
                            break;
                        }

                        j++;
                    }

                    if (textureName != null)
                    {
                        Shaders[shaderName] = textureName.Trim();
                    }
                }
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
                foreach (CoDMaterial tex in Textures)
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

                foreach (TriangleSoups f in Faces)
                {
                    string matName = Textures[f.MaterialID].Name.String().Trim('\0');

                    // o - separate objects, g - grouped into one
                    fs.WriteLine($"{objectGroupChar} {FileName}_{obj_index++}");
                    fs.WriteLine($"usemtl {matName}");

                    for (int i = f.TriangleOffset; i < f.TriangleOffset + f.TriangleLength; i += 3) // Vertex indicies offset (not vertex)
                    {
                        int i1 = MeshVerts[i].Offset + f.VertexOffset + 1;
                        int i2 = MeshVerts[i + 1].Offset + f.VertexOffset + 1;
                        int i3 = MeshVerts[i + 2].Offset + f.VertexOffset + 1;

                        fs.WriteLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
                    }
                }
            }
        }
    }
}
