using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    // Класс-помощник для заголовка
    public class FileHeaderHelper
    {
        private readonly FileStream _prodFs;
        private readonly FileStream _specFs;

        public FileHeaderHelper(FileStream prodFs, FileStream specFs)
        {
            _prodFs = prodFs;
            _specFs = specFs;
        }

        public int GetFirstProd() => ReadInt(_prodFs, 4);
        public void SetFirstProd(int value) => WriteInt(_prodFs, 4, value);
        public int GetFreeProd() => ReadInt(_prodFs, 8);
        public void UpdateFreeProd(int value) => WriteInt(_prodFs, 8, value);
        public int GetFreeSpec() => ReadInt(_specFs, 4);
        public void UpdateFreeSpec(int value) => WriteInt(_specFs, 4, value);

        private int ReadInt(FileStream fs, long position)
        {
            fs.Seek(position, SeekOrigin.Begin);
            byte[] b = new byte[4];
            fs.Read(b, 0, 4);
            return BitConverter.ToInt32(b, 0);
        }

        private void WriteInt(FileStream fs, long position, int value)
        {
            fs.Seek(position, SeekOrigin.Begin);
            fs.Write(BitConverter.GetBytes(value), 0, 4);
        }
    }
}
