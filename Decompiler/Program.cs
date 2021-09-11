using System;
using System.IO;
using BinaryFileReader;

namespace Decompiler
{
    static class Project
    {
        // Path to executable's folder
        public static readonly string RootLocation = AppDomain.CurrentDomain.BaseDirectory;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Project.RootLocation);
            string path = null;

            try {
                path = args[0];

                if (!File.Exists(path)) throw new FileNotFoundException();
            }
            catch(FileNotFoundException) {
                Console.WriteLine("ERROR: BSP file not found");
            }
            catch (IndexOutOfRangeException) {
                Console.WriteLine("ERROR: Drag and drop file onto executable to decompile");
            }
            finally {
                if (args.Length <= 0)
                {
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }

            Header header = BinLib.OpenReadCloseBinary<Header>(path);
            IBSP bsp;

            switch (header.Version)
            {
                case 59: bsp = new CoD1BSP(path); break;
                case 4: bsp = new CoD2BSP(path); break;
                
                    // TODO - Not finished
                case 18: // Beta Allied Assault
                case 19: // Release Allied Assault
                case 21: bsp = new MoHAABSP(path); break; // Breakthrough

                default:
                    Console.WriteLine($"ERROR: Unsupported BSP file version {header.Version}");
                    Console.ReadKey();
                    return;
            }

            Console.WriteLine($"INFO: Opened map {bsp.FileName}{bsp.FileExt}");

            bsp.LoadData();
            Console.WriteLine("INFO: Data loaded");

            bsp.PrintData();
           
            bsp.WriteFile();
            Console.WriteLine("INFO: Extracted geometry");

            bsp.CreateMaterials();
            Console.WriteLine("INFO: Materials created");

            if (bsp.GameDirectoryValid)
            {
                Console.Write("Extract textures from game files? (y/n) ");
                if (Console.ReadLine().Trim().ToLower() == "y")
                {
                    bsp.ExtractTextures();
                }
            }

            Console.WriteLine("INFO: Finished writing file");
            Console.WriteLine("\nBlender info: Clear Custom Split Normals Data; Flip Normals");
            Console.ReadKey();
        }
    }
}
