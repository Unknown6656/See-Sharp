using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System;

using static System.Math;

namespace SeeSharp
{
    using Effects;

    /// <summary>
    /// Represents an abstract bitmap effect
    /// </summary>
    public abstract class BitmapEffect
    {
        /// <summary>
        /// Applies the current effect to the given bitmap and returns the result
        /// </summary>
        /// <param name="bmp">Input bitmap</param>
        /// <returns>Output bitmap</returns>
        public abstract Bitmap Apply(Bitmap bmp);
    }

    /// <summary>
    /// Represents an abstract bitmap effect wich applies a stored color matrix to each of the bitmap's pixel
    /// </summary>
    public interface IColorEffect
    {
        /// <summary>
        /// Color matrix to be applied to the bitmap
        /// </summary>
        double[,] ColorMatrix { get; }
    }

    /// <summary>
    /// Represents an abstract bitmap effect wich applies a stored transformation matrix to each of the bitmap's pixel
    /// </summary>
    public interface ITransformEffect
    {
        /// <summary>
        /// Transformation matrix to be applied to the bitmap
        /// </summary>
        double[,] TransformMatrix { get; }
    }

    /// <summary>
    /// Represents an abstract bitmap effect which applies the underlying algorithm only to a specific rage of the bitmap
    /// </summary>
    public abstract class RangeEffect
        : BitmapEffect
    {
        /// <summary>
        /// The range, to which the effect should be applied (a null-value applies the effect to the entire image)
        /// </summary>
        public Rectangle? Range { set; get; }
    }

    /// <summary>
    /// Represents an Instagram-photo effect, which has been ported from the instagramm service's CSS code
    /// </summary>
    public abstract class InstagramEffect
        : RangeEffect
    {
    }

    /// <summary>
    /// Contains all basic functions needed to apply effects to bitmaps
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public static unsafe partial class BitmapEffectFunctions
    {
        internal static double Normalize(this double val) => val.Constrain(0, 1);

        internal static double Constrain(this double val, double min, double max) => Min(Max(val, min), max);

        /// <summary>
        /// Applies the given bitmap effect to a given range/section of the given bitmap
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect)
            where T : RangeEffect, new() => ApplyEffect(bmp, new T() { Range = rect });

        /// <summary>
        /// Applies the given parameterized bitmap effect to a given range/section of the given bitmap
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, params object[] args)
            where T : RangeEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return ApplyEffect(bmp, instance);
        }

        /// <summary>
        /// Applies the given bitmap effect to a given range/section of the given bitmap to a certain amount and interpolates the result with the input bitmap
        /// </summary>
        /// <param name="effect">Bitmap effect</param>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <param name="amount">Amount to which the effect shall be applied (1 = fully ... 0 = do not apply)</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap PartialApplyEffectRange(this Bitmap bmp, BitmapEffect effect, Rectangle? rect, double amount)
        {
            double a = amount.Normalize();

            if (a == 0)
                return bmp;

            Bitmap tmp = bmp.ApplyEffectRange(effect, rect);
            
            return a == 1 ? tmp : Merge(bmp, tmp.ApplyEffectRange<BitmapColorEffect>(rect, new double[5, 5]
            {
                { 1,0,0,0,0 },
                { 0,1,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,0,0 },
                { a,a,a,0,0 }
            }), false, false);
        }

        /// <summary>
        /// Applies the given bitmap effect to a given range/section of the given bitmap to a certain amount and interpolates the result with the input bitmap
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <param name="amount">Amount to which the effect shall be applied (1 = fully ... 0 = do not apply)</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap PartialApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, double amount)
            where T : RangeEffect, new() => bmp.PartialApplyEffect(new T() { Range = rect }, amount);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <param name="amount">Amount to which the effect shall be applied (1 = fully ... 0 = do not apply)</param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap PartialApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, double amount, params object[] args)
            where T : RangeEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return bmp.PartialApplyEffect(instance, amount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="effect">Bitmap effect</param>
        /// <param name="amount">Amount to which the effect shall be applied (1 = fully ... 0 = do not apply)</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap PartialApplyEffect(this Bitmap bmp, BitmapEffect effect, double amount)
        {
            Bitmap tmp = bmp.ApplyEffect(effect);
            double a = amount.Normalize();

            return Merge(bmp, tmp.ApplyEffect<BitmapColorEffect>(new double[5, 5]
            {
                { 1,0,0,0,0 },
                { 0,1,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,0,0 },
                { a,a,a,0,0 }
            }), false, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="amount"></param>
        /// <returns>Result bitmap</returns>
        public static Bitmap PartialApplyEffect<T>(this Bitmap bmp, double amount)
            where T : BitmapEffect, new() => bmp.PartialApplyEffect(new T(), amount);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp">Bitmap, to which the effect shall be (partially) applied</param>
        /// <param name="amount"></param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap PartialApplyEffect<T>(this Bitmap bmp, double amount, params object[] args)
            where T : BitmapEffect => bmp.PartialApplyEffect(Activator.CreateInstance(typeof(T), args) as T, amount);

        /// <summary>
        /// Applies the given bitmap blending effect to the two given bitmaps
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyBlendEffect<T>(this Bitmap bmp1, Bitmap bmp2)
            where T : BitmapBlendEffect, new() => new T().Blend(bmp1, bmp2);

        /// <summary>
        /// Applies the given bitmap blending effect to the two given bitmaps
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyBlendEffect<T>(this Bitmap bmp1, Bitmap bmp2, params object[] args)
            where T : BitmapBlendEffect => (Activator.CreateInstance(typeof(T), args) as T).Blend(bmp1, bmp2);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp"></param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyEffect<T>(this Bitmap bmp)
            where T : BitmapEffect, new() => ApplyEffect(bmp, new T());

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp"></param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyEffect<T>(this Bitmap bmp, params object[] args)
            where T : BitmapEffect => ApplyEffect(bmp, (T)Activator.CreateInstance(typeof(T), args));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="effect">Bitmap effect</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyEffect(this Bitmap bmp, BitmapEffect effect) => effect.Apply(bmp);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="effect">Bitmap effect</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyEffectRange(this Bitmap bmp, BitmapEffect effect, Rectangle? rect)
        {
            if (effect is RangeEffect fx)
                fx.Range = rect;

            return effect.Apply(bmp);
        }

        /// <summary>
        /// Applies the given bitmap blending effect to a given range/section of the two given bitmaps
        /// </summary>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="effect">Bitmap effect</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyBlendEffectRange(this Bitmap bmp1, Bitmap bmp2, BitmapBlendEffect effect, Rectangle? rect = null)
        {
            if (rect != null)
                effect.Range = rect;

            return effect.Blend(bmp1, bmp2);
        }

        /// <summary>
        /// Applies the given bitmap blending effect to a given range/section of the two given bitmaps
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <returns>Result bitmap</returns>
        public static Bitmap ApplyBlendEffectRange<T>(this Bitmap bmp1, Bitmap bmp2, Rectangle? rect)
            where T : BitmapBlendEffect, new() => new T() { Range = rect }.Blend(bmp1, bmp2);

        /// <summary>
        /// Applies the given parameterized bitmap blending effect to a given range/section of the two given bitmaps
        /// </summary>
        /// <typeparam name="T">Bitmap effect</typeparam>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="rect">Region, in which the effect shall be applied (a null-value applies the effect to the entire bitmap)</param>
        /// <param name="args">Effect initialization parameters</param>
        /// <returns>Result bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyBlendEffectRange<T>(this Bitmap bmp1, Bitmap bmp2, Rectangle? rect, params object[] args)
            where T : BitmapBlendEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return instance.Blend(bmp1, bmp2);
        }

        /// <summary>
        /// Merges the given two bitmaps by averaging each pixel's color information (even the α-channel)
        /// </summary>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="bmp1fac"></param>
        /// <returns>Merged bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Average(this Bitmap bmp1, Bitmap bmp2, double bmp1fac)
        {
            if ((bmp1.Width != bmp2.Width) || (bmp1.Height != bmp2.Height) || (bmp1.PixelFormat != bmp2.PixelFormat))
                throw new ArgumentException("Both bitmaps must have identical dimensions and pixel depth.", "bmp1,bmp2");

            Bitmap dest = new Bitmap(bmp1.Width, bmp1.Height, bmp1.PixelFormat);
            double val, f = bmp1fac.Normalize(), n = 1.0 - f;

            BitmapLockInfo nfo1 = bmp1.LockBitmap();
            BitmapLockInfo nfo2 = bmp2.LockBitmap();
            BitmapLockInfo nfod = dest.LockBitmap();

            int psz = nfod.DAT.Stride / nfod.DAT.Width;

            fixed (byte* ptr1 = nfo1.ARR)
            fixed (byte* ptr2 = nfo2.ARR)
            fixed (byte* ptrd = nfod.ARR)
                for (int i = 0, l = nfo1.ARR.Length, j; i < l; i++)
                {
                    val = (f * ptr1[i]) + (n * ptr2[i]);

                    ptrd[i] = (byte)(val < 0 ? 0 : val > 255 ? 255 : val);
                }

            nfod.Unlock();
            nfo1.Unlock();
            nfo2.Unlock();

            return dest;
        }

        /// <summary>
        /// Merges the given two bitmaps by averaging each pixel's color information (even the α-channel)
        /// </summary>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <returns>Merged bitmap</returns>
        public static Bitmap Merge(this Bitmap bmp1, Bitmap bmp2) => bmp1.Merge(bmp2, true, true);

        /// <summary>
        /// Merges the given two bitmaps by interpolating each pixel's color information
        /// </summary>
        /// <param name="bmp1">First bitmap</param>
        /// <param name="bmp2">Second bitmap</param>
        /// <param name="α">Decides, whether the alpha-channel should also be interpolated</param>
        /// <param name="avg">Decides, whether the pixel information shall be averaged instead of being added</param>
        /// <returns>Merged bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Merge(this Bitmap bmp1, Bitmap bmp2, bool α, bool avg)
        {
            if ((bmp1.Width != bmp2.Width) || (bmp1.Height != bmp2.Height) || (bmp1.PixelFormat != bmp2.PixelFormat))
                throw new ArgumentException("Both bitmaps must have identical dimensions and pixel depth.", "bmp1,bmp2");

            Bitmap dest = new Bitmap(bmp1.Width, bmp1.Height, bmp1.PixelFormat);
            double val, fac = avg ? .5 : 1;

            BitmapLockInfo nfo1 = bmp1.LockBitmap();
            BitmapLockInfo nfo2 = bmp2.LockBitmap();
            BitmapLockInfo nfod = dest.LockBitmap();

            int psz = nfod.DAT.Stride / nfod.DAT.Width;

            fixed (byte* ptr1 = nfo1.ARR)
            fixed (byte* ptr2 = nfo2.ARR)
            fixed (byte* ptrd = nfod.ARR)
                for (int i = 0, l = nfo1.ARR.Length, j; i < l; i += psz)
                    for (j = 0; j < psz; j++)
                    {
                        val = (fac * ptr1[i + j]) + (fac * ptr2[i + j]);

                        ptrd[i + j] = (byte)val.Constrain(0, 255);

                        if (j > 2)
                            ptrd[i + j] = (byte)(α ? val.Constrain(0, 255) : ptr1[i + j]);
                    }

            nfod.Unlock();
            nfo1.Unlock();
            nfo2.Unlock();

            return dest;
        }

        /// <summary>
        /// Unlocks the bitmap from the given lock information and returns it
        /// </summary>
        /// <param name="nfo">Bitmap lock information</param>
        /// <returns>Unlocked bitmap</returns>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Unlock(this BitmapLockInfo nfo)
        {
            Marshal.Copy(nfo.ARR, 0, nfo.DAT.Scan0, nfo.ARR.Length);

            nfo.BMP.UnlockBits(nfo.DAT);

            return nfo.BMP;
        }

        /// <summary>
        /// Locks the given bitmap and returns the corresponding locking structure
        /// </summary>
        /// <param name="bmp">Bitmap to be locked</param>
        /// <returns>Bimap lock information</returns>
        public static BitmapLockInfo LockBitmap(this Bitmap bmp)
        {
            BitmapData dat = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte[] arr = new byte[dat.Height * dat.Stride];

            Marshal.Copy(dat.Scan0, arr, 0, arr.Length);

            return new BitmapLockInfo()
            {
                ARR = arr,
                BMP = bmp,
                DAT = dat,
                PTR = dat.Scan0,
                PXF = dat.PixelFormat
            };
        }

        /// <summary>
        /// Converts the given bitmap to an 32-Bit ARGB (alpha, red, green and blue) bitmap
        /// </summary>
        /// <param name="bmp">Input bitmap (any pixel format)</param>
        /// <returns>32-Bit bitmap</returns>
        public static Bitmap ToARGB32(this Bitmap bmp)
        {
            Bitmap res = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(res))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="mask"></param>
        /// <param name="tolerance"></param>
        /// <param name="grayscale"></param>
        /// <param name="subtracttolerance"></param>
        /// <param name="ignorealpha"></param>
        /// <returns></returns>
        public static Bitmap DifferenceMask(this Bitmap src, Bitmap mask, double tolerance = 0, bool grayscale = false, bool subtracttolerance = false, bool ignorealpha = false)
        {
            tolerance = tolerance.Normalize() * 255;

            if (grayscale)
            {
                src = src.ToARGB32().ApplyEffect<GrayscaleBitmapEffect>();
                mask = mask.ToARGB32().ApplyEffect<GrayscaleBitmapEffect>();
            }

            Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            BitmapLockInfo srcd = src.LockBitmap();
            BitmapLockInfo mskd = mask.LockBitmap();
            BitmapLockInfo dstd = dst.LockBitmap();

            double diff;
            fixed (byte* srcptr = srcd.ARR)
            fixed (byte* mskptr = mskd.ARR)
            fixed (byte* dstptr = dstd.ARR)
                for (int i = 0, l = srcd.ARR.Length; i < l; i++)
                {
                    diff = Abs(srcptr[i] - mskptr[i]);

                    if (diff >= tolerance)
                    {
                        if (subtracttolerance)
                        {
                            diff = srcptr[i] - diff;

                            dstptr[i] = (byte)(diff < 0 ? 0 : diff > 255 ? 255 : diff);
                        }
                        else
                            dstptr[i] = srcptr[i];
                    }

                    if (ignorealpha && (i % 4) == 3)
                        dstptr[i] = 0xff;
                }

            srcd.Unlock();
            mskd.Unlock();

            return dstd.Unlock();
        }

        /// <summary>
        /// Converts the given RGB-color to a HSL-color
        /// </summary>
        /// <param name="r">The RGB-color's red channel</param>
        /// <param name="g">The RGB-color's green channel</param>
        /// <param name="b">The RGB-color's blue channel</param>
        /// <param name="h">The HSL-color's hue channel</param>
        /// <param name="s">The HSL-color's saturation channel</param>
        /// <param name="l">The HSL-color's luminosity channel</param>
        public static void RGBtoHSL(byte r, byte g, byte b, out double h, out double s, out double l)
        {
            double _R = r / 255d;
            double _G = g / 255d;
            double _B = b / 255d;

            double α = Min(Min(_R, _G), _B);
            double β = Max(Max(_R, _G), _B);
            double δ = β - α;

            l = (β + α) / 2.0;

            if (δ != 0)
            {
                s = δ / (l < 0.5f ? β + α : 2.0f - β - α);
                h = (_R == β ? _G - _B : _G == β ? 2 + _B - _R : 4 + _R - _G) / δ;
            }
            else
                s = h = 0;
        }

        /// <summary>
        /// Converts the given HSL-color to a RGB-color
        /// </summary>
        /// <param name="r">The RGB-color's red channel</param>
        /// <param name="g">The RGB-color's green channel</param>
        /// <param name="b">The RGB-color's blue channel</param>
        /// <param name="h">The HSL-color's hue channel</param>
        /// <param name="s">The HSL-color's saturation channel</param>
        /// <param name="l">The HSL-color's luminosity channel</param>
        public static void HSLtoRGB(double h, double s, double l, out byte r, out byte g, out byte b)
        {
            if (s == 0)
                r = g = b = (byte)Round(l * 255);
            else
            {
                double t2 = l < .5 ? l * (1 + s) : (l + s) - (l * s);
                double t1 = (2 * l) - t2;
                double th = h / 6.0;
                double tr = th + (1d / 3);
                double tb = th - (1d / 3);
                double tg;

                tr = __clrcalc(tr, t1, t2) * 255;
                tg = __clrcalc(th, t1, t2) * 255;
                tb = __clrcalc(tb, t1, t2) * 255;

                r = (byte)tr.Constrain(0, 255);
                g = (byte)tg.Constrain(0, 255);
                b = (byte)tb.Constrain(0, 255);
            }
        }

        // SHIT .... WHAT DOES THIS DO AGAIN .... ?
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double __clrcalc(double c, double t1, double t2)
        {
            c += 1d;
            c %= 1d;

            if (6 * c < 1)
                return t1 + ((t2 - t1) * 6 * c);
            if (2 * c < 1)
                return t2;
            if (3 * c < 2)
                return t1 + ((t2 - t1) * ((2d / 3) - c) * 6);
            return t1;
        }
    }

    /// <summary>
    /// Represents a dataset containing valuable information about a locked unmanaged bitmap
    /// </summary>
    [DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe sealed class BitmapLockInfo
        : IDisposable
    {
        /// <summary>
        /// The bitmap's pixel format
        /// </summary>
        public PixelFormat PXF { internal set; get; }
        /// <summary>
        /// The bitmap's internal data structure
        /// </summary>
        public BitmapData DAT { internal set; get; }
        /// <summary>
        /// The (locked) bitmap
        /// </summary>
        public Bitmap BMP { internal set; get; }
        /// <summary>
        /// A memory pointer pointing to the first of the bitmap's pixel
        /// </summary>
        public IntPtr PTR { internal set; get; }
        /// <summary>
        /// A raw byte array containing all of the bitmap's color information
        /// </summary>
        public byte[] ARR { internal set; get; }

        /// <summary>
        /// Locks the given bitmap and returns the corresponding locking structure
        /// </summary>
        /// <param name="bmp">Bitmap to be locked</param>
        /// <returns>Bimap lock information</returns>
        public static BitmapLockInfo Lock(Bitmap bmp) => BitmapEffectFunctions.LockBitmap(bmp);

        /// <summary>
        /// Unlocks the underlying bitmap and returns it
        /// </summary>
        /// <returns>(Now unlocked) bitmap</returns>
        public Bitmap Unlock() => BitmapEffectFunctions.Unlock(this);

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose()
        {
            try
            {
                BitmapEffectFunctions.Unlock(this);
            }
            catch
            {
            }

            this.ARR = null;
            this.DAT = null;
            this.PTR = IntPtr.Zero;
            this.BMP?.Dispose();
            this.BMP = null;
        }

        /// <summary>
        /// Converts the given bitmap locking structure to an unmanaged pixel pointer
        /// </summary>
        /// <param name="nfo">Bitmap locking structure</param>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pixel* (BitmapLockInfo nfo) => (Pixel*)(byte*)nfo;

        /// <summary>
        /// Converts the given bitmap locking structure to an unmanaged byte pointer
        /// </summary>
        /// <param name="nfo">Bitmap locking structure</param>
        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte* (BitmapLockInfo nfo)
        {
            if (nfo == null)
                return (byte*)0;
            else
                fixed (byte* ptr = nfo.ARR)
                    return ptr;
        }
    }

    /// <summary>
    /// Represents a native pixel 32-bit color information structure
    /// </summary>
    [NativeCppClass, Serializable, StructLayout(LayoutKind.Explicit)]
    public unsafe struct Pixel
    {
        /// <summary>
        /// The pixel's blue channel
        /// </summary>
        [FieldOffset(0)]
        public byte B;
        /// <summary>
        /// The pixel's green channel
        /// </summary>
        [FieldOffset(1)]
        public byte G;
        /// <summary>
        /// The pixel's red channel
        /// </summary>
        [FieldOffset(2)]
        public byte R;
        /// <summary>
        /// The pixel's alpha channel
        /// </summary>
        [FieldOffset(3)]
        public byte A;

        /// <summary>
        /// The color information stored in an 32-Bit signed integer value
        /// </summary>
        public int ARGB
        {
            set => this.ARGBu = (uint)value;
            get => (int)this.ARGBu;
        }
        /// <summary>
        /// The color information stored in an 32-Bit unsigned integer value
        /// </summary>
        public uint ARGBu
        {
            set
            {
                A = (byte)((value >> 24) & 0xff);
                R = (byte)((value >> 16) & 0xff);
                G = (byte)((value >> 8) & 0xff);
                B = (byte)(value & 0xff);
            }
            get => (uint)((A << 24)
                        | (R << 16)
                        | (G << 8)
                        | B);
        }
        /// <summary>
        /// The pixel's alpha channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Af
        {
            set => this.A = (byte)(value.Normalize() * 255);
            get => this.A / 255.0;
        }
        /// <summary>
        /// The pixel's red channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Rf
        {
            set => this.R = (byte)(value.Normalize() * 255);
            get => this.R / 255.0;
        }
        /// <summary>
        /// The pixel's green channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Gf
        {
            set => this.G = (byte)(value.Normalize() * 255);
            get => this.G / 255.0;
        }
        /// <summary>
        /// The pixel's blue channel represented as floating-point value in the interval of [0..1]
        /// </summary>
        public double Bf
        {
            set => this.B = (byte)(value.Normalize() * 255);
            get => this.B / 255.0;
        }
        /// <summary>
        /// An unmanaged structure pointer pointing to the current instance
        /// </summary>
        public Pixel* Pointer
        {
            get
            {
                fixed (Pixel* p = &this)
                    return p;
            }
        }
        /// <summary>
        /// An unmanaged byte pointer pointing to the current instance
        /// </summary>
        public byte* BytePointer => (byte*)this.Pointer;
        /// <summary>
        /// An unmanaged void pointer pointing to the current instance
        /// </summary>
        public void* VoidPointer => (void*)this.Pointer;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        public Pixel(byte r, byte g, byte b)
            : this(255, r, g, b)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="α">Alpha value</param>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        public Pixel(byte α, byte r, byte g, byte b)
        {
            this.A = α;
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }
}
