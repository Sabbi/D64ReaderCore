using D64Reader.Renderers;
using System;
using System.IO;
using System.Linq;
using test.Properties;

namespace test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var image = Resources._2ND1.ToArray();

            using (var ms = new MemoryStream(image))
            {
                var d64Reader = new D64Reader.D64ReaderCore(ms);

                var result = d64Reader.Render(new D64PngRenderer());

                Console.WriteLine(result.Length);
            }
        }
    }
}