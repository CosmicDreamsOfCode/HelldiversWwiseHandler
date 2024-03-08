using System.Linq;

namespace HelldiversWwiseHandler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //these will be args eventually
            string path = @"E:\Game-Tools\Helldivers\Extractor\output\content\audio";
            string inputbank = "music.wwise_bank";
            string streampath = @"E:\Game-Tools\Helldivers\Extractor\output\content\audio\[wwise_stream]";
            string outputpath = @"E:\Media\Music\Rips\Helldivers2\New";
            string[] wemstreams = Directory.GetFiles(streampath, "*.wwise_stream");

            using (var reader = new BinaryReader(File.OpenRead(Path.Combine(path, inputbank))))
            {
                reader.BaseStream.Position = 16; //skip stingray header
                reader.ReadChars(4); //BKHD
                uint headersize = reader.ReadUInt32(); //header size
                reader.BaseStream.Position += headersize; //just skip the rest we dont need to read any of it

                reader.ReadChars(4); //DIDX
                uint didxsize = reader.ReadUInt32(); //chunk size

                if (didxsize <= 0) //if theres no DIDX data i think that means theres nothing stored in here, there can be banks without any wems
                {
                    Console.WriteLine("Bank has no DIDX chunk data! (Corrupted or has no files?)");
                    return;
                }

                uint wemcount = didxsize / 12;
                WEMEntry[] wementries = new WEMEntry[wemcount];
                for (int i = 0; i < wemcount; i++)
                {
                    wementries[i] = new WEMEntry();
                    wementries[i].id = reader.ReadUInt32();
                    wementries[i].offset = reader.ReadUInt32();
                    wementries[i].size = reader.ReadUInt32();
                }

                reader.ReadChars(4); //DATA
                reader.ReadUInt32(); //chunk size
                long datapos = reader.BaseStream.Position; //save position of the start of the data chunk

                int prefetchcount = 0;
                int matchcount = 0;
                foreach (WEMEntry entry in wementries)
                {
                    reader.BaseStream.Position = datapos + entry.offset;
                    reader.ReadChars(4); //RIFF
                    uint size = reader.ReadUInt32();
                    if (size > entry.size - 8) //Prefetches have the same size in the header as their streams, but are just a small part, so if we do this, it should be able to detect if its one
                    {
                        entry.prefetch = true;
                        prefetchcount++;
                    }

                    reader.BaseStream.Position = datapos + entry.offset; //go back to start of data
                    entry.data = reader.ReadBytes((int)entry.size); //read data into an array

                    if (entry.prefetch)
                    {
                        foreach (string wemstream in wemstreams)
                        {
                            using (var wemreader = new BinaryReader(File.OpenRead(wemstream)))
                            {
                                wemreader.BaseStream.Position = 12; //skip stingray header
                                byte[] compare = wemreader.ReadBytes(entry.data.Length);
                                if (compare.SequenceEqual(entry.data)) //we need to compare the prefetch data to streams to match them, if its a match we wanna do some stuff
                                {
                                    Console.WriteLine(entry.id);
                                    matchcount++;
                                    wemreader.BaseStream.Position = 12; //go back to start of file
                                    byte[] streamdata = wemreader.ReadBytes((int)wemreader.BaseStream.Length -12);
                                    using (var writer = new BinaryWriter(File.Create(Path.Combine(outputpath, entry.id + ".wem"))))
                                    {
                                        writer.Write(streamdata);

                                    }
                                }

                            }
                        }
                    }
                }

                Console.WriteLine("entry count: " + wementries.Count() + " prefetch count: " + prefetchcount + " match count: " + matchcount);
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