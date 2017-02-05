using System.Runtime.ExceptionServices;
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

        [STAThread, HandleProcessCorruptedStateExceptions]
        public static int Main(string[] args)
        {
            Directory.SetCurrentDirectory($@"{dir}..\");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool res = InnerMain(Properties.Resources.test_image_1, Properties.Resources.test_image_2);

            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey(true);

            return res ? 0 : 1;
        }

        private static bool InnerMain(Bitmap src1, Bitmap src2)
        {
            bool err = false;

            foreach ((BitmapEffect fx, Type t) in from t in typeof(BitmapEffect).Assembly.GetTypes()
                                                  where !t.IsAbstract
                                                  where t.IsClass
                                                  where typeof(BitmapEffect).IsAssignableFrom(t)
                                                  orderby t.Name ascending
                                                  let attr = t.GetCustomAttributes(true)
                                                  let obs = from a in attr
                                                            where a is ObsoleteAttribute
                                                            select a
                                                  where !obs.Any()
                                                  let cons = from c in t.GetConstructors()
                                                             where c.GetParameters().Length == 0
                                                             select c
                                                  where cons.Any()
                                                  select (Activator.CreateInstance(t) as BitmapEffect, t))
                try
                {
                    Bitmap dst = fx is BitmapBlendEffect blend ? blend.Blend(src1, src2) : src1.ApplyEffect(fx);

                    dst.Save($"{dir}[{DateTime.Now:yyyy-MM-dd HH-mm-ss}] {t.FullName}.png", ImageFormat.Png);

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

                    err |= true;
                }

            return err;
        }
    }
}
