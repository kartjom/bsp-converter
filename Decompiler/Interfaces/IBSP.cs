namespace Decompiler
{
    interface IBSP
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileDirectory { get; set; }
        public string OutputFolder { get; set; }
        public bool GameDirectoryValid { get; set; }

        public void PrintData();
        public void LoadData();
        public void ExtractTextures();
        public void CreateMaterials();
        public void WriteFile();
    }
}
