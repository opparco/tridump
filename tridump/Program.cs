using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tridump
{
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

            TriFile tri = new TriFile();
            try
            {
                tri.Load(tri_file);
                tri.Dump();
                //tri.Save("out.tri");
            }
            catch (FormatException ex)
            {
                Console.WriteLine("Failed to load. Reason: {0}", ex.Message);
            }
        }
    }
}
