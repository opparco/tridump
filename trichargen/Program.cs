using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace trichargen
{
    [DataContract]
    class Default
    {
        [DataMember]
        internal uint[] presets;
        [DataMember(Name = "morphs")]
        internal float[] sliders;

        // aliases
        public uint NoseType { get { return presets[0]; } }
        public uint EyesType { get { return presets[2]; } }
        public uint LipType { get { return presets[3]; } }

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
        [DataMember(Name = "vertices")]
        internal short num_positions;

        public void Dump()
        {
            Console.WriteLine("  host: {0} #positions: {1}", host, num_positions);
        }
    }
    [DataContract]
    class Morphs
    {
        [DataMember(Name = "default")]
        internal Default m_default;
        [DataMember]
        internal Morph[] sculpt;
        [DataMember(Name = "sculptDivisor")]
        internal short sculpt_divisor;

        public void Dump()
        {
            m_default.Dump();

            if (sculpt == null)
                return;

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
                System.Console.WriteLine("Usage: trichargen <source file>");
                return;
            }

            string source_file = args[0];

            FileStream stream = File.OpenRead(source_file);
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(RaceMenuSlot));
                RaceMenuSlot slot = (RaceMenuSlot)serializer.ReadObject(stream);
                slot.Dump();

                if (slot.morphs.sculpt == null)
                    return;

                Console.WriteLine("-- assign Sculpt to .tri files --");

                float multiplier = 1.0f / slot.morphs.sculpt_divisor;
                Console.WriteLine("multiplier: {0}", multiplier);

                string origin_type = string.Format("NoseType{0}", slot.morphs.m_default.NoseType);
                string preset_type = "NoseType32";

                Console.WriteLine("origin type: {0}", origin_type);
                Console.WriteLine("preset type: {0}", preset_type);

                foreach (Morph morph in slot.morphs.sculpt)
                {
                    string tri_file = Path.Combine("meshes", morph.host);
                    if (!File.Exists(tri_file))
                    {
                        Console.WriteLine("error: file not found! {0}", tri_file);
                        continue;
                    }

                    tridump.TriFile tri = new tridump.TriFile();
                    Console.WriteLine("updating {0}", tri_file);
                    tri.Load(tri_file);

                    // find the morph of origin type
                    tridump.Morph origin_tri_morph = null;
                    foreach (tridump.Morph tri_morph in tri.morphs)
                    {
                        if (tri_morph.num_positions != morph.num_positions)
                        {
                            Console.WriteLine("error: #positions mismatch! {0} != {1}", tri_morph.num_positions, morph.num_positions);
                            continue;
                        }

                        if (tri_morph.name == origin_type)
                        {
                            origin_tri_morph = tri_morph;
                            break;
                        }
                    }

                    // find the morph of preset type
                    tridump.Morph preset_tri_morph = null;
                    foreach (tridump.Morph tri_morph in tri.morphs)
                    {
                        if (tri_morph.num_positions != morph.num_positions)
                        {
                            Console.WriteLine("error: #positions mismatch! {0} != {1}", tri_morph.num_positions, morph.num_positions);
                            continue;
                        }

                        if (tri_morph.name == preset_type)
                        {
                            preset_tri_morph = tri_morph;
                            break;
                        }
                    }

                    if (preset_tri_morph != null)
                    {
                        if (origin_tri_morph != null)
                            AssignPositions(preset_tri_morph, origin_tri_morph, morph, multiplier);
                        else
                            AssignPositions(preset_tri_morph, morph, multiplier);
                    }
                    else
                    {
                        // append new morph
                        tridump.Morph tri_morph = new tridump.Morph(tri.num_positions);
                        tri_morph.name = preset_type;
                        tri_morph.positions = new tridump.Vector3[tri.num_positions];

                        if (origin_tri_morph != null)
                            AssignPositions(tri_morph, origin_tri_morph, morph, multiplier);
                        else
                            AssignPositions(tri_morph, morph, multiplier);

                        tridump.Morph[] new_morphs = new tridump.Morph[tri.num_morphs + 1];
                        for (int i = 0; i < tri.num_morphs; i++)
                        {
                            new_morphs[i] = tri.morphs[i];
                        }
                        new_morphs[tri.num_morphs] = tri_morph;
                        tri.morphs = new_morphs;
                        tri.num_morphs++;
                    }

                    // overwrite
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

        static void AssignPositions(tridump.Morph tri_morph, tridump.Morph origin_tri_morph, Morph morph, float multiplier)
        {
            Console.WriteLine("  assign positions. morph name: {0}", tri_morph.name);

            foreach (short[] v in morph.data)
            {
                short i = v[0];

                float x0 = origin_tri_morph.positions[i].X * origin_tri_morph.multiplier;
                float y0 = origin_tri_morph.positions[i].Y * origin_tri_morph.multiplier;
                float z0 = origin_tri_morph.positions[i].Z * origin_tri_morph.multiplier;

                float x1 = v[1] * multiplier;
                float y1 = v[2] * multiplier;
                float z1 = v[3] * multiplier;

                float x2 = x0 + x1;
                float y2 = y0 + y1;
                float z2 = z0 + z1;

                tri_morph.positions[i].X = (short)(x2 * 10000.0f);
                tri_morph.positions[i].Y = (short)(y2 * 10000.0f);
                tri_morph.positions[i].Z = (short)(z2 * 10000.0f);
            }
            tri_morph.multiplier = 0.0001f;
        }

        static void AssignPositions(tridump.Morph tri_morph, Morph morph, float multiplier)
        {
            Console.WriteLine("  assign positions. morph name: {0}", tri_morph.name);

            foreach (short[] v in morph.data)
            {
                short i = v[0];

                tri_morph.positions[i].X = v[1];
                tri_morph.positions[i].Y = v[2];
                tri_morph.positions[i].Z = v[3];
            }
            tri_morph.multiplier = multiplier;
        }
    }
}
