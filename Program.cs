using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace HelldiversWwiseHandler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //these will be args eventually
            string path = @"E:\Game-Tools\Helldivers\Extractor\output\content\audio";
            string streampath = @"E:\Game-Tools\Helldivers\Extractor\output\content\audio\[wwise_stream]";
            string outputpath = @"E:\Game-Dumps\Helldivers2\Wwise";
            string[] banks = Directory.GetFiles(path, "*.wwise_bank", SearchOption.AllDirectories);
            string locale = "us";

            //DumpBanks(banks, outputpath, locale);
            GetWemNames(streampath, outputpath + @"\wemnames.txt", locale, outputpath);
            //Temp();

        }

        static void DumpBanks(string[] banks, string outputpath, string locale)
        {
            foreach (string bank in banks)
            {
                string inputbankname = Path.GetFileNameWithoutExtension(bank);
                string outputbankname = inputbankname + ".bnk";
                if (bank.Contains(@$"\{locale}\"))
                {
                    outputbankname = @$"{locale}\" + outputbankname;
                }
                using (var reader = new BinaryReader(File.OpenRead(bank)))
                {
                    reader.BaseStream.Position = 16;
                    byte[] streamdata = reader.ReadBytes((int)reader.BaseStream.Length - 16);
                    Directory.CreateDirectory(Path.Combine(outputpath, locale));
                    using (var writer = new BinaryWriter(File.Create(Path.Combine(outputpath, outputbankname))))
                    {
                        writer.Write(streamdata);
                    }
                }
            }
        }

        static void GetWemNames(string streampath, string wemidsfile, string locale, string outputpath)
        {
            string[] wemids = File.ReadAllLines(wemidsfile);
            int matchcount = 0;
            int locmatchcount = 0;
            int unmatchedcount = 0;
            List<string> matchedhashes = new List<string>();
            string[] streams = Directory.GetFiles(streampath);

            for (int i = 0;i < wemids.Length; i++)
            {
                string hash = new Hash64(wemids[i]).ToHex();
                string lochash = new Hash64(wemids[i].Insert(14, $"{locale}/")).ToHex();
                if (hash.Length < 16)
                {
                    for (int j = 0; hash.Length < 16; j++)
                        hash = hash.Insert(0, "0");
                }
                if (lochash.Length < 16)
                {
                    for (int j = 0; lochash.Length < 16; j++)
                        lochash = lochash.Insert(0, "0");
                }
                if (File.Exists(Path.Combine(streampath, hash + ".wwise_stream")))
                {
                    matchcount++;
                    matchedhashes.Add(wemids[i]);
                    using (var reader = new BinaryReader(File.OpenRead(Path.Combine(streampath, hash + ".wwise_stream"))))
                    {
                        reader.BaseStream.Position = 12;
                        byte[] streamdata = reader.ReadBytes((int)reader.BaseStream.Length - 12);
                        Directory.CreateDirectory(Path.Combine(outputpath, locale));
                        using (var writer = new BinaryWriter(File.Create(Path.Combine(outputpath, "wem", wemids[i].Remove(0, 14) + ".wem"))))
                        {
                            writer.Write(streamdata);
                        }
                    }
                }
                else if (File.Exists(Path.Combine(streampath, lochash + ".wwise_stream")))
                {
                    locmatchcount++;
                    matchedhashes.Add(wemids[i].Insert(14, $"{locale}/"));
                    using (var reader = new BinaryReader(File.OpenRead(Path.Combine(streampath, lochash + ".wwise_stream"))))
                    {
                        reader.BaseStream.Position = 12;
                        byte[] streamdata = reader.ReadBytes((int)reader.BaseStream.Length - 12);
                        Directory.CreateDirectory(Path.Combine(outputpath, locale));
                        using (var writer = new BinaryWriter(File.Create(Path.Combine(outputpath, "wem", locale, wemids[i].Remove(0, 14) + ".wem"))))
                        {
                            writer.Write(streamdata);
                        }
                    }
                }
                else
                {
                    unmatchedcount++;
                }
            }
            int totalcount = matchcount + locmatchcount;
            Console.WriteLine("Match count: " + matchcount + " Loc match count: " + locmatchcount + " total count: " + totalcount);

            File.WriteAllLines(Path.Combine(outputpath, "matchedhashes.txt"), matchedhashes);
        }

        static void Temp()
        {
            List<string> temp = new List<string>(File.ReadAllLines(@"E:\Game-Tools\Helldivers\Extractor\audio.txt"));
            List<string> dir = new List<string>(Directory.EnumerateFiles(@"E:\Game-Dumps\Helldivers2\Wwise", "*.bnk", SearchOption.AllDirectories));

            foreach (string line in dir)
            {
                string name = line.Substring(32);
                string name2 = name.Remove(name.Length - 4);
                string name3 = name2.Insert(0, "content/audio/").Replace(@"\", "/");


                if (!temp.Contains(name3))
                {
                    temp.Add(name3);
                }
            }
            File.WriteAllLines(@"E:\Game-Tools\Helldivers\Extractor\audionew.txt", temp);
        }

        public static class ImportFuncs
        {
            [DllImport("stingray_murmur.dll", EntryPoint = "stingray_murmur_64")]
            public static extern UInt64 stingray_murmur_64(String str);

            [DllImport("stingray_murmur.dll", EntryPoint = "stingray_murmur_32")]
            public static extern UInt32 stingray_murmur_32(String str);
        }

        public class Hash64
        {
            public ulong Hash = 0UL;
            public Hash64(string input)
            {
                Hash = ImportFuncs.stingray_murmur_64(input);
            }

            public string ToHex()
            {
                return Hash.ToString("X").ToLower();
            }
        }
    }
}