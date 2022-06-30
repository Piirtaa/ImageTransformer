using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageTransformer
{
    //pixel decoration to restrict mutation to one time only
    public class OneTimeMutatePixel : Pixel
    {
        private Pixel _pixel;

        public bool IsChanged {get; set;}

        public override Rgba32 Color { get => _pixel.Color;  }

        public override int X { get => _pixel.X;  }

        public override int Y { get => _pixel.Y;  }

        public override void SetColor(int r, int g, int b, int a)
        {
            if (!this.IsChanged)
            {    
                _pixel.SetColor(r, g, b, a);
                this.IsChanged = true;
            }
        }
        public OneTimeMutatePixel(Pixel pixel) :base()
        {
            this._pixel = pixel;
            this.IsChanged = false;
        }

        #region Static Helpers
        public static OneTimeMutatePixel Decorate(Pixel pixel)
        {
            if(pixel is OneTimeMutatePixel)
                return (OneTimeMutatePixel)pixel;

            return new OneTimeMutatePixel(pixel);
        }
        public static Pixel Undecorate(Pixel pixel)
        {
            //if we had a generic decorator we'd walk the chain here
            if(pixel is OneTimeMutatePixel)
                return ((OneTimeMutatePixel)pixel)._pixel;

            return pixel;
        }
        
        public static Pixel[] DecorateArray(Pixel[] pixels)
        {
            for(int i=0; i<pixels.Length; i++)
            {
               pixels[i]=OneTimeMutatePixel.Decorate(pixels[i]); 
            }
            return pixels;
        }
        public static Pixel[] UndecorateArray(Pixel[] pixels)
        {
            //if we had a generic decorator we'd walk the chain here
            for(int i=0; i<pixels.Length; i++)
            {
               pixels[i]=OneTimeMutatePixel.Undecorate(pixels[i]); 
            }
            return pixels;
        }


        #endregion
    }    

    //fluency
    public static class OneTimeMutatePixelExtensions
    {
        public static bool IsOneTime(this Pixel pixel)
        {
            return pixel is OneTimeMutatePixel;
        }
        public static OneTimeMutatePixel AsOneTime(this Pixel pixel)
        {
            return OneTimeMutatePixel.Decorate(pixel);
        }
        public static Pixel UnAsOneTime(this Pixel pixel)
        {
            return OneTimeMutatePixel.Undecorate(pixel);
        }

        public static bool IsOneTime(this Pixel[] pixels)
        {
            foreach(var each in pixels)
            {
                if(!each.IsOneTime())
                {
                    Console.WriteLine("not one time {0}", each);    
                    return false;
                }
            }
            return true;
        }

        public static Pixel[] AsOneTime(this Pixel[] pixels)
        {
            return OneTimeMutatePixel.DecorateArray(pixels);
        }
        public static Pixel[] UnAsOneTime(this Pixel[] pixels)
        {
            return OneTimeMutatePixel.UndecorateArray(pixels);
        }
    }

}
