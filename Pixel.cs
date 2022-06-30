using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

//todo:  fluently string these transformations together randomly to apply a ga approach
//to fuzzing a solution

namespace ImageTransformer
{
    //a container of image pixel information 
    public class Pixel : IEquatable<Pixel>
    {
        public virtual int X {get;set;}
        public virtual int Y {get;set;}
    
        public virtual Rgba32 Color {get;set;}

        protected Pixel (){}
        public Pixel(int x, int y, Rgba32 color)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
        }

        public virtual void SetColor(int r, int g, int b, int a)
        {
            this.Color = new Rgba32((byte)r, (byte)g, (byte)b, (byte)a);        
        }
        
        public override int GetHashCode()
        {
            #pragma warning disable CS8602
            return this.ToString().GetHashCode();
            #pragma warning restore CS8602
        }
        public bool Equals(Pixel? pixel)
        {
            if (pixel == null)
                return false;
            
            #pragma warning disable CS8602
            return this!.ToString().Equals(pixel.ToString());
            #pragma warning restore CS8602
        }

        public override string? ToString()
        {
            return string.Format("{0} {1} {2}", this.X, this.Y, this.Color);
        }
    }    

    //A set of extensions that orient around Pixel[] operations
    public static class PixelArrayExtensions
    {
        public static Pixel[] GetPixelArray (this Image<Rgba32> image)
        {
            List<Pixel> rv = new List<Pixel>();

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];

                        rv.Add(new Pixel(x, y, pixel));    
                    }
                }
            });

            return rv.ToArray();
        }

        public static Image<Rgba32> ApplyToImage(this Pixel[] source, Image<Rgba32> image)
        {
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];

                        //load the source to apply
                        Pixel? src = source.Get(x, y);
                        if (src != null)
                            pixel = src.Color;
                    }
                }
            });
            
            return image;
        }

        public static Pixel[] DeepClone(this Pixel[] source)
        {
            List<Pixel> rv = new ();
            foreach(var each in source)
            {
                rv.Add(new Pixel(each.X, each.Y, each.Color));
            }

            return rv.ToArray();
        }

        public static bool AreEqual(this Pixel[] source, Pixel[] compareTo)
        {
            bool rv = true;

            for(int i=0; i<source.Length; i++)
            {
                var a = source[i];
                var b = compareTo[i];        
                if(a.X != b.X )
                {
                    Console.WriteLine("mode1. different pixels {0} {1}", a, b );
                    rv = false;    
                    break;
                }

                if(a.Y != b.Y )
                {
                    Console.WriteLine("mode2. different pixels {0} {1}", a, b );
                    rv = false;    
                    break;
                }

                if(a.Color.ToString() != b.Color.ToString())
                {
                  Console.WriteLine("mode3. different pixels {0} {1}", a, b );
                    rv = false;    
                    break;
                }
            }        
            return rv;
        }

        public static Pixel? Get(this Pixel[] source, int x, int y)
        {
            Pixel? src = source.FirstOrDefault(p=>{ return p.X == x && p.Y == y;  });
            return src;
        }

        public static List<Pixel>  GetNeighbours(this Pixel[] source, Pixel current, int count, bool north, bool south, bool east, bool west)
        {
            List<Pixel> rv = new List<Pixel>();
            
            for(int i=1; i<=count ; i++)
            {
                if(north)
                {   
                    var n = source.Get(current.X, current.Y - i);
                    if (n != null)
                        rv.Add(n);
                }
                if(south)
                {   
                    var s = source.Get(current.X, current.Y + i);
                    if (s != null)
                        rv.Add(s);
                }        
                if(east)
                {   
                    var e = source.Get(current.X, current.Y + i);
                    if (e != null)
                        rv.Add(e);
                }        
                if(west)
                {   
                    var w = source.Get(current.X, current.Y - i);
                    if (w != null)
                        rv.Add(w);
                }        
            }
            return rv;
        }
        
        public static List<Pixel> GetInbetweenPixels(this Pixel[] source, Pixel a, Pixel b)
        {
            List<Pixel> rv = new List<Pixel>();

            //if the pixels aren't aligned on either X or Y there is no inbetween to iterate
            if(a.X == b.X)
            {
                if(a.Y > b.Y)
                {
                    for(int i=b.Y; i < a.Y; i++)
                    {
                        Pixel? p = source.Get(a.X,i);
                        if (p != null)
                            rv.Add(p);
                    }        
                }
                else if(b.Y > a.Y)
                {
                    for(int i=a.Y; i < b.Y; i++)
                    {
                        Pixel? p = source.Get(a.X,i);
                        if (p != null)
                            rv.Add(p);
                    }        
                }
            }
            else if (a.Y == b.Y)
            {
                if(a.X > b.X)
                {
                    for(int i=b.X; i < a.X; i++)
                    {
                        Pixel? p = source.Get(i, a.Y);
                        if (p != null)
                            rv.Add(p);
                    }        
                }
                else if(b.X > a.X)
                {
                    for(int i=a.X; i < b.X; i++)
                    {
                        Pixel? p = source.Get(i, a.Y);
                        if (p != null)
                            rv.Add(p);
                    }        
                }
            }

            return rv;
        }

        //gets the count of each colour
        public static Dictionary<Rgba32, int> GetColourModes(this Pixel[] source)
        {
            Dictionary<Rgba32, int> rv = new Dictionary<Rgba32, int>();
            foreach(var each in source)
            {
                if(!rv.ContainsKey(each.Color))
                    rv.Add(each.Color, 0);

                rv[each.Color]++;
            }
            return rv;
        }

        public static List<Rgba32> GetBiModes(this Pixel[] source)
        {
            List<Rgba32> rv = new List<Rgba32>();
            var modes = source.GetColourModes();
            var sorted = modes.OrderByDescending(x => x.Value);
            rv.Add(sorted.First().Key);
            rv.Add(sorted.Skip(1).Take(1).First().Key);
            return rv;
        }

        //generic fluent mutator via pluggable strategy
        public static Pixel[] Transform (this Pixel[] source, /*source, pixel*/ Action<Pixel[], Pixel> strategy)
        {
           foreach(Pixel each in source)
           {
                strategy(source, each);
           }   

           //finally we can reset the 
           return source;  
        }
    }


}
