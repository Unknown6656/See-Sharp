using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace Documentation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(args[0]);
                FileInfo targ = new FileInfo(args[1]);

                if (!targ.Exists)
                    targ.Create();


                return 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}
