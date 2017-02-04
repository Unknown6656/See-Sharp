// #define USE_HSL_BRIGHTNESS
// #define USE_HSL_SATURATION

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Linq;
using System;

using static System.Math;

namespace SeeSharp
{
    using Effects;

    namespace Effects
    {
        #region BASIC COLOR EFFECTS

        /// <summary>
        /// Represents a grayscale bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class GrayscaleBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public GrayscaleBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public GrayscaleBitmapEffect(double amount)
            {
                double i = amount.Normalize() / 3.0;
                double a = 1 - (i * 2);

                this.ColorMatrix = new double[5, 5] {
                    { a, i, i, 0, 0, },
                    { i, a, i, 0, 0, },
                    { i, i, a, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { 1, 1, 1, 1, 1, },
                };
            }
        }

        /// <summary>
        /// Represents an opacity bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class OpacityBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public OpacityBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public OpacityBitmapEffect(double amount)
            {
                this.ColorMatrix = new double[5, 5] {
                    { 1, 0, 0, 0, 0, },
                    { 0, 1, 0, 0, 0, },
                    { 0, 0, 1, 0, 0, },
                    { 0, 0, 0, amount.Normalize(), 0, },
                    { 1, 1, 1, 1, 1, },
                };
            }
        }

        /// <summary>
        /// Represents an inversion bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class InvertBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public InvertBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public InvertBitmapEffect(double amount)
            {
                double a = amount.Normalize();

                this.ColorMatrix = new double[5, 5] {
                    { 1 - a, 0, a, 0, 0, },
                    { 0, 1, 0, 0, 0, },
                    { a, 0, 1 - a, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { 1, 1, 1, 1, 1, },
                };
            }
        }

        /// <summary>
        /// Represents a brightness bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class BrightnessBitmapEffect
    #if USE_HSL_BRIGHTNESS
            : IBitmapEffect
    #else
            : BitmapColorEffect
    #endif
        {
            internal double Amount;
#if USE_HSL_BRIGHTNESS
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
                double h, l, s, a = Amount;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                    for (int i = 0, _l = src.ARR.Length, psz = src.DAT.Stride / src.DAT.Width; i < _l; i += psz)
                    {
                        BitmapEffectFunctions.RGBtoHSL(srcptr[i + 2], srcptr[i + 1], srcptr[i], out h, out s, out l);
                        BitmapEffectFunctions.HSLtoRGB(h, s, l * a, out dstptr[i + 2], out dstptr[i + 1], out dstptr[i]);

                        if (psz > 3)
                            dstptr[i + 3] = srcptr[i + 3];
                    }

                src.Unlock();

                return dst.Unlock();
            }
#endif
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public BrightnessBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public BrightnessBitmapEffect(double amount)
            {
                double a = Amount = amount < 0 ? 0 : amount;
    #if !USE_HSL_BRIGHTNESS
                this.ColorMatrix = new double[5, 5] {
                    { 1, 0, 0, 0, 0, },
                    { 0, 1, 0, 0, 0, },
                    { 0, 0, 1, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { a, a, a, 1, 1, },
                };
    #endif
            }
        }

        /// <summary>
        /// Represents a saturation bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class SaturationBitmapEffect
            : RangeEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public double Amount { internal set; get; }

            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                double a = Amount;
    #if !USE_HSL_SATURATION
                double r = (1 - a) * .3086;
                double g = (1 - a) * .6094;
                double b = (1 - a) * .0820;

                return bmp.ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                    { b + a, b, b, 0, 0, },
                    { g, g + a, g, 0, 0, },
                    { r, r, r + a, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { 1, 1, 1, 1, 1, },
                });
    #else
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
                double h, l, s;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                    for (int i = 0, _l = src.ARR.Length, psz = src.DAT.Stride / src.DAT.Width; i < _l; i += psz)
                    {
                        BitmapEffectFunctions.RGBtoHSL(srcptr[i + 2], srcptr[i + 1], srcptr[i], out h, out s, out l);
                        BitmapEffectFunctions.HSLtoRGB(h, s * a, l, out dstptr[i + 2], out dstptr[i + 1], out dstptr[i]);

                        if (psz > 3)
                            dstptr[i + 3] = srcptr[i + 3];
                    }

                src.Unlock();

                return dst.Unlock();
    #endif
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SaturationBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public SaturationBitmapEffect(double amount) => this.Amount = amount < 0 ? 0 : amount;
        }

        /// <summary>
        /// Represents a contrast bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class ContrastBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public ContrastBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public ContrastBitmapEffect(double amount)
            {
                double c = amount < 0 ? 0 : amount;
                double t = (2 - c) / 2.0;

                this.ColorMatrix = new double[5, 5] {
                    { c, 0, 0, 0, 0, },
                    { 0, c, 0, 0, 0, },
                    { 0, 0, c, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { t, t, t, 1, 1, },
                };
            }
        }

        /// <summary>
        /// Represents a sepia bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class SepiaBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SepiaBitmapEffect()
                : this(1.0)
            {
            }

            /// <summary>
            /// Creates a new instance, which applies the current effect to the given amount
            /// </summary>
            /// <param name="amount">Amount [0..1]</param>
            public SepiaBitmapEffect(double amount)
            {
                double a = amount.Normalize();
                double b = ((1 - .131) * (1 - a)) + .131;
                double g = ((1 - .686) * (1 - a)) + .686;
                double r = ((1 - .393) * (1 - a)) + .393;

                this.ColorMatrix = new double[5, 5] {
                    { b, a * .534, a * .272, 0, 0, },
                    { a * .168, g, a * .349, 0, 0, },
                    { a * .189, a * .769, r, 0, 0, },
                    { 0, 0, 0, 1, 0, },
                    { 1, 1, 1, 1, 1, },
                };
            }
        }

        /// <summary>
        /// Represents an overlay bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class OverlayBitmapEffect
            : BitmapColorEffect
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="color"></param>
            public OverlayBitmapEffect(Color color)
                : this(color, 1.0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="color"></param>
            /// <param name="amount"></param>
            public OverlayBitmapEffect(Color color, double amount)
                : this(color.A / 255.0, color.R / 255.0, color.G / 255.0, color.B / 255.0, 1.0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public OverlayBitmapEffect(byte r, byte g, byte b)
                : this(1.0, r / 255.0, g / 255.0, b / 255.0, 1.0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            /// <param name="amount"></param>
            public OverlayBitmapEffect(byte r, byte g, byte b, double amount)
                : this(1.0, r / 255.0, g / 255.0, b / 255.0, amount)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            /// <param name="amount"></param>
            public OverlayBitmapEffect(byte a, byte r, byte g, byte b, byte amount)
                : this(a / 255.0, r / 255.0, g / 255.0, b / 255.0, amount)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public OverlayBitmapEffect(double r, double g, double b)
                : this(1.0, r, g, b, 1.0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            /// <param name="amount"></param>
            public OverlayBitmapEffect(double r, double g, double b, double amount)
                : this(1.0, r, g, b, amount)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            /// <param name="amount"></param>
            public OverlayBitmapEffect(double a, double r, double g, double b, double amount)
                : this(a.Normalize(), r.Normalize(), g.Normalize(), b.Normalize(), amount.Normalize(), 1 - amount.Normalize())
            {
            }

            private OverlayBitmapEffect(double a, double r, double g, double b, double d, double i)
                : base(new double[5, 5] {
                    { 1,0,0,0,b, },
                    { 0,1,0,0,g, },
                    { 0,0,1,0,r, },
                    { 0,0,0,1,a, },
                    { i,i,i,i,d, },
                })
            {
            }
        }

        /// <summary>
        /// Represents a tint bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class TintBitmapEffect
            : RangeEffect
        {
            /// <summary>
            /// The tint degree [0..2π]
            /// </summary>
            public double Degree { internal set; get; }
            /// <summary>
            /// The tint amount
            /// </summary>
            public double Amount { internal set; get; }

            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
                Func<int, int, bool> inrange;
                int w = src.DAT.Width;

                if (Range == null)
                    inrange = (xx, yy) => true;
                else
                {
                    int rcx = Range.Value.X;
                    int rcy = Range.Value.Y;
                    int rch = Range.Value.Bottom;
                    int rcw = Range.Value.Right;

                    inrange = (xx, yy) => (xx >= rcx) && (xx < rcw) && (yy >= rcy) && (yy < rch);
                }

                double ry, gy, by, oy, ryy, gyy, byy, or, og, ob, r, g, b;
                double θ = Degree % (PI * 2);
                double δ = this.Amount;
                double β = 1 - δ;

                int S = (int)(256 * Sin(θ));
                int C = (int)(256 * Cos(θ));

                byte* sptr = (byte*)src.DAT.Scan0;

                fixed (byte* dptr = dst.ARR)
                    for (int y = 0, h = src.DAT.Height, t = src.DAT.Stride, psz = t / w, x, ndx; y < h; y++)
                        for (x = 0; x < w; x++)
                            if (inrange(x, y))
                            {
                                ndx = (y * t) + (x * psz);

                                or = r = sptr[ndx + 2];
                                og = g = sptr[ndx + 1];
                                ob = b = sptr[ndx];

                                ry = ((70 * r) - (59 * g) - (11 * b)) / 100d;
                                gy = ((-30 * r) + (41 * g) - (11 * b)) / 100d;
                                by = ((-30 * r) - (59 * g) + (89 * b)) / 100d;
                                oy = ((30 * r) + (59 * g) + (11 * b)) / 100d;

                                ryy = ((S * by) + (C * ry)) / 256d;
                                byy = ((C * by) - (S * ry)) / 256d;
                                gyy = ((-51 * ryy) - (19 * byy)) / 100d;

                                r = ((ryy + oy) * δ) + (or * β);
                                g = ((gyy + oy) * δ) + (og * β);
                                b = ((byy + oy) * δ) + (ob * β);

                                dptr[ndx + 2] = (byte)(r < 0 ? 0 : r > 255 ? 255 : r);
                                dptr[ndx + 1] = (byte)(g < 0 ? 0 : g > 255 ? 255 : g);
                                dptr[ndx] = (byte)(b < 0 ? 0 : b > 255 ? 255 : b);

                                if (psz > 3)
                                    dptr[ndx + 3] = sptr[ndx + 3];
                            }
                            else
                            {
                                ndx = (y * t) + (x * psz);

                                for (int n = 0; n < psz; n++)
                                    dptr[ndx + n] = sptr[ndx + n];
                            }

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public TintBitmapEffect()
                : this(-PI, 1)
            {
            }

            /// <summary>
            /// Creates a new instance with the given tinting degree
            /// </summary>
            /// <param name="degree">The tint degree [0..2π]</param>
            public TintBitmapEffect(double degree)
                : this(degree, 1)
            {
            }

            /// <summary>
            /// Creates a new instance with the given tinting degree and amount
            /// </summary>
            /// <param name="degree">The tint degree [0..2π]</param>
            /// <param name="amount">The tint amount</param>
            public TintBitmapEffect(double degree, double amount)
            {
                this.Amount = amount;
                this.Degree = degree;
            }
        }

        /// <summary>
        /// Represents a smiple glow bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class SimpleGlowBitmapEffect
            : RangeEffect
        {
            /// <summary>
            /// 
            /// </summary>
            public double Radius { internal set; get; }
            /// <summary>
            /// The glow radius
            /// </summary>
            public double Amount { internal set; get; }
            
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                bmp = bmp.ToARGB32();

                return bmp.ApplyEffectRange<FastBlurBitmapEffect>(Range, Radius)
                          .ApplyBlendEffectRange<AddBitmapBlendEffect>(bmp, Range)
                          .ApplyEffectRange<SaturationBitmapEffect>(Range, 1 + (.075 * Amount))
                          .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1 - (.075 * Amount))
                          .Average(bmp, Amount);
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SimpleGlowBitmapEffect()
                : this(5)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            public SimpleGlowBitmapEffect(double radius)
                : this(radius, 1)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            /// <param name="amount"></param>
            public SimpleGlowBitmapEffect(double radius, double amount)
            {
                this.Radius = radius < 0 ? 0 : radius;
                this.Amount = amount.Normalize();
            }
        }

        #endregion
        #region PORTED INSTAGRAM CSS COLOR FILTERS

        /// <summary>
        /// Represents the Instagram 'Nashville' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class NashvilleBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// The tint amount (between -1 and +1)
            /// </summary>
            public double Tint { internal set; get; }

            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ToARGB32()
                                                           .ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                                                               { 1,0,0,0,-.50, },
                                                               { 0,1,0,0,-.69, },
                                                               { 0,0,1,0,-.96, },
                                                               { 0,0,0,1,0, },
                                                               { 1,1,1,1,.3, },
                                                           })
                                                           .ApplyEffectRange<SepiaBitmapEffect>(Range, .2)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, 1.2)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.4)
                                                           .ApplyEffectRange<TintBitmapEffect>(Range, (360 + (Tint * 40)) * PI / 180)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.1)
                                                           .ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                                                               { 1,0,0,0,.59, },
                                                               { 0,1,0,0,.27, },
                                                               { 0,0,1,0,.10, },
                                                               { 0,0,0,1,0, },
                                                               { 1,1,1,1,.2, },
                                                           })
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.4);
            // darken rgba(247, 176, 153, .56);
            // sepia(.2) contrast(1.2) brightness(1.05) saturate(1.2);
            // lighten rgba(0, 70, 150, .4);

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public NashvilleBitmapEffect()
                : this(0)
            {
            }

            /// <summary>
            /// Creates a new instance with the given tint amount
            /// </summary>
            /// <param name="tint">The tint amount (between -1 and +1)</param>
            public NashvilleBitmapEffect(double tint) => this.Tint = tint.Constrain(-1, 1);
        }

        /// <summary>
        /// Represents the Instagram additive 'Nashville' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class AdditiveNashvilleBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ToARGB32()
                                                           .ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                                                               { 1,0,0,0,-.50, },
                                                               { 0,1,0,0,-.69, },
                                                               { 0,0,1,0,-.96, },
                                                               { 0,0,0,1,0, },
                                                               { 1,1,1,1,.3, },
                                                           })
                                                           .ApplyEffectRange<SepiaBitmapEffect>(Range, .2)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, 1.2)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.5)
                                                           .ApplyEffectRange<TintBitmapEffect>(Range, 320 * PI / 180)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.2)
                                                           .ApplyEffectRange<OverlayBitmapEffect>(Range, 1, .1, .27, .59, .4)
                                                           .ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                                                               { 1.5,0,0,0,0, },
                                                               { 0,1.5,0,0,0, },
                                                               { 0,0,1.5,0,0, },
                                                               { 0,0,0,1,10, },
                                                               { 1.4,1.4,1.4,1,1, },
                                                           });
            // darken rgba(247, 176, 153, .56);
            // lighten rgba(0, 70, 150, .4);
            // mix more with solid 0/70/150/255 ---> maybe up to 30% lighter color?
        }

        /// <summary>
        /// Represents the Instagram 'Aden' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class AdenBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ToARGB32()
                                                           .ApplyEffectRange<TintBitmapEffect>(Range, -PI / 9)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, .9)
                                                           .PartialApplyEffectRange<SaturationBitmapEffect>(Range, .4, .85)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.5)
                                                           .ApplyEffectRange<BitmapColorEffect>(Range, new double[5, 5] {
                                                               { 1,0,0,0,-.055, },
                                                               { 0,1,0,0,-.039, },
                                                               { 0,0,1,0,-.26, },
                                                               { 0,0,0,1,0, },
                                                               { 1,1,1,1,.1, },
                                                           });
            // hue-rotate(-20deg) contrast(.9) saturate(.85) brightness(1.2);
            // rgba(66, 10, 14, .2) darken
        }

        /// <summary>
        /// Represents the Instagram 'Inkwell' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class InkwellBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ToARGB32()
                                                           .ApplyEffectRange<SepiaBitmapEffect>(Range, .3)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, 1.2)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.4)
                                                           .ApplyEffectRange<GrayscaleBitmapEffect>(Range, .9);
            // sepia(.3) contrast(1.1) brightness(1.1) grayscale(1)
        }

        /// <summary>
        /// Represents the Instagram 'Reyes' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class ReyesBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ToARGB32()
                                                           .ApplyEffectRange<SepiaBitmapEffect>(Range, .22)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.1)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, .85)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, .75);
            // sepia(.22) brightness(1.1) contrast(.85) saturate(.75);
        }

        /// <summary>
        /// Represents the Instagram smooth 'Walden' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class SmoothWaldenBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.1)
                                                           .ApplyEffectRange<TintBitmapEffect>(Range, -PI)
                                                           .ApplyEffectRange<SepiaBitmapEffect>(Range, .3)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.6)
                                                           .ApplyEffectRange<ScreenBlendColorEffect>(Range, 0, .075, .225) // (0,¼,¾) * .3
                                                           .Average(bmp, .3);
            // -webkit-filter: brightness(1.1) hue-rotate(-10deg) sepia(.3) saturate(1.6);
            // screen #04c .3
        }

        /// <summary>
        /// Represents the Instagram 'Walden' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class WaldenBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ApplyEffectRange<SepiaBitmapEffect>(Range, .35)
                                                           .ApplyEffectRange<ContrastBitmapEffect>(Range, 1.2)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.1)
                                                           .ApplyEffectRange<TintBitmapEffect>(Range, PI / 18.0)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.5)
                                                           .ApplyEffectRange<ScreenBlendColorEffect>(Range, .1, .1, .4) // (0,¼,¾) * .3
                                                           .ApplyEffectRange<ScreenBlendColorEffect>(Range, 0, .4, 1)
                                                           .Average(bmp, .2);
            // sepia(0.35) contrast(0.9) brightness(1.1) hue-rotate(-10deg) saturate(1.5);
        }

        /// <summary>
        /// Represents the Instagram 'LoFi' CSS bitmap effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class LoFiBitmapEffect
            : InstagramEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp) => bmp.ApplyEffectRange<ContrastBitmapEffect>(Range, 1.5)
                                                           .ApplyEffectRange<BrightnessBitmapEffect>(Range, 1.7)
                                                           .ApplyEffectRange<SaturationBitmapEffect>(Range, 1.1)
                                                           .ApplyEffectRange<ScreenBlendColorEffect>(Range, .81 * .05, .75 * .05, .29 * .05);
            // sepia(0.35) contrast(0.9) brightness(1.1) hue-rotate(-10deg) saturate(1.5);
        }

        #endregion
        #region COLOR BLEND EFFECTS

        /// <summary>
        /// Represents a screen (inverse multiplicative) bitmap blend effect
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class ScreenBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v = 0;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = 1.0 - ((1.0 - matr[o]) * (1.0 - (srcptr[i] / 255.0)));
                            v *= 255;

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public ScreenBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public ScreenBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public ScreenBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public ScreenBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class MultiplyBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v = 0;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = matr[o] * srcptr[i];

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public MultiplyBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public MultiplyBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public MultiplyBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public MultiplyBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class DivideBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v = 0;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = matr[o] == 0 ? 0 : srcptr[i] / (matr[o] * 255d);

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public DivideBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public DivideBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DivideBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DivideBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class OverlayBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v, ival;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            ival = srcptr[i] / 255d;

                            if (ival < .5)
                                v = matr[o] * 2 * ival;
                            else
                                v = 1 - (2d * (1 - ival) * (1 - matr[o]));

                            v *= 255d;

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public OverlayBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public OverlayBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public OverlayBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public OverlayBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class HardLightBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v, ival;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            ival = srcptr[i] / 255d;

                            if (matr[o] < .5)
                                v = matr[o] * 2 * ival;
                            else
                                v = 1 - (2d * (1 - ival) * (1 - matr[o]));

                            v *= 255d;

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public HardLightBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public HardLightBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public HardLightBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public HardLightBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class SoftLightBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v, ival;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            ival = srcptr[i] / 255d;

                            v = (((1 - (2 * matr[o])) * ival) + (2 * matr[o])) * ival * 255d;

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SoftLightBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public SoftLightBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public SoftLightBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public SoftLightBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class AddBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = (matr[o] * 255d) + srcptr[i];

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public AddBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public AddBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public AddBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public AddBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class SubtractBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = srcptr[i] - (matr[o] * 255d);

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SubtractBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public SubtractBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public SubtractBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public SubtractBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class DifferenceBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = Abs(srcptr[i] - (matr[o] * 255d));

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public DifferenceBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public DifferenceBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DifferenceBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DifferenceBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class DarkerOnlyBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = Min(srcptr[i], matr[o] * 255d);

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public DarkerOnlyBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public DarkerOnlyBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DarkerOnlyBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public DarkerOnlyBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed unsafe class LighterOnlyBlendColorEffect
            : BlendColorEffect
        {
            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;
                Func<int, bool> inrange = __inrange1(psz, t);
                double v;

                fixed (byte* srcptr = src.ARR)
                fixed (byte* dstptr = dst.ARR)
                fixed (double* matr = refcolor)
                    for (int i = 0, o, l = src.ARR.Length; i < l; i++)
                        if (inrange(i))
                        {
                            o = i % psz;
                            v = Max(srcptr[i], matr[o] * 255d);

                            dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                        }
                        else
                            dstptr[i] = srcptr[i];

                src.Unlock();

                return dst.Unlock();
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public LighterOnlyBlendColorEffect()
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clr"></param>
            public LighterOnlyBlendColorEffect(Color clr)
                : base(clr)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public LighterOnlyBlendColorEffect(double r, double g, double b)
                : base(0, r, g, b)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="a"></param>
            /// <param name="r"></param>
            /// <param name="g"></param>
            /// <param name="b"></param>
            public LighterOnlyBlendColorEffect(double a, double r, double g, double b)
                : base(a, r, g, b)
            {
            }
        }

        #endregion
        #region BLEND EFFECTS

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public unsafe sealed class LighterBitmapBlendEffect
            : BitmapBlendEffect
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="src1"></param>
            /// <param name="src2"></param>
            /// <param name="dst"></param>
            /// <param name="inrange"></param>
            protected internal override void __blend(ref BitmapLockInfo src1, ref BitmapLockInfo src2, ref BitmapLockInfo dst, Func<int, int, bool> inrange)
            {
                int t = src1.DAT.Stride;
                int h = src1.DAT.Height;
                int w = src1.DAT.Width;
                int psz = t / w, i, ndx;

                if (psz < 3)
                    throw new ArgumentException("The bitmap must have a minimum pixel depth of 24 Bits.", "bmp");

                fixed (byte* dptr = dst.ARR)
                fixed (byte* sptr1 = src1.ARR)
                fixed (byte* sptr2 = src2.ARR)
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            ndx = (y * t) + (x * psz);

                            if (inrange(x, y))
                                for (i = 0; i < psz; i++)
                                    dptr[ndx + i] = Max(sptr1[ndx + i], sptr2[ndx + i]);
                            else
                                for (i = 0; i < psz; i++)
                                    dptr[ndx + i] = sptr1[ndx + i];
                        }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public unsafe sealed class AddBitmapBlendEffect
            : BitmapBlendEffect
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="src1"></param>
            /// <param name="src2"></param>
            /// <param name="dst"></param>
            /// <param name="inrange"></param>
            protected internal override void __blend(ref BitmapLockInfo src1, ref BitmapLockInfo src2, ref BitmapLockInfo dst, Func<int, int, bool> inrange)
            {
                int t = src1.DAT.Stride;
                int h = src1.DAT.Height;
                int w = src1.DAT.Width;
                int psz = t / w, i, ndx, v;

                fixed (byte* dptr = dst.ARR)
                fixed (byte* sptr1 = src1.ARR)
                fixed (byte* sptr2 = src2.ARR)
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            ndx = (y * t) + (x * psz);

                            if (inrange(x, y))
                                for (i = 0; i < psz; i++)
                                {
                                    v = sptr1[ndx + i] + sptr2[ndx + i];

                                    dptr[ndx + i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                                }
                            else
                                for (i = 0; i < psz; i++)
                                    dptr[ndx + i] = sptr1[ndx + i];
                        }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public unsafe sealed class MultiplyBitmapBlendEffect
            : BitmapBlendEffect
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="src1"></param>
            /// <param name="src2"></param>
            /// <param name="dst"></param>
            /// <param name="inrange"></param>
            protected internal override void __blend(ref BitmapLockInfo src1, ref BitmapLockInfo src2, ref BitmapLockInfo dst, Func<int, int, bool> inrange)
            {
                int t = src1.DAT.Stride;
                int h = src1.DAT.Height;
                int w = src1.DAT.Width;
                int psz = t / w, i, ndx;
                double v;

                fixed (byte* dptr = dst.ARR)
                fixed (byte* sptr1 = src1.ARR)
                fixed (byte* sptr2 = src2.ARR)
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            ndx = (y * t) + (x * psz);

                            if (inrange(x, y))
                                for (i = 0; i < psz; i++)
                                {
                                    v = sptr1[ndx + i] / 255d * sptr2[ndx + i];

                                    dptr[ndx + i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                                }
                            else
                                for (i = 0; i < psz; i++)
                                    dptr[ndx + i] = sptr1[ndx + i];
                        }
            }
        }

        #endregion
        #region DOUBLE MATRIX CONVOLUTION EFFECTS

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class SobelBitmapEffect
            : MatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SobelBitmapEffect()
                : this(false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public SobelBitmapEffect(bool grayscale)
                : base(new double[3, 3] {
                    { -1, 0, 1 },
                    { -2, 0, 2 },
                    { -1, 0, 1 },
                }, new double[3, 3] {
                    { 1, 2, 1 },
                    { 0, 0, 0 },
                    { -1, -2, -1 },
                }, grayscale)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class PrewittBitmapEffect
            : MatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public PrewittBitmapEffect()
                : this(false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public PrewittBitmapEffect(bool grayscale)
                : base(new double[3, 3] {
                    { -1, 0, 1 },
                    { -1, 0, 1 },
                    { -1, 0, 1 },
                }, new double[3, 3] {
                    { 1, 1, 1 },
                    { 0, 0, 0 },
                    { -1, -1, -1 },
                }, grayscale)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class ScharrBitmapEffect
            : MatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public ScharrBitmapEffect()
                : this(false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public ScharrBitmapEffect(bool grayscale)
                : base(new double[3, 3] {
                    { 3, 10, 3 },
                    { 0, 0, 0 },
                    { -3, -10, -3 },
                }, new double[3, 3] {
                    { 3, 0, -3 },
                    { 10, 0, -10 },
                    { 3, 0, -3 },
                }, grayscale)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class KirschBitmapEffect
            : MatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public KirschBitmapEffect()
                : this(1, false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public KirschBitmapEffect(bool grayscale)
                : this(1, grayscale)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="amount"></param>
            public KirschBitmapEffect(double amount)
                : this(amount, false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="amount"></param>
            /// <param name="grayscale"></param>
            public KirschBitmapEffect(double amount, bool grayscale)
                : base(null, null, grayscale)
            {
                double a = amount.Normalize();

                this.HorizontalMatrix = new double[3, 3] {
                    { a * 5, a * 5, a * 5, },
                    { a * -3, 1 - a, a * -3, },
                    { a * -3, a * -3, a * -3, },
                };
                this.VerticalMatrix = new double[3, 3] {
                    { a * 5, a * -3, a * -3, },
                    { a * 5, 1 - a, a * -3, },
                    { a * 5, a * -3, a * -3, },
                };
            }
        }

        #endregion
        #region SINGLE MATRIX CONVOLUTION EFFECTS

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class SharpenerBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public SharpenerBitmapEffect()
                : this(3d)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            public unsafe SharpenerBitmapEffect(double radius)
                : base(null, 1, 0, false)
            {
                if (radius < 0)
                    radius = 0;

                int r = (int)radius;

                Matrix = new double[(r * 2) + 1, (r * 2) + 1];

                for (int i = 0, l = Matrix.Length, w = Matrix.GetLength(0); i < l; i++)
                    Matrix[i / w, i % w] = -1d / l;

                Matrix[r, r] += 2d;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class FastBlurBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public FastBlurBitmapEffect()
                : this(3d)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            public unsafe FastBlurBitmapEffect(double radius)
                : base(null, 1, 0, false)
            {
                if (radius < 0)
                    radius = 0;

                Matrix = new double[((int)radius * 2) + 1, ((int)radius * 2) + 1];

                for (int i = 0, l = Matrix.Length, w = Matrix.GetLength(0); i < l; i++)
                    Matrix[i / w, i % w] = 1d / l;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class FastSharpenerBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public FastSharpenerBitmapEffect()
                : this(3d)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            public unsafe FastSharpenerBitmapEffect(double radius)
                : base(null, 1, 0, false)
            {
                if (radius < 0)
                    radius = 0;

                int sum = 1;
                int r = r = (int)radius;

                Matrix = new double[(r * 2) + 1, (r * 2) + 1];

                for (int i = 0, l = Matrix.GetLength(0); i < l; i++)
                {
                    if (i == r)
                        continue;

                    Matrix[i, r] =
                    Matrix[r, i] = -1;

                    sum += 2;
                }

                Matrix[r, r] = sum;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class EdgeDetectionBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public EdgeDetectionBitmapEffect()
                : base(new double[3, 3] {
                    { -1,-1,-1, },
                    { -1,8,-1, },
                    { -1,-1,-1, },
                }, 1, 0, false)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class EmbossBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public EmbossBitmapEffect()
                : base(new double[3, 3] {
                    { -2,-1,0, },
                    { -1,1,1, },
                    { 0,1,2, },
                }, 1, 0, false)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class EngraveBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public EngraveBitmapEffect()
                : base(new double[3, 3] {
                    { -2,0,0, },
                    { 0,2,0, },
                    { 0,0,0, },
                }, 1, 95, false)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class Laplace5x5BitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public Laplace5x5BitmapEffect()
                : base(new double[5, 5] {
                    { -1, -1, -1, -1, -1, },
                    { -1, -1, -1, -1, -1, },
                    { -1, -1, 24, -1, -1, },
                    { -1, -1, -1, -1, -1, },
                    { -1, -1, -1, -1, -1, },
                }, 1, 0, false)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class Gaussian5x5BitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public Gaussian5x5BitmapEffect()
                : base(new double[5, 5] {
                    { 1, 4, 6, 4, 1, },
                    { 4, 16, 24, 16, 4, },
                    { 6, 24, 36, 24, 6, },
                    { 4, 16, 24, 16, 4, },
                    { 1, 4, 6, 4, 1, },
                }, 1 / 256.0, 0, false)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class GaussianBlurBitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public GaussianBlurBitmapEffect()
                : this(5)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="radius"></param>
            public GaussianBlurBitmapEffect(double radius)
                : base(null, 1, 0, false)
            {
                int size = ((int)radius * 2) + 1;
                int r = size / 2;

                double[,] matrix = new double[size, size];
                double w = Pow(radius, 4);
                double φ = 1.0 / (PI * w);
                double s = 0;
                double d = 0;

                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                    {
                        d = ((x * x) + (y * y)) / w * 16;

                        s += matrix[y + r, x + r] = φ * Exp(-d);
                    }

                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        matrix[y, x] *= 1.0 / s;

                this.Matrix = matrix;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public sealed class ED88BitmapEffect
            : SingleMatrixConvolutionBitmapEffect
        {
            /// <summary>
            /// Creates a new instance
            /// </summary>
            public ED88BitmapEffect()
                : this(false)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public ED88BitmapEffect(bool grayscale)
                : base(new double[5, 5] {
                    { 1,0,-2,-1,1, },
                    { -1,0,-1,0,0, },
                    { -2,-1,.5,1,2, },
                    { 0,0,1,0,1, },
                    { -1,1,2,0,-1, },
                }, 1, 0, grayscale)
            {
            }
        }

        #endregion
        #region OTHER EFFECTS

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public unsafe sealed class NormalMapBitmapEffect
            : BitmapEffect
        {
            /// <summary>
            /// 
            /// </summary>
            public bool Grayscale { internal set; get; }
            /// <summary>
            /// 
            /// </summary>
            public double BlurRadius { internal set; get; }
            /// <summary>
            /// 
            /// </summary>
            public NormalFilter Filter { internal set; get; }

            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                if (Grayscale)
                    bmp = new GrayscaleBitmapEffect().Apply(bmp);

                BitmapLockInfo src = bmp.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

                int w = src.DAT.Width, h = src.DAT.Height, s = src.DAT.Stride, l = s * h, psz = s / w;
                double[] hmat = new double[9] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
                double[] vmat = new double[9] { 1, 2, 1, 0, 0, 0, -1, -2, -1 };

                if (Filter == NormalFilter.Scharr)
                {
                    hmat = new double[9] { -1, 0, 1, -1, 0, 1, -1, 0, 1 };
                    vmat = new double[9] { 1, 1, 1, 0, 0, 0, -1, -1, -1 };
                }
                else if (Filter == NormalFilter.Prewitt)
                {
                    hmat = new double[9] { 3, 10, 3, 0, 0, 0, -3, -10, -3 };
                    vmat = new double[9] { 3, 0, -3, 10, 0, -10, 3, 0, -3 };
                }

                double sx, sy, sz, sum;
                int so, to, nx, ox, oy, fx, fy;

                byte* sptr = (byte*)src.DAT.Scan0;

                fixed (double* vptr = vmat)
                fixed (double* hptr = hmat)
                fixed (byte* dptr = dst.ARR)
                    for (oy = 0; oy < h; oy++)
                        for (ox = 0; ox < w; ox++)
                        {
                            to = (oy * s) + (ox * psz);
                            sx = sy = 128;

                            for (fy = -1; fy <= 1; fy++)
                                for (fx = -1; fx <= 1; fx++)
                                {
                                    so = to + (fx * psz) + (fy * s);

                                    if (so < 0 || so >= l - 3)
                                        continue;

                                    sum = (0.0 + sptr[so] + sptr[so + 1] + sptr[so + 2]) / 3.0;
                                    nx = ((fy + 1) * 3) + fx + 1;

                                    sx += hptr[nx] * sum;
                                    sy += vptr[nx] * sum;
                                }

                            sz = ((Abs(sx - 128.0) + Abs(sy - 128.0)) / 4.0);

                            dptr[to + 0] = (byte)(sz > 64 ? 191 : sz < 0 ? 255 : 255 - sz);
                            dptr[to + 1] = (byte)(sy > 255 ? 255 : sy < 0 ? 0 : sy);
                            dptr[to + 2] = (byte)(sx > 255 ? 255 : sx < 0 ? 0 : sx);

                            if (psz > 3)
                                dptr[to + 3] = 255;
                        }

                src.Unlock();

                bmp = dst.Unlock();

                if ((BlurRadius < 0 ? 0 : BlurRadius) > 0)
                    bmp = bmp.ApplyEffect<FastBlurBitmapEffect>(BlurRadius)
                             .ApplyEffect<FastBlurBitmapEffect>(BlurRadius / 2);

                return bmp;
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public NormalMapBitmapEffect()
                : this(false, 0, NormalFilter.Sobel)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            public NormalMapBitmapEffect(bool grayscale)
                : this(grayscale, 0, NormalFilter.Sobel)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            /// <param name="radius"></param>
            public NormalMapBitmapEffect(bool grayscale, double radius)
                : this(grayscale, radius, NormalFilter.Sobel)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="grayscale"></param>
            /// <param name="radius"></param>
            /// <param name="filter"></param>
            public NormalMapBitmapEffect(bool grayscale, double radius, NormalFilter filter)
            {
                this.Filter = filter;
                this.BlurRadius = radius;
                this.Grayscale = grayscale;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
        public unsafe sealed partial class RGBSplitBitmapEffect
            : BitmapEffect
        {
            /// <summary>
            /// 
            /// </summary>
            public double Direction { internal set; get; }
            /// <summary>
            /// 
            /// </summary>
            public double Amount { internal set; get; }

            /// <summary>
            /// Applies the current effect to the given bitmap and returns the result
            /// </summary>
            /// <param name="bmp">Input bitmap</param>
            /// <returns>Output bitmap</returns>
            public override Bitmap Apply(Bitmap bmp)
            {
                Bitmap bmp1 = bmp.ToARGB32().ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                    { 1,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,1,1 },
                    { 1,1,1,1,1 },
                });
                Bitmap bmp2 = bmp.ToARGB32().ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,1,0,0 },
                    { 0,0,0,1,1 },
                    { 1,1,1,1,1 },
                });
                Bitmap tmp = bmp1.DifferenceMask(bmp2, .05, true, true);

                bmp2 = bmp2.DifferenceMask(bmp1, .05, true, true);
                bmp1 = tmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                    { 1,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,1,1 },
                    { 1,1,1,1,.4 },
                });
                bmp2 = tmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,1,0,0 },
                    { 0,0,0,1,1 },
                    { 1,1,1,1,.4 },
                });

                double x = Amount * Cos(Direction);
                double y = -Amount * Sin(Direction);

                tmp = bmp1.Merge(bmp2);
                bmp = bmp.DifferenceMask(tmp, .05, false, true, true);
                bmp = bmp.ApplyEffect<BitmapColorEffect>(new double[5, 5] {
                    { 1,0,0,0,0 },
                    { 0,1,0,0,0 },
                    { 0,0,1,0,0 },
                    { 0,0,0,1,0 },
                    { 1,1,1,1,0 },
                });

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(bmp1, (float)x, (float)y, bmp.Width, bmp.Height);
                    g.DrawImage(bmp2, -(float)x, -(float)y, bmp.Width, bmp.Height);
                }

                return bmp;
            }

            [Obsolete("Use `Apply` instead.", true)]
            internal Bitmap __OLD__Apply(Bitmap bmp)
            {
                Func<double, double, double, double> MatrixValue = new Func<double, double, double, double>((x, y, θ) =>
                {
                    double fac = 2 * Sqrt(2);
                    double τ = y - (Tan(θ) * x);
                    double nx = τ - x;
                    double ny = (τ * Tan(θ)) - y;
                    double δ = Sqrt((nx * nx) + (ny * ny));
                    double σ = Max(0, fac - δ) / fac;

                    if (Abs(x) < 1 && Abs(y) < 1)
                        return 1;
                    else if (x == 0)
                        return y < 0 ? -σ : σ;
                    else
                        return x < 0 ? -σ : σ;
                });

                int amnt = (int)Amount;
                int size = 1 + (amnt * 2);
                double[,] tmatr = new double[size, size];
                double[,] omatr = new double[size, size];
                double sum = 0;

                for (int x = -amnt; x <= amnt; x++)
                    for (int y = -amnt; y <= amnt; y++)
                        sum += tmatr[amnt + x, amnt + y] = MatrixValue(x + .5, y + .5, Direction);

                Console.WriteLine(sum);
                Console.WriteLine();

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                        Console.Write(tmatr[x, y].ToString("0.00").PadLeft(5, ' ') + "  ");

                    Console.WriteLine();
                }

                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        omatr[size - 1 - x, size - 1 - y] = tmatr[x, y];

                Bitmap bmp1 = bmp.ApplyEffect(new SingleMatrixConvolutionColorBitmapEffect(tmatr, new double[5, 5] {
                    { 1,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,0,1,0 },
                    { 1,1,1,1,0 },
                }, 1.0 / sum, 0, false));
                Bitmap bmp2 = bmp.ApplyEffect(new SingleMatrixConvolutionColorBitmapEffect(omatr, new double[5, 5] {
                    { 0,0,0,0,0 },
                    { 0,0,0,0,0 },
                    { 0,0,1,0,0 },
                    { 0,0,0,1,0 },
                    { 1,1,1,1,0 },
                }, 1.0 / sum, 0, false));

                return bmp1.Merge(bmp2);
            }

            /// <summary>
            /// Creates a new instance
            /// </summary>
            public RGBSplitBitmapEffect()
                : this(1, 0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="amount"></param>
            public RGBSplitBitmapEffect(double amount)
                : this(amount, 0)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="amount"></param>
            /// <param name="direction"></param>
            public RGBSplitBitmapEffect(double amount, double direction)
            {
                const double π2 = PI * 2;

                this.Amount = amount < 0 ? 0 : amount;
                this.Direction = (direction + π2) % π2;
            }
        }

        [Serializable, DebuggerStepThrough, DebuggerNonUserCode, Obsolete("Use `CoreLib::Imaging::RGBSplitBitmapEffect` instead.", true)]
        internal unsafe sealed partial class __OLD__RGBSplitBitmapEffect
            : BitmapEffect
        {
            public double Delta { internal set; get; }
            public double Theta { internal set; get; }

            public unsafe override Bitmap Apply(Bitmap bmp)
            {
                using (BitmapLockInfo nfo = bmp.LockBitmap())
                {
                    double δ = Delta;
                    double θ = Theta;
                    double xo = δ * Sin(θ);
                    double yo = δ * Cos(θ + PI);

                    byte[] dup = new byte[nfo.ARR.Length];

                    Array.Copy(nfo.ARR, dup, nfo.ARR.Length);

                    int w = nfo.BMP.Width;
                    int h = nfo.BMP.Height;

                    fixed (byte* src = dup)
                    fixed (byte* tar = nfo.ARR)
                        for (int y = 0, o; y < h; y++)
                            for (int x = 0; x < w; x++)
                            {
                                o = (x + (y * w)) * 4;

                                int r = src[o + 1];
                                int b = src[o + 3];

                                int xs = (int)Max(x - xo, 0);
                                int ys = (int)Max(y - yo, 0);

                                o = (xs + (ys * w)) * 4;

                                r += (int)(src[o + 1] * .5);

                                xs = (int)Min(xs + (2 * xo), w);
                                ys = (int)Min(ys + (2 * yo), h);

                                o = (xs + (ys * w)) * 4;

                                b += (int)(src[o + 3] * .5);

                                o = (x + (y * w)) * 4;

                                tar[o + 1] = (byte)Min(255, r);
                                tar[o + 3] = (byte)Min(255, b);
                            }

                    return nfo.Unlock();
                }
            }

            public __OLD__RGBSplitBitmapEffect()
                : this(5, 0)
            {
            }

            public __OLD__RGBSplitBitmapEffect(double δ, double θ)
            {
                this.Delta = δ;
                this.Theta = θ;
            }
        }

        #endregion
    }

    #region EFFECT BASE DEFINITIONS

    /// <summary>
    /// 
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe sealed class MatrixConvolutionColorBitmapEffect
        : MatrixConvolutionBitmapEffect
        , IColorEffect
    {
        internal const int MATRIX_SIZE = BitmapColorEffect.MATRIX_SIZE;
        /// <summary>
        /// 
        /// </summary>
        public double[,] ColorMatrix { internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public new void Dispose()
        {
            this.ColorMatrix = null;

            base.Dispose();
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void matrmult(double* matr, double* b, double* g, double* r, double* a)
        {
            double vb = *b, vg = *g, vr = *r, va = *a;

            vb = (matr[0] * vb) + (matr[1] * vg) + (matr[2] * vr) + (matr[3] * va);
            vg = (matr[4] * vb) + (matr[5] * vg) + (matr[6] * vr) + (matr[7] * va);
            vr = (matr[8] * vb) + (matr[9] * vg) + (matr[10] * vr) + (matr[11] * va);
            va = (matr[12] * vb) + (matr[13] * vg) + (matr[14] * vr) + (matr[15] * va);

            vb *= matr[16];
            vg *= matr[17];
            vr *= matr[18];
            va *= matr[19];
        }

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override Bitmap Apply(Bitmap bmp)
        {
            if (Grayscale)
                bmp = new GrayscaleBitmapEffect().Apply(bmp);

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

            int psz = src.DAT.Stride / src.DAT.Width;
            double ax, bx, gx, rx, ay, by, gy, ry, at, bt, gt, rt, vm, hm;
            double[,] hmat = HorizontalMatrix;
            double[,] vmat = VerticalMatrix;

            if (psz < 3)
                throw new ArgumentException("The bitmap must have a minimum pixel depth of 24 Bits.", nameof(bmp));
            if (hmat.GetLength(0) != hmat.GetLength(1))
                throw new InvalidProgramException("The horizontal convolution matrix must be symertical.");
            if (vmat.GetLength(0) != vmat.GetLength(1))
                throw new InvalidProgramException("The vertical convolution matrix must be symertical.");
            if (vmat.GetLength(0) != hmat.GetLength(0))
                throw new InvalidProgramException("The vertical and horizontal convolution matrices must have the same dimensions.");

            int fo = hmat.GetLength(0) / 2;
            int s = src.DAT.Stride;
            int l = s * src.DAT.Height;
            int so, to;

            byte* sptr = (byte*)src.DAT.Scan0;

            fixed (byte* dptr = dst.ARR)
            fixed (double* matr = ColorMatrix)
                for (int oy = 0, h = src.DAT.Height; oy < h; oy++)
                    for (int ox = 0, w = src.DAT.Width; ox < w; ox++)
                    {
                        ax = bx = gx = rx = ay = by = gy = ry = at = bt = gt = rt = 0;
                        to = (oy * s) + (ox * psz);

                        for (int fy = -fo; fy <= fo; fy++)
                            for (int fx = -fo; fx <= fo; fx++)
                            {
                                so = to + (fx * psz) + (fy * s);

                                if (so < 0 || so >= l - 3)
                                    continue;

                                vm = vmat[fy + fo, fx + fo];
                                hm = hmat[fy + fo, fx + fo];

                                bx += sptr[so] * hm;
                                gx += sptr[so + 1] * hm;
                                rx += sptr[so + 2] * hm;

                                if (so + 3 < l)
                                    ax += sptr[so + 3] * hm;

                                by += sptr[so] * vm;
                                gy += sptr[so + 1] * vm;
                                ry += sptr[so + 2] * vm;

                                if (so + 3 < l)
                                    ay += sptr[so + 3] * vm;
                            }

                        bt = Sqrt((bx * bx) + (by * by));
                        gt = Sqrt((gx * gx) + (gy * gy));
                        rt = Sqrt((rx * rx) + (ry * ry));
                        at = Sqrt((ax * ax) + (ay * ay));

                        matrmult(matr, &bt, &gt, &rt, &at);

                        dptr[to + 0] = (byte)(bt > 255 ? 255 : bt < 0 ? 0 : bt);
                        dptr[to + 1] = (byte)(gt > 255 ? 255 : gt < 0 ? 0 : gt);
                        dptr[to + 2] = (byte)(rt > 255 ? 255 : rt < 0 ? 0 : rt);

                        if (psz > 3)
                            dptr[to + 3] = (byte)(at > 255 ? 255 : at < 0 ? 0 : at);
                    }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hmatrix"></param>
        /// <param name="vmatrix"></param>
        /// <param name="cmatrix"></param>
        /// <param name="grayscale"></param>
        public MatrixConvolutionColorBitmapEffect(double[,] hmatrix, double[,] vmatrix, double[,] cmatrix, bool grayscale)
            : base(hmatrix, vmatrix, grayscale) => this.ColorMatrix = cmatrix;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe sealed class SingleMatrixConvolutionColorBitmapEffect
        : SingleMatrixConvolutionBitmapEffect
        , IColorEffect
    {
        internal const int MATRIX_SIZE = BitmapColorEffect.MATRIX_SIZE;
        /// <summary>
        /// 
        /// </summary>
        public double[,] ColorMatrix { internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public new void Dispose()
        {
            this.ColorMatrix = null;

            base.Dispose();
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void matrmult(double* matr, double* b, double* g, double* r, double* a)
        {
            double vb = *b, vg = *g, vr = *r, va = *a;

            vb = (matr[0] * vb) + (matr[1] * vg) + (matr[2] * vr) + (matr[3] * va);
            vg = (matr[4] * vb) + (matr[5] * vg) + (matr[6] * vr) + (matr[7] * va);
            vr = (matr[8] * vb) + (matr[9] * vg) + (matr[10] * vr) + (matr[11] * va);
            va = (matr[12] * vb) + (matr[13] * vg) + (matr[14] * vr) + (matr[15] * va);

            vb *= matr[16];
            vg *= matr[17];
            vr *= matr[18];
            va *= matr[19];
        }

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override Bitmap Apply(Bitmap bmp)
        {
            if (Grayscale)
                bmp = new GrayscaleBitmapEffect().Apply(bmp);

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

            int psz = src.DAT.Stride / src.DAT.Width;
            double r, g, b, a, m, fac, bia;
            double[,] mat = Matrix;

            if (psz < 3)
                throw new ArgumentException("The bitmap must have a minimum pixel depth of 24 Bits.", nameof(bmp));
            if (mat.GetLength(0) != mat.GetLength(1))
                throw new InvalidProgramException("The horizontal convolution matrix must be symertical.");

            int s = src.DAT.Stride;
            int l = s * src.DAT.Height;
            int fw = mat.GetLength(1);
            int fh = mat.GetLength(0);
            int fo = (fw - 1) / 2;
            int so, to, x, y;

            fac = this.Factor;
            bia = this.Bias;

            byte* sptr = (byte*)src.DAT.Scan0;

            fixed (byte* dptr = dst.ARR)
            fixed (double* cmat = ColorMatrix)
                for (int oy = 0, h = src.DAT.Height; oy < h; oy++)
                    for (int ox = 0, w = src.DAT.Width; ox < w; ox++)
                    {
                        b = g = r = a = 0;
                        to = (oy * s) + (ox * psz);

                        for (int fy = -fo; fy <= fo; fy++)
                            for (int fx = -fo; fx <= fo; fx++)
                            {
                                x = ((fx * psz) + s) % s;
                                y = ((fy * s) + h) % h;

                                so = to + x + y;

                                if (so < 0 || so >= l - 3)
                                    continue;

                                m = mat[fy + fo, fx + fo];

                                b += sptr[so] * m;
                                g += sptr[so + 1] * m;
                                r += sptr[so + 2] * m;

                                if (so + 3 < l)
                                    a += sptr[so + 3] * m;
                            }

                        b = (fac * b) + bia;
                        g = (fac * g) + bia;
                        r = (fac * r) + bia;

                        matrmult(cmat, &b, &g, &r, &a);

                        dptr[to + 0] = (byte)(b > 255 ? 255 : b < 0 ? 0 : b);
                        dptr[to + 1] = (byte)(g > 255 ? 255 : g < 0 ? 0 : g);
                        dptr[to + 2] = (byte)(r > 255 ? 255 : r < 0 ? 0 : r);

                        if (psz > 3)
                            dptr[to + 3] = (byte)(a > 255 ? 255 : a < 0 ? 0 : a);
                    }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tmatrix"></param>
        /// <param name="cmatrix"></param>
        /// <param name="factor"></param>
        /// <param name="bias"></param>
        /// <param name="grayscale"></param>
        public SingleMatrixConvolutionColorBitmapEffect(double[,] tmatrix, double[,] cmatrix, double factor, double bias, bool grayscale)
            : base(tmatrix, factor, bias, grayscale) => this.ColorMatrix = cmatrix;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class MatrixConvolutionBitmapEffect
        : RangeEffect
        , IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public double[,] HorizontalMatrix { protected internal set; get; }
        /// <summary>
        /// 
        /// </summary>
        public double[,] VerticalMatrix { protected internal set; get; }
        /// <summary>
        /// 
        /// </summary>
        public bool Grayscale { protected internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose()
        {
            this.HorizontalMatrix =
            this.VerticalMatrix = null;
        }

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override Bitmap Apply(Bitmap bmp)
        {
            if (Grayscale)
                bmp = new GrayscaleBitmapEffect().Apply(bmp);

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
            Func<int, int, bool> inrange;
            int w = src.DAT.Width;

            if (Range == null)
                inrange = (x, y) => true;
            else
            {
                int rcx = Range.Value.X;
                int rcy = Range.Value.Y;
                int rch = Range.Value.Bottom;
                int rcw = Range.Value.Right;

                inrange = (x, y) => (x >= rcx) && (x < rcw) && (y >= rcy) && (y < rch);
            }

            int psz = src.DAT.Stride / w;
            double bx, gx, rx, ax, by, gy, ry, ay, bt, gt, rt, at, vm, hm;
            double[,] hmat = HorizontalMatrix;
            double[,] vmat = VerticalMatrix;

            if (psz < 3)
                throw new ArgumentException("The bitmap must have a minimum pixel depth of 24 Bits.", nameof(bmp));
            if (hmat.GetLength(0) != hmat.GetLength(1))
                throw new InvalidProgramException("The horizontal convolution matrix must be symertical.");
            if (vmat.GetLength(0) != vmat.GetLength(1))
                throw new InvalidProgramException("The vertical convolution matrix must be symertical.");
            if (vmat.GetLength(0) != hmat.GetLength(0))
                throw new InvalidProgramException("The vertical and horizontal convolution matrices must have the same dimensions.");

            int fo = hmat.GetLength(0) / 2;
            int s = src.DAT.Stride;
            int l = s * src.DAT.Height;
            int so, to;

            byte* sptr = (byte*)src.DAT.Scan0;

            fixed (byte* dptr = dst.ARR)
                for (int oy = 0, h = src.DAT.Height, ox; oy < h; oy++)
                    for (ox = 0; ox < w; ox++)
                        if (inrange(ox, oy))
                        {
                            bx = gx = rx = ax = by = gy = ry = ay = bt = gt = rt = at = 0;
                            to = (oy * s) + (ox * psz);

                            for (int fy = -fo; fy <= fo; fy++)
                                for (int fx = -fo; fx <= fo; fx++)
                                {
                                    so = to + (fx * psz) + (fy * s);

                                    if (so < 0 || so >= l - 3)
                                        continue;

                                    vm = vmat[fy + fo, fx + fo];
                                    hm = hmat[fy + fo, fx + fo];

                                    bx += sptr[so] * hm;
                                    gx += sptr[so + 1] * hm;
                                    rx += sptr[so + 2] * hm;

                                    if (so + 3 < l)
                                        ax += sptr[so + 3] * hm;

                                    by += sptr[so] * vm;
                                    gy += sptr[so + 1] * vm;
                                    ry += sptr[so + 2] * vm;

                                    if (so + 3 < l)
                                        ay += sptr[so + 3] * vm;
                                }

                            bt = Sqrt((bx * bx) + (by * by));
                            gt = Sqrt((gx * gx) + (gy * gy));
                            rt = Sqrt((rx * rx) + (ry * ry));
                            at = Sqrt((ax * ax) + (ay * ay));

                            dptr[to + 0] = (byte)(bt > 255 ? 255 : bt < 0 ? 0 : bt);
                            dptr[to + 1] = (byte)(gt > 255 ? 255 : gt < 0 ? 0 : gt);
                            dptr[to + 2] = (byte)(rt > 255 ? 255 : rt < 0 ? 0 : rt);

                            if (psz > 3)
                                dptr[to + 3] = (byte)(at > 255 ? 255 : at < 0 ? 0 : at);
                        }
                        else
                        {
                            to = (oy * s) + (ox * psz);

                            for (int n = 0; n < psz; n++)
                                dptr[to + n] = sptr[to + n];
                        }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="grayscale"></param>
        public MatrixConvolutionBitmapEffect(double[,] matrix, bool grayscale)
            : this(matrix, matrix, grayscale)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hmatrix"></param>
        /// <param name="vmatrix"></param>
        /// <param name="grayscale"></param>
        public MatrixConvolutionBitmapEffect(double[,] hmatrix, double[,] vmatrix, bool grayscale)
        {
            this.HorizontalMatrix = hmatrix;
            this.VerticalMatrix = vmatrix;
            this.Grayscale = grayscale;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable, DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class SingleMatrixConvolutionBitmapEffect
        : RangeEffect
        , IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public double[,] Matrix { protected internal set; get; }
        /// <summary>
        /// 
        /// </summary>
        public bool Grayscale { protected internal set; get; }
        /// <summary>
        /// 
        /// </summary>
        public double Factor { protected internal set; get; }
        /// <summary>
        /// 
        /// </summary>
        public double Bias { protected internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose() => this.Matrix = null;

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override Bitmap Apply(Bitmap bmp)
        {
            if (Grayscale)
                bmp = new GrayscaleBitmapEffect().Apply(bmp);

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
            Func<int, int, bool> inrange;
            int w = src.DAT.Width;

            if (Range == null)
                inrange = (xx, yy) => true;
            else
            {
                int rcx = Range.Value.X;
                int rcy = Range.Value.Y;
                int rch = Range.Value.Bottom;
                int rcw = Range.Value.Right;

                inrange = (xx, yy) => (xx >= rcx) && (xx < rcw) && (yy >= rcy) && (yy < rch);
            }

            int psz = src.DAT.Stride / w;
            double r, g, b, a, m, fac, bia;
            double[,] mat = Matrix;

            if (psz < 3)
                throw new ArgumentException("The bitmap must have a minimum pixel depth of 24 Bits.", nameof(bmp));
            if (mat.GetLength(0) != mat.GetLength(1))
                throw new InvalidProgramException("The horizontal convolution matrix must be symertical.");

            int s = src.DAT.Stride;
            int l = s * src.DAT.Height;
            int fw = mat.GetLength(1);
            int fh = mat.GetLength(0);
            int fo = (fw - 1) / 2;
            int so, to, x, y;

            fac = this.Factor;
            bia = this.Bias;

            fixed (byte* sptr = src.ARR)
            fixed (byte* dptr = dst.ARR)
                for (int oy = 0, h = src.DAT.Height, ox; oy < h; oy++)
                    for (ox = 0; ox < w; ox++)
                        if (inrange(ox, oy))
                        {
                            b = g = r = a = 0;
                            to = (oy * s) + (ox * psz);

                            for (int fy = -fo; fy <= fo; fy++)
                                for (int fx = -fo; fx <= fo; fx++)
                                {
                                    x = (fx * psz) + s;
                                    y = fy * s;

                                    so = to + x + y;

                                    if (so < 0 || so >= l - 3)
                                        continue;

                                    m = mat[fy + fo, fx + fo];

                                    b += sptr[so] * m;
                                    g += sptr[so + 1] * m;
                                    r += sptr[so + 2] * m;

                                    if (psz > 3)
                                        a += sptr[so + 3] * m;
                                }

                            b = (fac * b) + bia;
                            g = (fac * g) + bia;
                            r = (fac * r) + bia;
                            a = (fac * a) + bia;

                            dptr[to + 0] = (byte)(b > 255 ? 255 : b < 0 ? 0 : b);
                            dptr[to + 1] = (byte)(g > 255 ? 255 : g < 0 ? 0 : g);
                            dptr[to + 2] = (byte)(r > 255 ? 255 : r < 0 ? 0 : r);

                            if (psz > 3)
                                dptr[to + 3] = (byte)(a > 255 ? 255 : a < 0 ? 0 : a);
                        }
                        else
                        {
                            to = (oy * s) + (ox * psz);

                            for (int n = 0; n < psz; n++)
                                dptr[to + n] = sptr[to + n];
                        }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="fac"></param>
        /// <param name="bia"></param>
        /// <param name="grayscale"></param>
        public SingleMatrixConvolutionBitmapEffect(double[,] matrix, double fac, double bia, bool grayscale)
        {
            this.Bias = bia;
            this.Factor = fac;
            this.Matrix = matrix;
            this.Grayscale = grayscale;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class BitmapColorEffect
        : RangeEffect
        , IColorEffect
        , IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public const int MATRIX_SIZE = 5;
        /// <summary>
        /// 
        /// </summary>
        public double[,] ColorMatrix { protected internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose() => this.ColorMatrix = null;

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override sealed Bitmap Apply(Bitmap bmp)
        {
            if ((ColorMatrix.GetLength(0) != MATRIX_SIZE) || (ColorMatrix.GetLength(1) != MATRIX_SIZE))
                throw new InvalidProgramException("The field `ColorMatrix : double[,]` defined inside `" + this.GetType() + "` must have the dimension " + MATRIX_SIZE + "x" + MATRIX_SIZE + ".");

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

            Func<int, bool> inrange;
            int w = src.DAT.Width, t = src.DAT.Stride, psz = t / w;

            if (Range == null)
                inrange = i => true;
            else
            {
                int rx = Range.Value.X * psz;
                int ry = Range.Value.Y;
                int rh = Range.Value.Bottom;
                int rw = Range.Value.Right * psz;
                int x, y;

                inrange = i => {
                    x = i % t;
                    y = i / t;

                    return (x >= rx) && (x < rw) && (y >= ry) && (y < rh);
                };
            }

            fixed (byte* srcptr = src.ARR)
            fixed (byte* dstptr = dst.ARR)
            fixed (double* matr = ColorMatrix)
            {
                // box pointer into system::object because of delegate management
                object s = Pointer.Box(srcptr, typeof(byte*));
                object d = Pointer.Box(dstptr, typeof(byte*));
                object m = Pointer.Box(matr, typeof(double*));

                Parallel.For(0, src.ARR.Length, i => {
                    // unbox pointer to their appropriate types
                    byte* _srcptr = (byte*)Pointer.Unbox(s);
                    byte* _dstptr = (byte*)Pointer.Unbox(d);
                    double* _matr = (double*)Pointer.Unbox(m);

                    if (inrange(i))
                    {
                        int o = i % psz;
                        int p = i - o;
                        double v = 0;

                        for (int n = 0; n < psz; n++)
                            v += _matr[(MATRIX_SIZE * o) + n] * _srcptr[p + n];

                        v += _matr[(MATRIX_SIZE * MATRIX_SIZE) - 1] * _matr[(MATRIX_SIZE * o) + MATRIX_SIZE - 1] * 255;
                        v *= _matr[(MATRIX_SIZE * (MATRIX_SIZE - 1)) + o];

                        _dstptr[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                    }
                    else
                        _dstptr[i] = _srcptr[i];
                });
            }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BitmapColorEffect()
            : this(new double[5, 5] {
                { 1,0,0,0,0, },
                { 0,1,0,0,0, },
                { 0,0,1,0,0, },
                { 0,0,0,1,0, },
                { 1,1,1,1,0, },
            })
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public BitmapColorEffect(double[,] matrix) => this.ColorMatrix = matrix;
    }

    /// <summary>
    /// 
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class HSLBitmapColorEffect
        : BitmapEffect
        , IColorEffect
        , IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public const int MATRIX_SIZE = 5;
        /// <summary>
        /// 
        /// </summary>
        public double[,] ColorMatrix { protected internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose() => this.ColorMatrix = null;

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override sealed Bitmap Apply(Bitmap bmp)
        {
            if ((ColorMatrix.GetLength(0) != MATRIX_SIZE) || (ColorMatrix.GetLength(1) != MATRIX_SIZE))
                throw new InvalidProgramException("The field `ColorMatrix : double[,]` defined inside `" + this.GetType() + "` must have the dimension " + MATRIX_SIZE + "x" + MATRIX_SIZE + ".");

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();
            double l, a, oh, os, ol, oa;

            fixed (byte* srcptr = src.ARR)
            fixed (byte* dstptr = dst.ARR)
            fixed (double* matr = ColorMatrix)
                for (int i = 0, _l = src.ARR.Length, psz = src.DAT.Stride / src.DAT.Width; i < _l; i += psz)
                {
                    BitmapEffectFunctions.RGBtoHSL(srcptr[i + 2], srcptr[i + 1], srcptr[i], out double h, out double s, out l);

                    a = psz > 3 ? srcptr[i + 3] / 255d : 1;
                    h /= PI * 2;

                    oh = (matr[0] * h) + (matr[1] * s) + (matr[2] * l) + (matr[3] * a);
                    os = (matr[5] * h) + (matr[6] * s) + (matr[7] * l) + (matr[8] * a);
                    ol = (matr[10] * h) + (matr[11] * s) + (matr[12] * l) + (matr[13] * a);
                    oa = (matr[15] * h) + (matr[16] * s) + (matr[17] * l) + (matr[18] * a);

                    oh *= matr[20] * PI * 2;
                    os *= matr[21];
                    ol *= matr[22];
                    oa *= matr[23];
                    oh += matr[24] * matr[4] * PI * 2;
                    os += matr[24] * matr[9];
                    ol += matr[24] * matr[14];
                    oa += matr[24] * matr[19];

                    BitmapEffectFunctions.HSLtoRGB(oh, os, ol, out dstptr[i + 2], out dstptr[i + 1], out dstptr[i]);

                    if (psz > 3)
                    {
                        a = oa * 255;
                        dstptr[i + 3] = (byte)(a < 0 ? 0 : a > 255 ? 255 : a);
                    }
                }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public HSLBitmapColorEffect()
            : this(new double[5, 5] {
                { 1,0,0,0,0, },
                { 0,1,0,0,0, },
                { 0,0,1,0,0, },
                { 0,0,0,1,0, },
                { 1,1,1,1,0, },
            })
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public HSLBitmapColorEffect(double[,] matrix) => this.ColorMatrix = matrix;
    }

    /// <summary>
    /// 
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class BitmapTransformEffect
        : BitmapEffect
        , ITransformEffect
        , IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public const int MATRIX_SIZE = 2;
        /// <summary>
        /// 
        /// </summary>
        public double[,] TransformMatrix { protected internal set; get; }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose() => this.TransformMatrix = null;

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public override sealed Bitmap Apply(Bitmap bmp)
        {
            if ((TransformMatrix.GetLength(0) != 2) || (TransformMatrix.GetLength(1) != 2))
                throw new InvalidProgramException("The field `TransformMatrix : double[,]` defined inside `" + this.GetType() + "` must have the dimension 2x2.");

            BitmapLockInfo src = bmp.LockBitmap();
            BitmapLockInfo dst = new Bitmap(bmp.Width, bmp.Height, bmp.PixelFormat).LockBitmap();

            double x, y;

            fixed (byte* srcptr = src.ARR)
            fixed (byte* dstptr = dst.ARR)
            fixed (double* matr = TransformMatrix)
                for (int i = 0, l = src.ARR.Length, h = src.DAT.Height, w = src.DAT.Width, t = src.DAT.Stride, psz = t / w; i < l; i += psz)
                {
                    int p = i / psz;

                    double ix = (p % w) - ((double)w / 2.0);
                    double iy = (p / w) - ((double)h / 2.0);

                    x = (matr[0] * ix) + (matr[1] * iy);
                    y = (matr[2] * ix) + (matr[3] * iy);

                    x += (double)w / 2.0;
                    y += (double)h / 2.0;

                    int np = ((int)y * t) + ((int)x * psz);

                    if (np < 0)
                        continue;
                    else if (np > l - psz)
                        continue;

                    for (int n = 0; n < psz; n++)
                        dstptr[np + n] = srcptr[i + n];
                }

            src.Unlock();

            return dst.Unlock();
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BitmapTransformEffect()
            : this(new double[2,2] {
                { 1, 0 },
                { 0, 1 }
            })
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        public BitmapTransformEffect(double[,] matrix) => this.TransformMatrix = matrix;
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe abstract class BitmapBlendEffect
        : RangeEffect
        , IDisposable
    {
        internal Bitmap Second { set; get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src1"></param>
        /// <param name="src2"></param>
        /// <param name="dst"></param>
        /// <param name="inrange"></param>
        protected internal abstract void __blend(ref BitmapLockInfo src1, ref BitmapLockInfo src2, ref BitmapLockInfo dst, Func<int, int, bool> inrange);

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose() => Second?.Dispose();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected Func<int, int, bool> __inrange2()
        {
            if (Range == null)
                return (x, y) => true;
            else
            {
                int rcx = Range.Value.X;
                int rcy = Range.Value.Y;
                int rch = Range.Value.Bottom;
                int rcw = Range.Value.Right;

                return (x, y) => (x >= rcx) && (x < rcw) && (y >= rcy) && (y < rch);
            }
        }

        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public sealed override Bitmap Apply(Bitmap bmp) => Blend(bmp, Second);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp1"></param>
        /// <param name="bmp2"></param>
        /// <returns></returns>
        public virtual Bitmap Blend(Bitmap bmp1, Bitmap bmp2)
        {
            if ((bmp1.Width != bmp2.Width) || (bmp1.Height != bmp2.Height))
                throw new ArgumentException("Both given bitmaps must have identical dimensions.", "bmp1,bmp2");
            else if (bmp1.PixelFormat != bmp2.PixelFormat)
                throw new ArgumentException("Both given bitmaps must have identical pixel formats.", "bmp1,bmp2");
            else
            {
                BitmapLockInfo src1 = bmp1.LockBitmap();
                BitmapLockInfo src2 = bmp2.LockBitmap();
                BitmapLockInfo dst = new Bitmap(bmp1.Width, bmp1.Height, bmp1.PixelFormat).LockBitmap();

                try
                {
                    __blend(ref src1, ref src2, ref dst, __inrange2());
                }
                finally
                {
                    src1.Unlock();
                    src2.Unlock();
                }

                return dst.Unlock();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe abstract class BlendColorEffect
        : RangeEffect
    {
        /// <summary>
        /// 
        /// </summary>
        protected double[] refcolor = new double[4] { 0, 0, 0, 0 };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="psz"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        protected Func<int, bool> __inrange1(int psz, int t)
        {
            if (Range == null)
                return i => true;
            else
            {
                int rx = Range.Value.X * psz;
                int ry = Range.Value.Y;
                int rh = Range.Value.Bottom;
                int rw = Range.Value.Right * psz;
                int x, y;

                return i => {
                    x = i % t;
                    y = i / t;

                    return (x >= rx) && (x < rw) && (y >= ry) && (y < rh);
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected Func<int, int, bool> __inrange2()
        {
            if (Range == null)
                return (x, y) => true;
            else
            {
                int rcx = Range.Value.X;
                int rcy = Range.Value.Y;
                int rch = Range.Value.Bottom;
                int rcw = Range.Value.Right;

                return (x, y) => (x >= rcx) && (x < rcw) && (y >= rcy) && (y < rch);
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BlendColorEffect()
            : this(Color.Black)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clr"></param>
        public BlendColorEffect(Color clr)
            : this(clr.A / 255.0, clr.R / 255.0, clr.G / 255.0, clr.B / 255.0)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public BlendColorEffect(double r, double g, double b)
            : this(0, r, g, b)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public BlendColorEffect(double a, double r, double g, double b) =>
            refcolor = new double[4] { b.Normalize(), g.Normalize(), r.Normalize(), a.Normalize() };
    }

    #endregion

    /// <summary>
    /// An enumeration of edge-finding filters to generate a normal map from a diffuse image
    /// </summary>
    [Serializable]
    public enum NormalFilter
        : byte
    {
        /// <summary>
        /// Sobel filter
        /// </summary>
        Sobel,
        /// <summary>
        /// Scharr filter
        /// </summary>
        Scharr,
        /// <summary>
        /// Prewitt filter
        /// </summary>
        Prewitt,
    }
}

// TODO: BOKEH