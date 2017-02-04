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

    [DebuggerStepThrough, DebuggerNonUserCode]
    public static unsafe partial class BitmapEffectFunctions
    {
        internal static double Normalize(this double val) => val.Constrain(0, 1);

        internal static double Constrain(this double val, double min, double max) => Min(Max(val, min), max);

        public static Bitmap ApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect)
            where T : RangeEffect, new() => ApplyEffect(bmp, new T() { Range = rect });

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, params object[] args)
            where T : RangeEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return ApplyEffect(bmp, instance);
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap PartialApplyEffectRange(this Bitmap bmp, BitmapEffect effect, Rectangle? rect, double amount)
        {
            Bitmap tmp = bmp.ApplyEffectRange(effect, rect);
            double a = amount.Normalize();

            return Merge(bmp, tmp.ApplyEffectRange<BitmapColorEffect>(rect, new double[5, 5]
            {
                { 1,0,0,0,0 },
                { 0,1,0,0,0 },
                { 0,0,1,0,0 },
                { 0,0,0,0,0 },
                { a,a,a,0,0 }
            }), false, false);
        }

        public static Bitmap PartialApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, double amount)
            where T : RangeEffect, new() => bmp.PartialApplyEffect(new T() { Range = rect }, amount);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap PartialApplyEffectRange<T>(this Bitmap bmp, Rectangle? rect, double amount, params object[] args)
            where T : RangeEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return bmp.PartialApplyEffect(instance, amount);
        }

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

        public static Bitmap PartialApplyEffect<T>(this Bitmap bmp, double amount)
            where T : BitmapEffect, new() => bmp.PartialApplyEffect(new T(), amount);

        public static Bitmap PartialApplyEffect<T>(this Bitmap bmp, double amount, params object[] args)
            where T : BitmapEffect => bmp.PartialApplyEffect(Activator.CreateInstance(typeof(T), args) as T, amount);

        public static Bitmap ApplyBlendEffect<T>(this Bitmap bmp1, Bitmap bmp2)
            where T : BitmapBlendEffect, new() => new T().Blend(bmp1, bmp2);

        public static Bitmap ApplyBlendEffect<T>(this Bitmap bmp1, Bitmap bmp2, params object[] args)
            where T : BitmapBlendEffect => (Activator.CreateInstance(typeof(T), args) as T).Blend(bmp1, bmp2);

        public static Bitmap ApplyEffect<T>(this Bitmap bmp)
            where T : BitmapEffect, new() => ApplyEffect(bmp, new T());

        public static Bitmap ApplyEffect<T>(this Bitmap bmp, params object[] args)
            where T : BitmapEffect => ApplyEffect(bmp, (T)Activator.CreateInstance(typeof(T), args));

        public static Bitmap ApplyEffect(this Bitmap bmp, BitmapEffect effect) => effect.Apply(bmp);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyEffectRange(this Bitmap bmp, BitmapEffect effect, Rectangle? rect)
        {
            if (effect is RangeEffect fx)
                fx.Range = rect;

            return effect.Apply(bmp);
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyBlendEffectRange(this Bitmap bmp1, Bitmap bmp2, BitmapBlendEffect effect, Rectangle? rect = null)
        {
            if (rect != null)
                effect.Range = rect;

            return effect.Blend(bmp1, bmp2);
        }

        public static Bitmap ApplyBlendEffectRange<T>(this Bitmap bmp1, Bitmap bmp2, Rectangle? rect)
            where T : BitmapBlendEffect, new() => new T() { Range = rect }.Blend(bmp1, bmp2);

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap ApplyBlendEffectRange<T>(this Bitmap bmp1, Bitmap bmp2, Rectangle? rect, params object[] args)
            where T : BitmapBlendEffect
        {
            T instance = Activator.CreateInstance(typeof(T), args) as T;

            instance.Range = rect;

            return instance.Blend(bmp1, bmp2);
        }

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

        public static Bitmap Merge(this Bitmap bmp1, Bitmap bmp2) => bmp1.Merge(bmp2, true, true);

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

                        ptrd[i + j] = (byte)(val < 0 ? 0 : val > 255 ? 255 : val);

                        if (j > 2)
                            ptrd[i + j] = (byte)(α ? val < 0 ? 0 : val > 255 ? 255 : val : ptr1[i + j]);
                    }

            nfod.Unlock();
            nfo1.Unlock();
            nfo2.Unlock();

            return dest;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bitmap Unlock(this BitmapLockInfo nfo)
        {
            Marshal.Copy(nfo.ARR, 0, nfo.DAT.Scan0, nfo.ARR.Length);

            nfo.BMP.UnlockBits(nfo.DAT);

            return nfo.BMP;
        }

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

        public static Bitmap ToARGB32(this Bitmap bmp)
        {
            Bitmap res = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(res))
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

            return res;
        }

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

    [DebuggerStepThrough, DebuggerNonUserCode]
    public unsafe class BitmapLockInfo
        : IDisposable
    {
        public PixelFormat PXF { internal set; get; }
        public BitmapData DAT { internal set; get; }
        public Bitmap BMP { internal set; get; }
        public IntPtr PTR { internal set; get; }
        public byte[] ARR { internal set; get; }


        public static BitmapLockInfo Lock(Bitmap bmp) => BitmapEffectFunctions.LockBitmap(bmp);

        public Bitmap Unlock() => BitmapEffectFunctions.Unlock(this);

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
            this.BMP.Dispose();
            this.BMP = null;
        }

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pixel* (BitmapLockInfo nfo) => (Pixel*)(byte*)nfo;

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

        public int ARGB
        {
            set => this.ARGBu = (uint)value;
            get => (int)this.ARGBu;
        }

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

        public double Af
        {
            set => this.A = (byte)(value.Normalize() * 255);
            get => this.A / 255.0;
        }

        public double Rf
        {
            set => this.R = (byte)(value.Normalize() * 255);
            get => this.R / 255.0;
        }

        public double Gf
        {
            set => this.G = (byte)(value.Normalize() * 255);
            get => this.G / 255.0;
        }

        public double Bf
        {
            set => this.B = (byte)(value.Normalize() * 255);
            get => this.B / 255.0;
        }

        public Pixel* Pointer
        {
            get
            {
                fixed (Pixel* p = &this)
                    return p;
            }
        }

        public byte* BytePointer => (byte*)this.Pointer;

        public Pixel(byte r, byte g, byte b)
            : this(255, r, g, b)
        {
        }

        public Pixel(byte a, byte r, byte g, byte b)
        {
            this.A = a;
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }
}
