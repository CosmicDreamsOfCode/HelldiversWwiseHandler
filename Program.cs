using System.Linq;
using System.Reflection.PortableExecutable;

namespace HelldiversWwiseHandler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //these will be args eventually
            string path = @"E:\Game-Tools\Helldivers\Extractor\output\content\audio";
            string outputpath = @"E:\Game-Dumps\Helldivers2\Wwise";
            string[] banks = Directory.GetFiles(path, "*.wwise_bank", SearchOption.AllDirectories);
            string lang = "us";

            foreach (string bank in banks)
            {
                string inputbankname = Path.GetFileNameWithoutExtension(bank);
                string outputbankname = inputbankname + ".bnk";
                if (bank.Contains(@$"\{lang}\"))
                {
                    outputbankname = @$"{lang}\" + outputbankname;
                }
                using (var reader = new BinaryReader(File.OpenRead(bank)))
                {
                    reader.BaseStream.Position = 16;
                    byte[] streamdata = reader.ReadBytes((int)reader.BaseStream.Length - 16);
                    Directory.CreateDirectory(Path.Combine(outputpath, lang));
                    using (var writer = new BinaryWriter(File.Create(Path.Combine(outputpath, outputbankname))))
                    {
                        writer.Write(streamdata);
                    }
                }
            }
        }

        public class WEMEntry
        {
            public uint id;
            public uint offset;
            public uint size;
            public bool prefetch = false;
            public byte[] data;
        }
    }
}