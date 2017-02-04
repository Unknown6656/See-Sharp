using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using SeeSharp.Effects;
using SeeSharp;

namespace Testing
{
    public static class Program
    {
        public static readonly string dir = $@"{new FileInfo(typeof(Program).Assembly.Location).Directory.FullName}\output\";

        public static int Main(string[] args)
        {
            Bitmap src = Properties.Resources.test_image;

            Directory.SetCurrentDirectory($@"{dir}..\");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            int main()
            {
                try
                {
                    src.ApplyEffect<NashvilleBitmapEffect>()
                       .Save($"{dir}{DateTime.Now:yyyy-MM-dd-HH-mm-ss-ffffff}.png", ImageFormat.Png);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("OK.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    while (ex != null)
                    {
                        Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");

                        ex = ex.InnerException;
                    }

                    return -1;
                }

                return 0;
            }

            int res = main();

            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey(true);

            return res;
        }
    }
}
