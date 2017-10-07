using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace tridump
{
    // float = short * multiplier
    struct Vector3
    {
        public short X;
        public short Y;
        public short Z;
    }

    class Morph
    {
        public string name;
        public float multiplier;
        public Vector3[] positions;

        public int num_positions;

        public Morph(int num_positions)
        {
            this.num_positions = num_positions;
        }

        public void Dump()
        {
            Console.WriteLine("  name: {0}", name);
#if false
            Console.WriteLine("  multiplier: {0:F8}", multiplier);
            for (int i=0; i<num_positions; i++)
            {
                Console.WriteLine("    v: {0} {1} {2} {3}", i, positions[i].X, positions[i].Y, positions[i].Z);
            }
#endif
        }

        // read SizedString
        // len: uint
        // value: array of char (null terminated)
        public static string ReadSizedString(BinaryReader reader)
        {
            uint len = reader.ReadUInt32();
            StringBuilder string_builder = new StringBuilder();
            while (string_builder.Length != len)
            {
                char c = reader.ReadChar();
                if (c == 0)
                    break;
                string_builder.Append(c);
            }
            return string_builder.ToString();
        }

        public static void ReadVector3(BinaryReader reader, out Vector3 v)
        {
            v.X = reader.ReadInt16();
            v.Y = reader.ReadInt16();
            v.Z = reader.ReadInt16();
        }

        public void Read(BinaryReader reader)
        {
            this.name = ReadSizedString(reader);
            this.multiplier = reader.ReadSingle();

            this.positions = new Vector3[num_positions];

            for (int pi=0; pi<num_positions; pi++)
            {
                ReadVector3(reader, out this.positions[pi]);
            }
        }

        public static void WriteSizedString(BinaryWriter writer, string value)
        {
            writer.Write((uint)value.Length + 1);
            foreach (byte i in Encoding.Default.GetBytes(value))
                writer.Write(i);

            writer.Write((byte)0);
        }

        public static void Write(BinaryWriter writer, ref Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public void Write(BinaryWriter writer)
        {
            WriteSizedString(writer, this.name);
            writer.Write(this.multiplier);

            for (int pi=0; pi<num_positions; pi++)
            {
                Write(writer, ref this.positions[pi]);
            }
        }
    }

    class triFile
    {
        readonly byte[] magic_expect = System.Text.Encoding.ASCII.GetBytes("FRTRI003");

        internal int num_positions;
        internal int num_triangles;
        internal int num_quads;
        internal int unknown2;
        internal int unknown3;
        internal int num_texcoords;
        internal int flags;
        internal int num_morphs;
        internal int num_extend_morphs;
        internal int num_extend_positions;
        internal int unknown7;
        internal int unknown8;
        internal int unknown9;
        internal int unknown0;

        internal byte[] bin_positions;
        internal byte[] bin_position_indices;
        internal byte[] bin_texcoords;
        internal byte[] bin_texcoord_indices;

        internal Morph[] morphs;

        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        public void Load(Stream source_stream)
        {
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            byte[] magic = reader.ReadBytes(0x08);
            for (int i=0; i<8; i++)
                if (magic[i] != magic_expect[i])
                    throw new FormatException("File is not tri!");

            this.num_positions = reader.ReadInt32();
            this.num_triangles = reader.ReadInt32();
            this.num_quads = reader.ReadInt32();
            this.unknown2 = reader.ReadInt32();
            this.unknown3 = reader.ReadInt32();
            this.num_texcoords = reader.ReadInt32();
            this.flags = reader.ReadInt32();
            this.num_morphs = reader.ReadInt32();
            this.num_extend_morphs = reader.ReadInt32();
            this.num_extend_positions = reader.ReadInt32();
            this.unknown7 = reader.ReadInt32();
            this.unknown8 = reader.ReadInt32();
            this.unknown9 = reader.ReadInt32();
            this.unknown0 = reader.ReadInt32();

            this.bin_positions = reader.ReadBytes(num_positions * 3 * sizeof(float));
            this.bin_position_indices = reader.ReadBytes(num_triangles * 3 * sizeof(int));

            this.bin_texcoords = reader.ReadBytes(num_texcoords * 2 * sizeof(float));
            this.bin_texcoord_indices = reader.ReadBytes(num_triangles * 3 * sizeof(int));

            this.morphs = new Morph[num_morphs];

            for (int mi=0; mi<num_morphs; mi++)
            {
                morphs[mi] = new Morph(num_positions);
                morphs[mi].Read(reader);
            }

            //Console.WriteLine("stream position: {0}", source_stream.Position);
        }

        public void Dump()
        {
            Console.WriteLine("#positions: {0}", num_positions);
            Console.WriteLine("#triangles: {0}", num_triangles);
            Console.WriteLine("#quads: {0}", num_quads);
            Console.WriteLine("#texcoords: {0}", num_texcoords);
            Console.WriteLine("#flags: {0:X8}", flags);
            Console.WriteLine("#morphs: {0}", num_morphs);
            Console.WriteLine("#extend positions: {0}", num_extend_positions);
            Console.WriteLine("#extend morphs: {0}", num_extend_morphs);

            Console.WriteLine("Morphs:");
            for (int mi=0; mi<num_morphs; mi++)
                morphs[mi].Dump();
        }

        public void Save(string dest_file)
        {
            using (Stream dest_stream = File.Create(dest_file))
                Save(dest_stream);
        }

        public void Save(Stream dest_stream)
        {
            BinaryWriter writer = new BinaryWriter(dest_stream, System.Text.Encoding.Default);

            writer.Write(this.magic_expect);

            writer.Write(this.num_positions);
            writer.Write(this.num_triangles);
            writer.Write(this.num_quads);
            writer.Write(this.unknown2);
            writer.Write(this.unknown3);
            writer.Write(this.num_texcoords);
            writer.Write(this.flags);
            writer.Write(this.num_morphs);
            writer.Write(this.num_extend_morphs);
            writer.Write(this.num_extend_positions);
            writer.Write(this.unknown7);
            writer.Write(this.unknown8);
            writer.Write(this.unknown9);
            writer.Write(this.unknown0);

            writer.Write(this.bin_positions);
            writer.Write(this.bin_position_indices);

            writer.Write(this.bin_texcoords);
            writer.Write(this.bin_texcoord_indices);

            for (int mi=0; mi<num_morphs; mi++)
            {
                morphs[mi].Write(writer);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: tridump <tri file>");
                return;
            }

            string tri_file = args[0];

            triFile tri = new triFile();
            try
            {
                tri.Load(tri_file);
                tri.Dump();
                tri.Save("out.tri");
            }
            catch (FormatException ex)
            {
                Console.WriteLine("Failed to load. Reason: {0}", ex.Message);
            }
        }
    }
}
