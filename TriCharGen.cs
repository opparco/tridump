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
        uint[] presets;
        [DataMember(Name="morphs")]
        float[] sliders;

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
    class MorphData
    {
        [DataMember]
        internal short i;
        [DataMember]
        internal short x;
        [DataMember]
        internal short y;
        [DataMember]
        internal short z;
    }
    [DataContract]
    class Morph
    {
        [DataMember]
        internal MorphData[] data;
        [DataMember]
        internal string host;
        [DataMember]
        internal short vertices;

        public void Dump()
        {
            Console.WriteLine("  host: {0} vertices: {1}", host, vertices);
        }
    }
    [DataContract]
    class Morphs
    {
        [DataMember(Name="default")]
        Default m_default;
        [DataMember]
        Morph[] sculpt;
        [DataMember]
        short sculptDivisor;

        public void Dump()
        {
            m_default.Dump();

            Console.WriteLine("Sculpt .tri files:");
            foreach (Morph morph in sculpt)
            {
                morph.Dump();
            }
            Console.WriteLine("sculptDivisor: {0}", sculptDivisor);
        }
    }
    [DataContract]
    class RaceMenuSlot
    {
        [DataMember]
        Morphs morphs;

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
    }
