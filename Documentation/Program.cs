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

                const int resize_factor = 3;
                Bitmap bmp = Properties.Resources.emma;
                Bitmap bmp2 = Properties.Resources.hsv_map;
                Bitmap bmp_s = new Bitmap(bmp, new Size(bmp.Width / resize_factor, bmp.Height / resize_factor));
                Bitmap bmp2_s = new Bitmap(bmp2, new Size(bmp2.Width / resize_factor, bmp2.Height / resize_factor));
                Assembly asm = Assembly.LoadFrom(args[0]);
                FileInfo targ = new FileInfo(args[1]);
                string targdir = targ.Directory.FullName;
                Type fx_base = asm.GetType("SeeSharp.BitmapEffect");
                Type fx_inst = asm.GetType("SeeSharp.InstagramEffect");
                Type fx_scol = asm.GetType("SeeSharp.BitmapColorEffect");
                Type fx_conv = asm.GetType("SeeSharp.MatrixConvolutionBitmapEffect");
                Type fx_matr = asm.GetType("SeeSharp.SingleMatrixConvolutionBitmapEffect");
                Type fx_blendb = asm.GetType("SeeSharp.BitmapBlendEffect");
                Type fx_blendc = asm.GetType("SeeSharp.ColorBlendEffect");
                Type fx_transf = asm.GetType("SeeSharp.BitmapTransformEffect");
                IEnumerable<(object, Type)> effects = from t in asm.GetTypes()
                                                      where !t.IsAbstract
                                                      where t.IsClass
                                                      where fx_base.IsAssignableFrom(t)
                                                      where t != fx_scol
                                                      where t != fx_matr
                                                      where t != fx_transf
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
                string save(Bitmap b)
                {
                    string file = filename();

                    b.Save($"{targdir}\\{file}");

                    return $"./{file}";
                }

                #endregion
                #region .MD HEADER

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($@"<!-- Autogenerated : {DateTime.Now:yyyy-MM-dd HH:mm:ss:ffffff} -->
# See# documentation _(beta!)_

<img src=""./{args[2]}favicon.ico"" height=""50""/>

_**WARNING:** The library's soruce code is not for the faint of heart. It is ported legacy-code from an older project._<br/>
_**WARNING:** This markdown document has been autogenerated by one of the worst C# programs I've ever written ..... so use it at your own risk..._

<hr/>

### Defined effects:
The See# Library has a few ({effects.Count()}) pre-defined bitmap effects.
The following list contains all pre-defined effects and (in most cases) a generated image rendered with the respective bitmap effect.
The test images used are the following two:

<table>
    <tbody>
        <tr>
            <th>emma.png</th>
            <th>hsv_map.png</th>
        </tr>
        <tr>
            <td><img src=""{save(bmp_s)}"" height=""100""/></td>
            <td><img src=""{save(bmp2_s)}"" height=""100""/></td>
        </tr>
    </tbody>
</table>

Effect list:
<ul>");

                #endregion
                #region LIST EFFECTS

                Type fx_opac = asm.GetType("SeeSharp.Effects.OpacityBitmapEffect");
                string[] ignore = { "ZoomEffect", "RotateEffect" };

                foreach ((object fx, Type type) in effects)
                {
                    sb.AppendLine($@"<li><a name=""{type.Name}""/><b><code>{type.Name}</code></b>");

                    bool insta = fx_inst.IsAssignableFrom(type);

                    if (!ignore.Contains(type.Name))
                    {
                        #region MATRIX CONVOLUTION EFFECTS

                        if (fx_conv.IsAssignableFrom(type))
                        {
                            double[,] hmat = fx_conv.GetProperty("HorizontalMatrix").GetValue(fx) as double[,];
                            double[,] vmat = fx_conv.GetProperty("VerticalMatrix").GetValue(fx) as double[,];

                            sb.AppendLine($@"   <br/>The effect <code>{type.FullName}</code> uses two <a href=""https://en.wikipedia.org/wiki/Convolution"">convolution matrices</a> to calculate the resulting image:<br/>
<table>
    <tbody>
        <tr>
            <th>Vertical convolution matrix</th>
            <th>Horizontal convolution matrix</th>
        </tr>
        <tr>
            <td>
                {printmatrix(vmat)}
            </td>
            <td>
                {printmatrix(hmat)}
            </td>
        </tr>
    </tbody>
</table>
");
                        }
                        else if (fx_matr.IsAssignableFrom(type) && type.Name != "ED88BitmapEffect")
                            sb.AppendLine($@"   <br/>The effect <code>{type.FullName}</code> uses a single<a href=""https://en.wikipedia.org/wiki/Convolution"">convolution matrix</a> to calculate the resulting image:<br/>")
                              .AppendLine(printmatrix(fx_matr.GetProperty("Matrix").GetValue(fx) as double[,]));

                        #endregion
                        #region TINT

                        if (type.Name == "TintBitmapEffect")
                        {
                            sb.AppendLine(@"   <br/>Effect applied to ...<br/>
<table>
    <tbody>
        <tr>");
                            for (int i = 0; i < 10; i++)
                            {
                                if (i == 0)
                                    sb.AppendLine(@"
            <th>0</th>
            <th>π/4</th>
            <th>π/2</th>
            <th>3π/4</th>
            <th>π</th>
        </tr>
        <tr>");
                                else if (i == 5)
                                    sb.AppendLine(@"
        </tr>
        <tr>
            <th>5π/4</th>
            <th>3π/2</th>
            <th>7π/4</th>
            <th>2π</th>
            <th></th>
        </tr>
        <tr>");
                                if (i < 9)
                                {
                                    double v = i / 4d * Math.PI;

                                    sb.AppendLine($@"<td><img src=""{render(false, type == fx_opac ? 1 - v : v)}"" height=""90""/></td>");
                                }
                                else
                                    sb.AppendLine("<td></td>");
                            }

                            sb.AppendLine(@"</tr></tbody></table>");
                        }
                        #endregion
                        #region 'NORMAL' EFFECTS
                        else if (fx_matr.IsAssignableFrom(type) || fx_transf.IsAssignableFrom(type) || insta)
                            sb.AppendLine($@"   <br/>{(insta ? "This is a bitmap effect ported from Instagram's CSS code<br/>" : "")}<img src=""{render()}"" height=""200""/>");
                        #endregion
                        #region 0..100% EFFECTS
                        else if (fx_scol.IsAssignableFrom(type))
                        {
                            sb.AppendLine(@"   <br/>Effect applied to ...<br/>
<table>
    <tbody>
        <tr>
            <th>0%</th>
            <th>25%</th>
            <th>50%</th>
            <th>75%</th>
            <th>100%</th>
        </tr>
        <tr>");
                            for (int i = 0; i < 5; i++)
                            {
                                double v = i / 4d;

                                sb.Append($@"<td><img src=""{render(false, type == fx_opac ? 1 - v : v)}"" height=""90""/></td>");
                            }

                            sb.AppendLine(@"</tr></tbody></table>");
                        }
                        #endregion
                        #region COLOR BLENDING EFFECTS
                        else if (fx_blendc.IsAssignableFrom(type))
                            sb.AppendLine("   <br/>This effect is identical to it's bitmap blending counterpart -- except for the fact, that it does not blend two bitmaps, but one bitmap and a color.")
                              .AppendLine($@"   <br/><a href=""#{type.Name.Replace("Color", "Bitmap")}"">Click here</a> to jump to the corresponding bitmap blending effect.");
                        #endregion
                        #region BITMAP BLENDING EFFECTS
                        else if (fx_blendb.IsAssignableFrom(type))
                        {
                            Bitmap b1 = blend(bmp, bmp2);
                            Bitmap b2 = blend(bmp2, bmp);

                            sb.AppendLine($@"   <br/>
<table>
    <tbody>
        <tr>
            <th><code>Blend(emma, hsv_map)</code></th>
            <th><code>Blend(hsv_map, emma)</code></th>
        </tr>
        <tr>
            <td>
                <img src=""{save(b1)}"" height=""200""/>
            </td>
            <td>
                <img src=""{save(b2)}"" height=""200""/>
            </td>
        </tr>
    </tbody>
</table>");
                        }
                        #endregion
                    }

                    sb.AppendLine("</li>");

                    Bitmap blend(params object[] param) => fx_blendb.GetMethod("Blend").Invoke(Activator.CreateInstance(type), param) as Bitmap;
                    string render(bool big = true, params object[] param) =>
                        save(fx_base.GetMethod("Apply").Invoke((param ?? new object[0]).Length > 0 ? Activator.CreateInstance(type, param) : fx,
                                                               new object[] { big ? bmp : bmp_s }) as Bitmap);
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

        private static string printmatrix(double[,] matr)
        {
            var lines = from row in Enumerable.Range(0, matr.GetLength(0))
                        select $"<tr>{string.Join("", from col in Enumerable.Range(0, matr.GetLength(1)) select $"<td>{matr[row, col].RoundToSignificantDigits(3)}</td>")}</tr>";

            return $"<table><tbody>{string.Join("", lines)}</tbody></table>";
        }

        private static double RoundToSignificantDigits(this double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
    }
}
