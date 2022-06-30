using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageTransformer
{

    //contains strategies to be applied via Pixel[].Transform
    public static class PixelTransformation
    {
        //set pixels that aren't background or foreground to background
        public static Action<Pixel[], Pixel> ConvertToBiModal()
        {
            List<Rgba32>? modes = null;
            Rgba32 backgroundColour = Color.White ;
            Rgba32 foregroundColour = Color.White;
            Action<Pixel[], Pixel> rv;
            
            rv = (src, current) => {
            
                //initialize modes
                if (modes == null)
                {    
                    modes = src.GetBiModes();
                    backgroundColour = modes[0];
                    foregroundColour = modes[1];
                }

                //set pixels that aren't background or foreground to background
                if(current.Color.Equals(backgroundColour))
                    return;
                
                if(current.Color.Equals(foregroundColour))
                    return;
                
                current.SetColor(backgroundColour.R, backgroundColour.G, backgroundColour.B, backgroundColour.A);
            };

            return rv;

        }

        //general conversion strategy that needs an accumulate and mutation strategy
        //both strategies are contextual to current pixel, and its neighbours
        public static Action<Pixel[], Pixel> ConvertNeighbours ( int width, bool north, bool south, bool east, bool west,
         /*source, current, neighbour, accumulated*/ Func<Pixel[], Pixel,Pixel,List<Pixel>> accumulateStrategy, 
         /*current, neighbour, accumulated*/ Action<Pixel, Pixel, List<Pixel>> mutationStrategy )
        {
            Action<Pixel[], Pixel> rv;
            
            //decorate the accumulation strategy to not accumulate already changed pixels
            //this is the general approach:  
            //  have all extendable behaviour be strategy
            //  mutate strategy by decoration
            Func<Pixel[], Pixel,Pixel,List<Pixel>> accumulateUnchanged = (src, current, neighbour) =>{
                List<Pixel> accumulated = new List<Pixel>();        

                //skip processing any already processed pixels
                if(current.AsOneTime().IsChanged)
                    return accumulated;

                if(neighbour.AsOneTime().IsChanged)
                    return accumulated;
            
                //run decorated strategy
                accumulated.AddRange(accumulateStrategy(src, current, neighbour));

                //remove any processed ones
                accumulated.RemoveAll(x=>x.AsOneTime().IsChanged);

                return accumulated;
            };

            rv = (src, current) => {
                
                //skip processing any already processed pixels
                if(current.AsOneTime().IsChanged)
                    return;

                var neighbours = src.GetNeighbours(current, width, north, south, east, west);  

                //cycle thru the neighbours and accumulate pixels
                foreach(var neighbour in neighbours)
                {
                    //accumulate
                    List<Pixel> accumulated = accumulateUnchanged(src, current, neighbour);

                    //mutate
                    mutationStrategy(current, neighbour, accumulated);
                }
            };

            return rv;
        }
        
        //if a pixel has currentColour this strategy will set the neighbouring pixels to the same colour
        public static Action<Pixel[], Pixel> ConvertNeighboursToCurrentColour (Rgba32 currentColour, int width, bool north, bool south, bool east, bool west)
        {
            Func<Pixel[], Pixel,Pixel,List<Pixel>> accumulateStrategy = (src, current, neighbour)=>{

                List<Pixel> rv = new List<Pixel>();

                //if the current pixel isn't the currentColour, skip it        
                if(!current.Color.Equals(currentColour))
                    return rv;

                //if the neighbour pixel is the currentColour, skip it
                if(neighbour.Color.Equals(currentColour))
                    return rv;

                rv.Add(neighbour);
                return rv;
            };

            Action<Pixel, Pixel, List<Pixel>> mutationStrategy = (current, neighbour, accumulated)=>{
                accumulated.ForEach((item) => 
                { 
                    //Console.WriteLine("setting color of {0}", item);
                    item.SetColor(currentColour.R, currentColour.G, currentColour.B, currentColour.A); 
                    //Console.WriteLine("to {0}", item);
                });
            };

            return ConvertNeighbours(width, north, south, east, west, accumulateStrategy, mutationStrategy);
        }
        
       //if a pixel has currentColour this strategy will look for neighbour pixels that have the same colour and set any "inbetween" pixels
       //to the same colour
        public static Action<Pixel[], Pixel> ConvertInbetweenNeighboursToCurrentColour (Rgba32 currentColour, int width, bool north, bool south, bool east, bool west)
        {
            Func<Pixel[], Pixel,Pixel,List<Pixel>> accumulateStrategy = (src, current, neighbour)=>{

                List<Pixel> rv = new List<Pixel>();

                //if the home pixel isn't the currentColour, skip it        
                if(!current.Color.Equals(currentColour))
                    return rv;

                //if the neighbour pixel isn't the currentColour, skip it
                if(!neighbour.Color.Equals(currentColour))
                    return rv;

                //get all inbetween    
                List<Pixel> betweens = src.GetInbetweenPixels(current, neighbour);
                    
                rv.AddRange(betweens);
                return rv;
            };

            Action<Pixel, Pixel, List<Pixel>> mutationStrategy = (current, neighbour, accumulated)=>{
                accumulated.ForEach((item) => { item.SetColor(currentColour.R, currentColour.G, currentColour.B, currentColour.A); });
            };

            return ConvertNeighbours(width, north, south, east, west, accumulateStrategy, mutationStrategy);
        }

        public static Action<Pixel[], Pixel> AverageNeighbours(int width, bool north, bool south, bool east, bool west)
        {
            Action<Pixel[], Pixel> rv;
            rv = (src, current) => {

                List<Pixel> neighbours = src.GetNeighbours(current, width, north, south, east, west);  
                neighbours.Add(current);

                int count = neighbours.Count;
                int rTot = neighbours.Sum(q => q.Color.R);
                int gTot = neighbours.Sum(q => q.Color.G);
                int bTot = neighbours.Sum(q => q.Color.B);
                int aTot = neighbours.Sum(q => q.Color.A); 

                int rAvg = rTot / count;
                int gAvg = gTot / count;
                int bAvg = bTot / count;
                int aAvg = aTot / count;

                //now set the neighbours
                neighbours.ForEach(neighbour=>{
                    neighbour.SetColor(rAvg, gAvg, bAvg, aAvg);
                });
                  
            };

            return rv;
        }

    }
}
