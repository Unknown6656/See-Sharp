using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Text;
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

                return 0;
            }
            catch
            {
                return -1;
            }
        }
    }
}
