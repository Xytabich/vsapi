﻿using System;
using System.Drawing;

namespace Vintagestory.API.Common
{
    public interface IBitmap
    {
        Color GetPixel(int x, int y);
        Color GetPixelRel(float x, float y);

        int Width { get; }
        int Height { get; }

        int[] Pixels { get; }

        int[] GetPixelsTransformed(int rot = 0, int alpha = 100);
    }

    public class BakedBitmap: IBitmap
    {
        public int[] TexturePixels;
        public int Width;
        public int Height;

        public int[] Pixels
        {
            get
            {
                return TexturePixels;
            }
        }

        int IBitmap.Width => Width;

        int IBitmap.Height => Height;

        public Color GetPixel(int x, int y)
        {
            return Color.FromArgb(TexturePixels[Width * y + x]);
        }
        public int GetPixelArgb(int x, int y)
        {
            return TexturePixels[Width * y + x];
        }

        public Color GetPixelRel(float x, float y)
        {
            return Color.FromArgb(TexturePixels[Width * (int)(y * Height) + (int)(x * Width)]);
        }

        public int[] GetPixelsTransformed(int rot = 0, int alpha = 100)
        {
            int[] bmpPixels = new int[Width * Height];

            // Could be more compact, but this is therefore more efficient
            switch (rot)
            {
                case 0:
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            bmpPixels[x + y * Width] = GetPixelArgb(x, y);
                        }
                    }
                    break;
                case 90:
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            bmpPixels[y + x * Width] = GetPixelArgb(Width - x - 1, y);
                        }
                    }
                    break;
                case 180:
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            bmpPixels[x + y * Width] = GetPixelArgb(Width - x - 1, Height - y - 1);
                        }
                    }
                    break;

                case 270:
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            bmpPixels[y + x * Width] = GetPixelArgb(x, Height - y - 1);
                        }
                    }
                    break;
            }

            if (alpha != 100)
            {
                float af = alpha / 100f;
                int clearAlpha = ~(0xff << 24);
                for (int i = 0; i < bmpPixels.Length; i++)
                {
                    int col = bmpPixels[i];
                    int a = (col >> 24) & 0xff;
                    col &= clearAlpha;

                    bmpPixels[i] = col | ((int)(a * af) << 24);
                }
            }

            return bmpPixels;
        }
    }

    public abstract class BitmapRef : IDisposable, IBitmap
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int[] Pixels { get; }

        public abstract void Dispose();
        public abstract Color GetPixel(int x, int y);
        public abstract Color GetPixelRel(float x, float y);
        
        public abstract int[] GetPixelsTransformed(int rot = 0, int mulalpha = 255);
        public abstract void Save(string filename);

        public abstract void MulAlpha(int alpha = 255);


    }

}
