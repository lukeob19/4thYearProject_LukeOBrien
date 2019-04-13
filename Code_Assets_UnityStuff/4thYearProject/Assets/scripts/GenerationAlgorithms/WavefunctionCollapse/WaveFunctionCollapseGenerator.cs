

/**********************************************************************************************

    Luke O Brien - P11011180
    4th Year Project - Procedural Generation of Dungeons
    WaveFunctionCollapseGenerator.cs

************************************************************************************************/

using System;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;



public class WaveFunctionCollapseGenerator : MonoBehaviour
{
    int[,] dungeon;

    int width, height;

    // represents the size of the patterns taken from the input image
    public int N = 3;

    // if the input pattern is tiling, try to connect patterns right and bottom sides with left and top sides
    public bool periodicInput;

    // determines if the output solution is tilable
    public bool periodicOutput;

    // represents which additional symmetries of the input pattern are digested 
    // 1 is just the original input, 2-8 adds mirrored and rotated variations
    public int symmetry = 2;

    // the name of the input image
    public string inputName;


    // called once on creation
    private void Start()
    {
        // retrieve the output dimenions
        width = PlayerPrefs.GetInt("ppWidth");
        height = PlayerPrefs.GetInt("ppHeight");

        System.Random random = new System.Random();

        // create the model
        Model model;                
        model = new OverlappingModel(inputName, N, width, height, periodicInput, periodicOutput, symmetry);

        // iterate 10 times
        for (int i = 0; i < 10; i++)
        {
            // run the algorithm with a random seed and return whether or not it completed
            int seed = random.Next();
            bool finished = model.Run(seed);

            // if the algorithm completed
            if (finished)
            {
                // get the output image
                Texture2D texture2D = model.Graphics();

                //set the size of the dungeon array
                dungeon = new int[texture2D.width, texture2D.height];

                // iterate through the dungeon while mapping the image to it
                for (int column = 0; column < dungeon.GetLength(0); column++)
                {
                    for (int row = 0; row < dungeon.GetLength(1); row++)
                    {
                        // decide on whether the tile is a wall or not based on the greyscale value of the pixel
                        float tempValue = texture2D.GetPixel(column, row).grayscale;
                        if (tempValue < 0.51f)
                        {
                            dungeon[column, row] = 0;
                        }
                        else
                        {
                            dungeon[column, row] = 1;
                        }
                    }
                }

                // this block of code is just adding a border of walls around the generated dungeon
                int borderSize = 5;
                int[,] borderedDungeon = new int[width + borderSize * 2, height + borderSize * 2];

                // iterate through the 2d array. GetLength(0) returns the width of the array
                for (int column = 0; column < borderedDungeon.GetLength(0); column++)
                {
                    // GetLength(1) returns the height of the array
                    for (int row = 0; row < borderedDungeon.GetLength(1); row++)
                    {
                        // putting the original array into the bordered one
                        if (column >= borderSize && column < width + borderSize && row >= borderSize && row < height + borderSize)
                        {
                            borderedDungeon[column, row] = dungeon[column - borderSize, row - borderSize];
                        }
                        else // add the border
                        {
                            borderedDungeon[column, row] = 1;
                        }
                    }
                }

                // create a MeshGenerator and generate a mesh based on the 2d array
                MeshGenerator gen = GetComponent<MeshGenerator>();
                gen.GenerateMesh(borderedDungeon, 1);

                // pass the dungeon to be exported as a csv file
                FindObjectOfType<GameManager>().ExportDungeonData(borderedDungeon);
                

                // prints the output image inside unity
                Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
                GameObject gameObject = new GameObject($"{name}");
                SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                break;
            }
            
        }
    }

    // left-click to regenerate dungeon
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Start();
        }
    }
}


// defines the functionality of the algorithm
abstract class Model
{
    // stores the state/states of a pattern at each pixel
    protected bool[][] wave;

    // stores the pattern data as we move up, down, left and right
    protected int[][][] propagator;

    //for each pixel of each pattern, store the number of compatible patterns in a direction
    protected int[][][] compatible;

    // holds the final result of the pattern
    protected int[] finalState;

    // a tuple of int's
    (int, int)[] stack;
    int stacksize;

    System.Random random;

    // the output images dimensions
    protected int outputImageWidth, outputImageHeight;

    // number of patterns that are not repeated
    protected int remainingAllowedPatterns;

    // whether the output image is periodic
    protected bool periodic;

    // number of times each pattern is recorded
    protected double[] patternWeights;

    // patternWeightLogWeights[i] = patternWeights[i] * Math.Log(patternWeights[i]);
    double[] patternWeightLogWeights;

    //total number of patterns
    int[] sumsOfOnes;

    // storing the sum of weights and the positions entropy
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    // constructor that sets the dimensions of the output
    protected Model(int width, int height)
    {
        outputImageWidth = width;
        outputImageHeight = height;
    }

    //init function
    void Init()
    {
        // initialize wavefunction and compatible
        wave = new bool[outputImageWidth * outputImageHeight][];
        compatible = new int[wave.Length][][];


        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[remainingAllowedPatterns];
            compatible[i] = new int[remainingAllowedPatterns][];
            for (int j = 0; j < remainingAllowedPatterns; j++)
            {
                compatible[i][j] = new int[4];
            }
        }

        // initialize variables
        patternWeightLogWeights = new double[remainingAllowedPatterns];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        // getting the weights of the remaining allowed patterns
        for (int i = 0; i < remainingAllowedPatterns; i++)
        {
            patternWeightLogWeights[i] = patternWeights[i] * Math.Log(patternWeights[i]);
            sumOfWeights += patternWeights[i];
            sumOfWeightLogWeights += patternWeightLogWeights[i];
        }

        // shannon entropy formula
        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        // init variables
        sumsOfOnes = new int[outputImageWidth * outputImageHeight];
        sumsOfWeights = new double[outputImageWidth * outputImageHeight];
        sumsOfWeightLogWeights = new double[outputImageWidth * outputImageHeight];
        entropies = new double[outputImageWidth * outputImageHeight];

        stack = new (int, int)[wave.Length * remainingAllowedPatterns];
        stacksize = 0;
    }

    Nullable<bool> SelectPossibleStateToBeRemoved()
    {
        // the minimum entropy
        double minimum = 1E+3;

        // used for indexing
        int argmin = -1;


        for (int i = 0; i < this.wave.Length; i++)
        {
            if (OnBoundary(i % outputImageWidth, i / outputImageWidth))
            {
                continue;
            }

            // getting the amount of possible states
            int amount = sumsOfOnes[i];

            // if there are no compatible patterns then we've hit a contradiction and have failed.
            if (amount == 0)
            {
                return false;
            }

            double entropy = entropies[i];

            // if there is more than one pattern and the entropy is less than the current minimum
            if (amount > 1 && entropy <= minimum)
            {

                // using noise to select a random position to observe
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < minimum)
                {
                    minimum = entropy + noise;
                    argmin = i;
                }
            }
        }
        // if argmin = -1, then all waves have been collapsed
        if (argmin == -1) 
        {
            finalState = new int[outputImageWidth * outputImageHeight];
            for (int i = 0; i < this.wave.Length; i++)
            {
                for (int j = 0; j < remainingAllowedPatterns; j++)
                {
                    if (this.wave[i][j])
                    {
                        // store the index of the determined tile
                        finalState[i] = j;
                        break;
                    }
                }
            }
            //return true to start drawing the output
            return true;
        }

        // getting the weight of each pattern
        double[] distribution = new double[remainingAllowedPatterns];
        for (int i = 0; i < remainingAllowedPatterns; i++)
        {
            if(this.wave[argmin][i])
            {
                distribution[i] = patternWeights[i];
            }
            else
            {
                distribution[i] = 0;
            }
        }

        // select one of the patterns randomly based on how often they appear in the input
        int randomWave = distribution.GetWeightedRandom(random.NextDouble());

        bool[] wave = this.wave[argmin];

        // iterate through the wavefunctions, remove all except when i == randomWave
        for (int i = 0; i < remainingAllowedPatterns; i++)
        {
            if (wave[i] != (i == randomWave))
            {
                // remove
                RemovePossibleState(argmin, i);
            }
        }

        // continue
        return null;
    }
    
    // propogate the consequences of the wave at a position collapsing
    // it will recursively do this until no consequences remain
    protected void Propagate()
    {
        // while the stack isnt 0
        while (stacksize > 0)
        {

            var tuple = stack[stacksize - 1];
            stacksize--;

            //getting the pattern position
            int patternPosition1 = tuple.Item1;

            // getting the corrisponing coordinates
            int x1 = patternPosition1 % outputImageWidth, y1 = patternPosition1 / outputImageWidth;

            //for the current position, check the tiles above, below, to the left and right
            for (int i = 0; i < 4; i++)
            {
                // get the pixels in the cardinal directions
                int dx = xDirections[i], dy = yDirections[i];
                int x2 = x1 + dx, y2 = y1 + dy;
                if (OnBoundary(x2, y2))
                {
                    continue;
                }

                // for when we want the output to be tiling/periodic
                // (we want to try and connect the right/bottom sides of patterns to the left and top sides of other patterns)
                if (x2 < 0)
                {
                    x2 += outputImageWidth;
                }
                else if (x2 >= outputImageWidth)
                {
                    x2 -= outputImageWidth;
                }
                if (y2 < 0)
                {
                    y2 += outputImageHeight;
                }
                else if (y2 >= outputImageHeight)
                {
                    y2 -= outputImageHeight;
                }

                int patternPosition2 = x2 + y2 * outputImageWidth;
                int[] prop = propagator[i][tuple.Item2];

                // stores all of the compatable patterns
                int[][] compat = compatible[patternPosition2];

                for (int j = 0; j < prop.Length; j++)
                {
                    // the adjacent pattern is equals to prop[j], the pattern to be removed
                    int adjacentPattern = prop[j];

                    // get the number compatible patterns in a direction, then subtract one to get the current waves possible patterns
                    int[] comp = compat[adjacentPattern];
                    comp[i]--;

                    // if the pattern is not compatible with any possible pattern in the adjacent tile
                    if (comp[i] == 0)
                    {
                        // remove the pattern
                        RemovePossibleState(patternPosition2, adjacentPattern);
                    }
                }
            }
        }
    }

    // collapses the wavefunction
    public bool Run(int seed)
    {
        if (wave == null)
        {
            Init();
        }

        ClearOutput();
        random = new System.Random(seed);

 
        while (true)
        {
            // looks at the entropy values and selects a pattern to be removed based on how often it appears in the input
            Nullable<bool> result = SelectPossibleStateToBeRemoved();
            if (result != null)
            {
                return (bool)result;
            }
            Propagate();
        }
    }

    // looks at the entropy values and selects a pattern to be removed based on how often it appears in the input
    protected void RemovePossibleState(int i, int j)
    {
        wave[i][j] = false;
        int[] comp = compatible[i][j];

        for (int i2 = 0; i2 < 4; i2++)
        {
            // set the compatible patterns of the surrounding tiles to 0 
            comp[i2] = 0;
        }
        stack[stacksize] = (i, j);
        stacksize++;

        // the effect of the current pattern is removed from each position
        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= patternWeights[j];
        sumsOfWeightLogWeights[i] -= patternWeightLogWeights[j];

        double sum = sumsOfWeights[i];

        // update entropies
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    // it clears the output so that each tile is in a state of superposition
    // (so that any pattern could possibly be there)
    protected virtual void ClearOutput()
    {
        for (int pixel = 0; pixel < wave.Length; pixel++)
        {
            for (int pattern = 0; pattern < remainingAllowedPatterns; pattern++)
            {
                // making every pattern possible
                wave[pixel][pattern] = true;
                for (int direction = 0; direction < 4; direction++)
                {
                    // compatible = the number of matches in the four directions
                    compatible[pixel][pattern][direction] = propagator[opposite[direction]][pattern].Length;
                }
            }

            // setting the pattern total, weights and log weights and the initial entropy
            sumsOfOnes[pixel] = patternWeights.Length;
            sumsOfWeights[pixel] = sumOfWeights;
            sumsOfWeightLogWeights[pixel] = sumOfWeightLogWeights;
            entropies[pixel] = startingEntropy;
        }
    }



    protected abstract bool OnBoundary(int x, int y);
    public abstract UnityEngine.Texture2D Graphics();

    // left and right of the pattern
    protected static int[] xDirections = { -1, 0, 1, 0 };

    // above and below of the pattern
    protected static int[] yDirections = { 0, 1, 0, -1 };
    // reverse index
    static int[] opposite = { 2, 3, 0, 1 };
}


// needed a place where i can call the extension method 'GetWeightedRandom'
// an extension method allows you to add functionality to existing classes ( in this case 'double')
// they must be static otherwise the compiler throws an error
static class StaticTools
{
    public static int GetWeightedRandom(this double[] distribution, double random)

    {
        //get the sum of the weights in the double array
        double sum = distribution.Sum();

        //for each position in the array, divide by the sum
        for (int i = 0; i < distribution.Length; i++)
        {
            distribution[i] /= sum;
        }

        int j = 0;
        double x = 0;

        //while j is less than the array length
        // keep adding  the value in the array to x. if the random double is less than or equal to x, return j otherwise return 0
        while (j < distribution.Length)
        {
            x += distribution[j];
            if (random <= x)
            {
                return j;
            }
            j++;
        }

        return 0;
    }

}

// This is one of the models for the wave function collapse algorithm, I chose this one as its more suited to dungeon generation
// it breaks an input pattern into chunks similar to a markov chain
class OverlappingModel : Model
{
    // pattern size
    int N;

    // is an array of indexes in the format of patterns[X][N*N]
    byte[][] patterns;

    // list of unique colors
    List<Color> colors;

    // constructor
    public OverlappingModel(string name, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry)
        : base(width, height)
    {
        // sample size
        this.N = N; 

        // whether the output is tiled/periodic
        periodic = periodicOutput;

        // retrieving the input image
        string path = $"input/{name}";

        // storing the image as a texture
        Texture2D texture = Resources.Load<Texture2D>(path);

        // getting the images dimensions
        int inputImageWidth = texture.width;
        int inputImageHeight = texture.height;

        // stores the color index of the image
        byte[,] inputImage = new byte[inputImageWidth, inputImageHeight];

        colors = new List<Color>();

        // iterate through the image and extract the color of each pixel
        for (int row = 0; row < inputImageHeight; row++)
        {
            for (int column = 0; column < inputImageWidth; column++)
            {
                // get the pixels color
                Color color = texture.GetPixel(column, row);

                int i = 0;
                foreach (var c in colors)
                {
                    // break if the color is in colors
                    if (c == color)
                    {
                        break;
                    }
                    //increment i if it isn't
                    i++;
                }
                // if we havent found the color after searching through colors, then add it
                if (i == colors.Count)
                {
                    colors.Add(color);
                }
                // store the colors index
                inputImage[column, row] = (byte)i;
            }
        }
        // store the amount of colors
        int colorsCount = colors.Count;

        // get the max amount of different patterns
        long maxPat = 1;
        for(int i = 0; i < (N*N); i++)
        {
            maxPat *= colorsCount;
        }
        
        // process a pattern and return a new pattern, N*N byte array
        // allows us to pass a function in as an argument
        byte[] Pattern(Func<int, int, byte> f)
        {
            byte[] result = new byte[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    result[x + y * N] = f(x, y);
                }
            return result;
        };
        
        // get a pattern from the input
        byte[] GetPatternFromInput(int x, int y) => Pattern((dx, dy) => inputImage[(x + dx) % inputImageWidth, (y + dy) % inputImageHeight]);

        // rotate a pattern anti clockwise
        byte[] Rotate(byte[] inputPattern) => Pattern((x, y) => inputPattern[N - 1 - y + x * N]);

        // mirror a pattern so that it is backwards
        byte[] Mirror(byte[] inputPattern) => Pattern((x, y) => inputPattern[N - 1 - x + y * N]);

        // convert a pattern to a long number that will act as an index
        long IndexPattern(byte[] inputPattern)
        {
            long result = 0;
            long power = 1;
            for (int i = 0; i < inputPattern.Length; i++)
            {
                // multiply by 1 to prevent index conflicts
                result += inputPattern[inputPattern.Length - 1 - i] * power;
                power *= colorsCount;
            }
            return result;
        };

        //rebuild pattern from a passed in index long number
        byte[] GetPatternFromIndex(long patternIndex)
        {
            long residue = patternIndex;
            long power = maxPat;
            byte[] result = new byte[N * N];

            for (int i = 0; i < result.Length; i++)
            {
                power /= colorsCount;
                int count = 0;

                while (residue >= power)
                {
                    residue -= power;
                    count++;
                }

                result[i] = (byte)count;
            }

            return result;
        };

        // store the index and weight of the patterns
        Dictionary<long, int> weights = new Dictionary<long, int>();
        List<long> indexes = new List<long>();
        
        for (int row = 0; row < (periodicInput ? inputImageHeight : inputImageHeight - N + 1); row++)
        {

            for (int column = 0; column < (periodicInput ? inputImageWidth : inputImageWidth - N + 1); column++)
            {
                //8 options reliant on the symmetry variable
                byte[][] patternSymmetry = new byte[8][];

                //for each N*N pattern, get 3 rotated variants and every mirrored variant
                patternSymmetry[0] = GetPatternFromInput(column, row);
                patternSymmetry[1] = Mirror(patternSymmetry[0]);
                patternSymmetry[2] = Rotate(patternSymmetry[0]);
                patternSymmetry[3] = Mirror(patternSymmetry[2]);
                patternSymmetry[4] = Rotate(patternSymmetry[2]);
                patternSymmetry[5] = Mirror(patternSymmetry[4]);
                patternSymmetry[6] = Rotate(patternSymmetry[4]);
                patternSymmetry[7] = Mirror(patternSymmetry[6]);

                // determines based off of the symmetry variable whether the rotated and mirrored variants 
                // are also added to the possible pattern list
                for (int i = 0; i < symmetry; i++)
                {
                    long index = IndexPattern(patternSymmetry[i]);
                    if (weights.ContainsKey(index))
                    {
                        weights[index]++;
                    }
                    else
                    {
                        weights.Add(index, 1);
                        indexes.Add(index);
                    }
                }
            }
        }

        // the number of patterns that are not repeated
        remainingAllowedPatterns = weights.Count;

        patterns = new byte[remainingAllowedPatterns][];

        // the number of times each pattern occurs including rotated/mirrored variants
        patternWeights = new double[remainingAllowedPatterns];

        // used to iterate through the index list
        int imRunningOutOfCountNames = 0;

        // for each index
        // get the pattern, its weight and increment the counter
        foreach (long index in indexes)
        {
            patterns[imRunningOutOfCountNames] = GetPatternFromIndex(index);
            patternWeights[imRunningOutOfCountNames] = weights[index];
            imRunningOutOfCountNames++;
        }

        // determines if two patterns overlap after moving
        bool IsOverlapping(byte[] pattern1, byte[] pattern2, int directionX, int directionY)
        {
            // variables
            int xmin; 
            int xmax; 
            int ymin; 
            int ymax; 

            // get the postion of the left side of the pattern
            if(directionX < 0)
            {
                xmin = 0;
            }
            else
            {
                xmin = directionX;
            }

            // get the postion of the right side of the pattern
            if (directionX < 0)
            {
                xmax = directionX + N;
            }
            else
            {
                xmax = N;
            }

            // get the postion of the bottom side of the pattern
            if (directionY < 0)
            {
                ymin = 0;
            }
            else
            {
                ymin = directionY;
            }

            // get the postion of the top side of the pattern
            if (directionY < 0)
            {
                ymax = directionY + N;
            }
            else
            {
                ymax = N;
            }

            // iterate through the pattern pixels
            for (int row = ymin; row < ymax; row++)
            {
                for (int column = xmin; column < xmax; column++)
                {
                    //if they dont overlap return false, otherwise return true
                    if (pattern1[column + N * row] != pattern2[column - directionX + N * (row - directionY)])
                    {
                        return false;
                    }
                }
            }
            return true;
        };

        // check to see if 2 patterns will overlap in any of the 4 cardinal directions
        propagator = new int[4][][];
        for (int direction = 0; direction < 4; direction++)
        {
            propagator[direction] = new int[remainingAllowedPatterns][];
            for (int pattern1 = 0; pattern1 < remainingAllowedPatterns; pattern1++)
            {
                List<int> overlapList = new List<int>();
                for (int pattern2 = 0; pattern2 < remainingAllowedPatterns; pattern2++)
                {
                    // if pattern 1 is overlapping pattern2, add pattern2 to the overlap list
                    if (IsOverlapping(patterns[pattern1], patterns[pattern2], xDirections[direction], yDirections[direction]))
                    {
                        overlapList.Add(pattern2);
                    }
                }
                
                propagator[direction][pattern1] = new int[overlapList.Count];
                
                //store patterns from the list
                for (int c = 0; c < overlapList.Count; c++)
                {
                    propagator[direction][pattern1][c] = overlapList[c];
                }
            }
        }
    }
    // checks if the given position of a pattern will reach the boundary of the image.
    // always return false if the output is periodic 
    protected override bool OnBoundary(int x, int y) => !periodic && (x + N > outputImageWidth || y + N > outputImageHeight || x < 0 || y < 0);

    public override Texture2D Graphics()
    {
        // create the output texture and data
        Texture2D result = new Texture2D(outputImageWidth, outputImageHeight);
        int[] bitmapData = new int[result.height * result.width];

        // if the wavefunction has fully collapsed and in its final state
        if (finalState != null)
        {
            // set the colors of the output image based on the result of the collapsed wavefunction
            for (int row = 0; row < outputImageHeight; row++)
            {
                int dy; 
                // bounds check
                if (row < outputImageHeight - N + 1)
                {
                    dy = 0;
                }
                else
                {
                    dy = N - 1;
                }

                for (int column = 0; column < outputImageWidth; column++)
                {
                    int dx;
                    //bounds check
                    if(column < outputImageWidth - N + 1)
                    {
                        dx = 0;
                    }
                    else
                    {
                        dx = N - 1;
                    }

                    // setting the color of a pixel in the output 
                    int outputIndex = finalState[column - dx + (row - dy) * outputImageWidth];
                    Color color = colors[patterns[outputIndex][dx + dy * N]];
                    result.SetPixel(column, row, color);
                }
            }
        }
        // apply the changes to the output and return it
        result.Apply();
        return result;
    }
}

