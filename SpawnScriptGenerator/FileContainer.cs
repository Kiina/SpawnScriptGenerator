using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpawnScriptGenerator
{
    class FileContainer
    {
        public string fileName { get; set; }
        public string filePath { get; set; }

        public FileContainer(string fileName, string filePath)
        {
            this.fileName = fileName;
            this.filePath = filePath;
        }

        public FileContainer() { }

        public override string ToString()
        {
            return (filePath ?? "") + (fileName ?? "");
        }
    }
}
