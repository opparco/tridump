using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

/*
 * Update .tri files by RaceMenu Preset Sculpt
 */
    [DataContract]
    class Default
    {
        [DataMember]
        internal uint[] presets;
        [DataMember(Name="morphs")]
        internal float[] sliders;

        // aliases
        public uint NoseType    { get { return presets[0]; } }
        public uint EyesType    { get { return presets[2]; } }
        public uint LipType     { get { return presets[3]; } }

        public void Dump()
        {
            Console.WriteLine("-- Sliders --");
            Console.WriteLine("Nose Type: {0}", presets[0]);
            Console.WriteLine("Brow Type: {0}", presets[1]); // always -1
            Console.WriteLine("Eye Shape: {0}", presets[2]); // EyesType
            Console.WriteLine("Mouth Shape: {0}", presets[3]); // LipType

            Console.WriteLine("Nose Length: {0}", sliders[0]);
            Console.WriteLine("Nose Height: {0}", sliders[1]);
            Console.WriteLine("Jaw Height: {0}", sliders[2]);
            Console.WriteLine("Jaw Width: {0}", sliders[3]);
            Console.WriteLine("Jaw Forward: {0}", sliders[4]);
            Console.WriteLine("Cheekbone Height: {0}", sliders[5]);
            Console.WriteLine("Cheekbone Width: {0}", sliders[6]);
            Console.WriteLine("Eye Height: {0}", sliders[7]);
            Console.WriteLine("Eye Width: {0}", sliders[8]); // bug on RaceMenu 3.4: "Eye Depth"
            Console.WriteLine("Brow Height: {0}", sliders[9]);
            Console.WriteLine("Brow Width: {0}", sliders[10]);
            Console.WriteLine("Brow Forward: {0}", sliders[11]);
            Console.WriteLine("Mouth Height: {0}", sliders[12]);
            Console.WriteLine("Mouth Forward: {0}", sliders[13]);
            Console.WriteLine("Chin Height: {0}", sliders[14]);
            Console.WriteLine("Chin Width: {0}", sliders[15]);
            Console.WriteLine("Chin Forward: {0}", sliders[16]);
            Console.WriteLine("Eye Depth: {0}", sliders[17]);
            Console.WriteLine("");
        }
    }
    [DataContract]
    class Morph
    {
        [DataMember]
        internal short[][] data;
        [DataMember]
        internal string host;
        [DataMember(Name="vertices")]
        internal short num_positions;

        public void Dump()
        {
            Console.WriteLine("  host: {0} #positions: {1}", host, num_positions);
        }
    }
    [DataContract]
    class Morphs
    {
        [DataMember(Name="default")]
        internal Default m_default;
        [DataMember]
        internal Morph[] sculpt;
        [DataMember(Name="sculptDivisor")]
        internal short sculpt_divisor;

        public void Dump()
        {
            m_default.Dump();

            Console.WriteLine("Sculpt .tri files:");
            foreach (Morph morph in sculpt)
            {
                morph.Dump();
            }
            Console.WriteLine("sculpt divisor: {0}", sculpt_divisor);
        }
    }
    [DataContract]
    class RaceMenuSlot
    {
        [DataMember]
        internal Morphs morphs;

        public void Dump()
        {
            morphs.Dump();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: TriCharGen <source file>");
                return;
            }

            string source_file = args[0];

            FileStream stream = File.OpenRead(source_file);
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(RaceMenuSlot));
                RaceMenuSlot slot = (RaceMenuSlot)serializer.ReadObject(stream);
                slot.Dump();

                Console.WriteLine("-- assign Sculpt to .tri files --");

                float multiplier = 1.0f / slot.morphs.sculpt_divisor;
                string nose_type = string.Format("NoseType{0}", slot.morphs.m_default.NoseType);
                string eyes_type = string.Format("EyesType{0}", slot.morphs.m_default.EyesType);
                string lip_type = string.Format("LipType{0}", slot.morphs.m_default.LipType);

                Console.WriteLine("multiplier: {0}", multiplier);
                Console.WriteLine("nose type: {0}", nose_type);
                Console.WriteLine("eyes type: {0}", eyes_type);
                Console.WriteLine("lip type: {0}", lip_type);

                foreach (Morph morph in slot.morphs.sculpt)
                {
                    string tri_file = Path.Combine("meshes", morph.host);
                    if (!File.Exists(tri_file))
                    {
                        Console.WriteLine("error: file not found! {0}", tri_file);
                        continue;
                    }

                    tridump.triFile tri = new tridump.triFile();
                    Console.WriteLine("Load {0}", tri_file);
                    tri.Load(tri_file);

                    bool assigned = false;
                    foreach (tridump.Morph tri_morph in tri.morphs)
                    {
                        if (tri_morph.num_positions != morph.num_positions)
                        {
                            Console.WriteLine("error: #positions mismatch! {0} != {1}", tri_morph.num_positions, morph.num_positions);
                            continue;
                        }

                        if (assigned)
                            continue;

                        if (tri_morph.name == nose_type || tri_morph.name == eyes_type || tri_morph.name == lip_type)
                        {
                            AssignPositions(tri_morph, morph, multiplier);
                            assigned = true;
                        }
                    }

                    // overwrite
                    Console.WriteLine("Save {0}", tri_file);
                    tri.Save(tri_file);
                }
            }
            catch (SerializationException ex)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + ex.Message);
            }
            finally
            {
                stream.Close();
            }
        }

        static void AssignPositions(tridump.Morph tri_morph, Morph morph, float multiplier)
        {
            Console.WriteLine("  assign positions. morph name: {0}", tri_morph.name);

            foreach (short[] v in morph.data)
            {
                //Console.WriteLine("    v: {0} {1} {2} {3}", v[0], v[1], v[2], v[3]);
                short i = v[0];

                float x0 = tri_morph.positions[i].X * tri_morph.multiplier;
                float y0 = tri_morph.positions[i].Y * tri_morph.multiplier;
                float z0 = tri_morph.positions[i].Z * tri_morph.multiplier;

                float x1 = v[1] * multiplier;
                float y1 = v[2] * multiplier;
                float z1 = v[3] * multiplier;

                float x2 = x0 + x1;
                float y2 = y0 + y1;
                float z2 = z0 + z1;

                tri_morph.positions[i].X = (short)(x2 * 10000);
                tri_morph.positions[i].Y = (short)(y2 * 10000);
                tri_morph.positions[i].Z = (short)(z2 * 10000);
            }
            tri_morph.multiplier = 0.0001f;
        }

        static void AssignZeroPositions(tridump.Morph tri_morph)
        {
            Console.WriteLine("  assign zero positions. morph name: {0}", tri_morph.name);

            for (int i=0; i<tri_morph.num_positions; i++)
            {
                tri_morph.positions[i].X = 0;
                tri_morph.positions[i].Y = 0;
                tri_morph.positions[i].Z = 0;
            }
        }
    }
