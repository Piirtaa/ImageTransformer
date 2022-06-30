using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using CommandDotNet;

//todo:  fluently string these transformations together randomly to apply a ga approach
//to fuzzing a solution

namespace ImageTransformer
{
    public class Program
    {
        // this is the entry point of your application
        static int Main(string[] args)
        {
            // AppRunner<T> where T is the class defining your commands
            // You can use Program or create commands in another class
            return new AppRunner<Program>().Run(args);
        }

        //define the methods we're exposing to the command line
        public void convertBiModal(string sourceFile, string destFile)
        {
            using (Image<Rgba32> image = Image.Load<Rgba32>(sourceFile))
            {
                //convert to bimodal 
                Pixel[] pixels = image.GetPixelArray().AsOneTime();
                pixels=pixels.Transform(PixelTransformation.ConvertToBiModal());
                pixels.ApplyToImage(image);
                image.Save(destFile);
            }
        }

        public void convertNeighboursToMode2(string sourceFile, string destFile, int width, bool north,
         bool south, bool east, bool west)
         {
            using (Image<Rgba32> image = Image.Load<Rgba32>(sourceFile))
            {
                //convert to bimodal 
                Pixel[] pixels = image.GetPixelArray().AsOneTime();
                //see how many pixels were changed and how many were not
                var bimodes = pixels.GetBiModes();
                
                //make all neighbour pixels to foreground     
                pixels=pixels.Transform(PixelTransformation.ConvertNeighboursToCurrentColour(bimodes[1], 
                width, north, south, east, west));
                pixels.ApplyToImage(image);
                image.Save(destFile);
            }

         }

        public void convertBetweenNeighboursToMode2(string sourceFile, string destFile, int width, bool north,
         bool south, bool east, bool west)
         {
            using (Image<Rgba32> image = Image.Load<Rgba32>(sourceFile))
            {
                //convert to bimodal 
                Pixel[] pixels = image.GetPixelArray().AsOneTime();
                //see how many pixels were changed and how many were not
                var bimodes = pixels.GetBiModes();
                
                //make all neighbour pixels to foreground     
                pixels=pixels.Transform(PixelTransformation.ConvertInbetweenNeighboursToCurrentColour(bimodes[1], 
                width, north, south, east, west));
                pixels.ApplyToImage(image);
                image.Save(destFile);
            }
         }
         
        public void averageNeighbours(string sourceFile, string destFile, int width, bool north,
         bool south, bool east, bool west)
         {
            using (Image<Rgba32> image = Image.Load<Rgba32>(sourceFile))
            {
                //convert to bimodal 
                Pixel[] pixels = image.GetPixelArray().AsOneTime();
                //see how many pixels were changed and how many were not
                var bimodes = pixels.GetBiModes();
                
                //make all neighbour pixels to foreground     
                pixels=pixels.Transform(PixelTransformation.AverageNeighbours(width, north, south, east, west));
                pixels.ApplyToImage(image);
                image.Save(destFile);
            }
         }

         public void test(string sourceFile)
         {
            //runs a bunch of tests in a sequence to vet the existing functionality

            using (Image<Rgba32> image = Image.Load<Rgba32>(sourceFile))
            {
                //convert to bimodal 
                Pixel[] pixels = image.GetPixelArray().AsOneTime();
                pixels=pixels.Transform(PixelTransformation.ConvertToBiModal());
                pixels.ApplyToImage(image);
                   
                //verify expectations
                if(!pixels.IsOneTime())
                    throw new Exception("1 is not one time!");

                //see how many pixels were changed and how many were not
                var bimodes = pixels.GetBiModes();
                var unchanged = pixels.Where(pixel=>pixel.AsOneTime().IsChanged == false);
                foreach(var each in unchanged)
                {
                    if(each.Color.Equals(bimodes[0]) == false && each.Color.Equals(bimodes[1]) == false)
                        throw new Exception (string.Format("2 unchanged error {0}", each));
                }
                
                //moving east and south switch neighbours to foreground
                Pixel[] clone = pixels.DeepClone();
                
                //verify expectations
                if(!pixels.AreEqual(clone))
                    throw new Exception("3 different!");

                //make all neighbour pixels to foreground     
                clone=clone.AsOneTime();
                clone=clone.Transform(PixelTransformation.ConvertNeighboursToCurrentColour(bimodes[1], 
                1, true, true, true, true));
                clone.ApplyToImage(image);

                //todo:  fill in tests                
            }

         }

    }
}




