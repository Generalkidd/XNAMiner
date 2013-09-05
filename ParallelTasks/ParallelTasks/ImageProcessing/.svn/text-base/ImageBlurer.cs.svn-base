using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ParallelTasks;
using System.Diagnostics;

namespace ImageProcessing
{
    class ImageBlurer
    {
        public static Texture2D BlurSequential(Texture2D sourceImage, int kernalSize, out TimeSpan time)
        {
            Color[] source = new Color[sourceImage.Width * sourceImage.Height];
            sourceImage.GetData(source);

            DateTime start = DateTime.Now;
            Color[] blurredHorizontal = BlurImageSequential(source, sourceImage.Width, sourceImage.Height, new Point(1, 0), kernalSize);
            Color[] blurred = BlurImageSequential(blurredHorizontal, sourceImage.Width, sourceImage.Height, new Point(0, 1), kernalSize);
            DateTime stop = DateTime.Now;

            time = stop - start;

            Texture2D blurredImage = new Texture2D(sourceImage.GraphicsDevice, sourceImage.Width, sourceImage.Height);
            blurredImage.SetData(blurred);
            return blurredImage;
        }

        public static Texture2D BlurParallel(Texture2D sourceImage, int kernalSize, out TimeSpan time)
        {
            Color[] source = new Color[sourceImage.Width * sourceImage.Height];
            sourceImage.GetData(source);

            DateTime start = DateTime.Now;
            Color[] blurredHorizontal = BlurImageParallel(source, sourceImage.Width, sourceImage.Height, new Point(1, 0), kernalSize);
            Color[] blurred = BlurImageParallel(blurredHorizontal, sourceImage.Width, sourceImage.Height, new Point(0, 1), kernalSize);
            DateTime stop = DateTime.Now;

            time = stop - start;

            Texture2D blurredImage = new Texture2D(sourceImage.GraphicsDevice, sourceImage.Width, sourceImage.Height);
            blurredImage.SetData(blurred);
            return blurredImage;
        }


        private static Color[] BlurImageSequential(Color[] source, int width, int height, Point direction, int kernalSize)
        {
            Color[] destination = new Color[source.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 accum = Vector3.Zero;
                    for (int tap = -kernalSize; tap < kernalSize; tap++)
                    {
                        float weight = 1 - (Math.Abs(tap) / (float)kernalSize);
                        int index = CalculateIndex(width, height, x + (tap * direction.X), y + (tap * direction.Y));
                        Vector3 sample = source[index].ToVector3() * weight;
                        accum += sample;
                    }

                    destination[CalculateIndex(width, height, x, y)] = new Color(accum / kernalSize);
                }
            }

            return destination;
        }

        private static Color[] BlurImageParallel(Color[] source, int width, int height, Point direction, int kernalSize)
        {
            Color[] destination = new Color[source.Length];

            // only change to parallise this code. use Parallel.For instead of a for loop
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 accum = Vector3.Zero;
                    for (int tap = -kernalSize; tap < kernalSize; tap++)
                    {
                        float weight = 1 - (Math.Abs(tap) / (float)kernalSize);
                        int index = CalculateIndex(width, height, x + (tap * direction.X), y + (tap * direction.Y));
                        Vector3 sample = source[index].ToVector3() * weight;
                        accum += sample;
                    }

                    destination[CalculateIndex(width, height, x, y)] = new Color(accum / kernalSize);
                }
            });

            return destination;
        }

        public static int CalculateIndex(int width, int height, int x, int y)
        {
            x = (int)MathHelper.Clamp(x, 0, width - 1);
            y = (int)MathHelper.Clamp(y, 0, height - 1);

            return y * width + x;
        }
    }
}
