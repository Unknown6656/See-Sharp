﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Drawing;
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
                #region REFLECTION AND BASIC FILE STUFF

                Bitmap bmp = Properties.Resources.emma;
                Bitmap bmp_s = new Bitmap(bmp, new Size(bmp.Width / 3, bmp.Height / 3));
                Assembly asm = Assembly.LoadFrom(args[0]);
                FileInfo targ = new FileInfo(args[1]);
                string targdir = targ.Directory.FullName;
                Type fx_base = asm.GetType("SeeSharp.BitmapEffect");
                Type fx_inst = asm.GetType("SeeSharp.InstagramEffect");
                Type fx_scol = asm.GetType("SeeSharp.BitmapColorEffect");
                Type fx_matr = asm.GetType("SeeSharp.SingleMatrixConvolutionBitmapEffect");
                IEnumerable<(object, Type)> effects = from t in asm.GetTypes()
                                                      where !t.IsAbstract
                                                      where t.IsClass
                                                      where fx_base.IsAssignableFrom(t)
                                                      where t != fx_scol
                                                      where t != fx_matr
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
                                                      select (Activator.CreateInstance(t), t);

                foreach (FileInfo file in targ.Directory.GetFiles())
                    file.Delete();

                int incr = 0;

                string filename() => $"{++incr:x8}.png";

                #endregion
                #region .MD HEADER

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($@"<!-- Autogenerated : {DateTime.Now:yyyy-MM-dd HH:mm:ss:ffffff} -->
# See# documentation _(beta!)_

<img src=""./{args[2]}favicon.ico"" height=""50""/>

_**WARNING:** The library's soruce code is not for the faint of heart. It is ported legacy-code from an older project._<br/>
_**WARNING:** This markdown document has been autogenerated by one of the worst C# programs I've ever written ..... so use at your own risk..._

<hr/>

### Defined effects:
The See# Library has a few ({effects.Count()}) pre-defined bitmap effects.
The following list contains all pre-defined effects and (in most cases) a generated image rendered with the respective bitmap effect.
<ul>");

                #endregion
                #region LIST EFFECTS

                Type fx_opac = asm.GetType("SeeSharp.Effects.OpacityBitmapEffect");

                foreach ((object fx, Type type) in effects)
                {
                    sb.AppendLine($"<li><b>`{type.Name}`</b>");

                    if (fx_matr.IsAssignableFrom(type) || fx_inst.IsAssignableFrom(type))
                        sb.AppendLine($@"   <br/><img src=""{render()}"" height=""200""/>");
                    else if (fx_scol.IsAssignableFrom(type))
                    {
                        sb.AppendLine(@"   <br/>Effect applied to ...<br/>
<table><tbody><tr><th>0%</th><th>25%</th><th>50%</th><th>75%</th><th>100%</th></tr><tr>");
                        for (int i = 0; i < 5; i++)
                        {
                            double v = i / 4d;

                            sb.Append($@"<td><img src=""{render(false, type == fx_opac ? 1 - v : v)}"" height=""100""/></td>");
                        }

                        sb.AppendLine(@"</tr></tbody></table>");
                    }

                    sb.AppendLine("</li>");

                    string render(bool big = true, params object[] param)
                    {
                        string file = filename();

                        (fx_base.GetMethod("Apply").Invoke((param ?? new object[0]).Length > 0 ? Activator.CreateInstance(type, param) : fx,
                                                           new object[] { big ? bmp : bmp_s }) as Bitmap).Save($"{targdir}\\{file}");

                        return $"./{file}";
                    }
                }

                #endregion
                #region

                sb.AppendLine("</ul>");

                #endregion
                #region WRITE STRING INTO .MD-FILE

                using (FileStream fs = new FileStream(targ.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    sw.Write(sb.ToString());
                }

                return 0;

                #endregion
            }
            #region EXCEPTION HANDLING

            catch (Exception ex)
            {
                Console.WriteLine("========================================= START OF ERROR REPORT =========================================");

                while (ex != null)
                {
                    Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");

                    ex = ex.InnerException;
                }

                Console.WriteLine("========================================== END OF ERROR REPORT ==========================================");

                return -1;
            }

            #endregion
        }
    }
}
