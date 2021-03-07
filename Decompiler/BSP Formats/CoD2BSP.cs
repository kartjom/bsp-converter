using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using PrimitiveTypeExtensions;
using IWI_DDS_Converter;

namespace Decompiler
{
    class CoD2BSP : CoDExtension, IBSP
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileDirectory { get; set; }
        public string OutputFolder { get; set; }
        public bool GameDirectoryValid { get; set; }

        CoDMaterial[] Textures { get; set; }
        TriangleSoups[] Faces { get; set; }
        CoD2Vertex[] Vertices { get; set; }
        MeshVerts[] MeshVerts { get; set; }

        Dictionary<string, string> MaterialsDictionary = new Dictionary<string, string>();
        Dictionary<string, string> TexturesDictionary = new Dictionary<string, string>();

        public CoD2BSP(string path)
        {
            FilePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
            FileExt = Path.GetExtension(path);
            FileDirectory = Path.GetDirectoryName(path);
            OutputFolder = Path.Combine(FileDirectory, FileName);

            string cod2Dir = AppConfig.Settings().CoD2RootDirectory;

            if (cod2Dir != null)
            {
                if (Directory.Exists(Path.Combine(cod2Dir, "main")))
                {
                    GameDirectoryValid = true;
                    Console.WriteLine($"INFO: Game directory found: {cod2Dir}");
                }
                else
                {
                    GameDirectoryValid = false;
                    Console.WriteLine("WARNING: CoD 2 directory not found. No textures will be exported (see config.json)");
                }
            }
        }

        public void PrintData() { }

        public void LoadData()
        {
            using (BinaryReader stream = new BinaryReader(File.Open(FilePath, FileMode.Open)))
            {
                Lumps = ReadLumpList(stream);

                Textures = ReadLump<CoDMaterial>(stream, 0);
                Faces = ReadLump<TriangleSoups>(stream, 7);
                Vertices = ReadLump<CoD2Vertex>(stream, 8);
                MeshVerts = ReadLump<MeshVerts>(stream, 9);
            }

            for (int i = 0; i < Textures.Length; i++)
            {
                Textures[i].Name = Textures[i].Name.String().ToLower().Replace(@"\", @"/").ToCharArray();
            }
        }

        public void ExtractTextures()
        {
            if ( !GameDirectoryValid ) return;

            List<string> iwdFiles = new List<string>();
            string CoD2RootDirectory = AppConfig.Settings().CoD2RootDirectory;

            string[] cod2Main = Directory.GetFiles($"{CoD2RootDirectory}/main", "*.iwd");

            /* List of IWD archives to dig in */
            iwdFiles.AddRange(cod2Main);

            //////////////////////////////////////////////////////

            foreach (string file in iwdFiles)
            {
                using (ZipArchive zip = ZipFile.OpenRead(file))
                {
                    /* Material Path => Path to it's IWD file */
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (!entry.FullName.StartsWith(@"materials/")) continue;
                        if (entry.FullName.EndsWith(@"/")) continue;
                        MaterialsDictionary[entry.FullName] = file;
                    }

                    /* Texture Path => Path to it's IWD file */
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (!entry.FullName.StartsWith(@"images/")) continue;
                        if (entry.FullName.EndsWith(@"/")) continue;
                        if (!entry.FullName.EndsWith(@".iwi")) continue;
                        TexturesDictionary[entry.FullName] = file;
                    }
                }
            }

            int matIndex = 1;
            foreach (CoDMaterial mat in Textures)
            {
                string matName = mat.Name.String();
                string iwiFileOutput = $@"{OutputFolder}/materials/{matName}.dds";

                /* We must decode the material containing the texture name */
                byte[] matFile = ZipEntryToByteArray(MaterialsDictionary[$@"materials/{matName}"], $@"materials/{matName}");
                string extractedTex = IWI.ExtractTextureNameFromByteStream(matFile).Trim('\0');

                /* We can now extract and convert the texture as we have it's name */
                byte[] iwiFile = ZipEntryToByteArray(TexturesDictionary[$@"images/{extractedTex}.iwi"], $@"images/{extractedTex}.iwi");

                if ( File.Exists(iwiFileOutput) ) File.Delete(iwiFileOutput);
                IWI.ConvertToDDSFromByteStream(iwiFile, iwiFileOutput);

                Console.WriteLine($"({matIndex++}/{Textures.Length}) {matName}");
            }
        }

        public void CreateMaterials()
        {
            using (StreamWriter fs = new StreamWriter($@"{OutputFolder}/{FileName}.mtl"))
            {
                foreach (CoDMaterial mat in Textures)
                {
                    /* Example mat.Name:     materials/clip_nosight_metal (no extension)*/
                    string matName = mat.Name.String().Trim('\0');

                    fs.WriteLine($"newmtl {matName}"); /* Name of the material, doesn't really matter how we name it */
                    fs.WriteLine("Ns 225");
                    fs.WriteLine("Ka 1 1 1");
                    fs.WriteLine("Kd 0.8 0.8 0.8");
                    fs.WriteLine("Ks 0.5 0.5 0.5");
                    fs.WriteLine("Ke 0 0 0");
                    fs.WriteLine("Ni 1.45");
                    fs.WriteLine("d 1");
                    fs.WriteLine("illum 2");
                    fs.WriteLine(@$"map_Kd materials/{matName}.dds"); /* Texture's path */
                }
            }
        }

        public void WriteFile()
        {
            /* map.d3dbsp => ../map/map.obj */
            Directory.CreateDirectory(OutputFolder);

            int obj_index = 0;
            using (StreamWriter fs = new StreamWriter($@"{OutputFolder}/{FileName}.obj"))
            {
                /* Include material file */
                fs.WriteLine($"mtllib {FileName}.mtl");

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

                    fs.WriteLine($"v {x} {y} {z}"); // Create all vertices before iterating through meshes
                    fs.WriteLine($"vt {u} {1 - v}"); // We must flip the V coordinate
                    fs.WriteLine($"vn {nx} {ny} {nz}"); // Vertex normals
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
