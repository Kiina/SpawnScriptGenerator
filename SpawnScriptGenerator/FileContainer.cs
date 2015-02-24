namespace SpawnScriptGenerator
{
    class FileContainer
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public FileContainer(string fileName, string filePath)
        {
            FileName = fileName;
            FilePath = filePath;
        }

        public FileContainer() { }

        public override string ToString()
        {
            return (FilePath ?? "") + (FileName ?? "");
        }
    }
}
