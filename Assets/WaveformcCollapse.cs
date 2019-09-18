using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

struct Point
{
    public int x;
    public int y;

    public Point(int xp, int yp)
    {
        x = xp;
        y = yp;
    }
}

public class MyEqualityComparer : IEqualityComparer<int[,]>
{
    public bool Equals(int[,] x, int[,] y)
    {
        if (x.Length != y.Length || x.GetLength(0) != y.GetLength(0) || x.GetLength(1) != y.GetLength(1))
        {
            return false;
        }
        for (int i = 0; i < x.GetLength(0); i++)
        {
            for (int j = 0; j < x.GetLength(1); j++)
            {
                if (x[i, j] != y[i, j])
                {
                    return false;
                }
            }
        }
        return true;
    }

    public int GetHashCode(int[,] obj)
    {
        int result = 17;
        for (int i = 0; i < obj.GetLength(0); i++)
        {
            for (int j = 0; i < obj.GetLength(1); i++)
            {
                unchecked
                {
                    result = result * 23 + obj[i, j];
                }
            }
        }
        return result;
    }
}

public class WaveformcCollapse : MonoBehaviour
{
    public int n = 2;
    public int maxVal = 1;
    public int outputSizeX = 20;
    public int outputSizeY = 20;

    public List<Color> colors = new List<Color> {
        Color.black,
        Color.white
    };

    public List<List<int>> input = new List<List<int>>  {
        new List<int> { 0, 0, 0, 0, 0, },
        new List<int> { 0, 0, 0, 0, 0, },
        new List<int> { 0, 0, 1, 0, 1, },
        new List<int> { 0, 0, 0, 0, 0, },
        new List<int> { 0, 0, 0, 0, 0, },
    };

    private Dictionary<int[,], int> patterns;
    private Dictionary<int[,], bool>[,] wave;
    private Dictionary<Point, GameObject> squaresDrawn;

    public GameObject blackSquare;
    public GameObject whiteSquare;
    public bool autorun = false;
    int offsetx = 0;
    int offsety = 0;
    int steps = 0;
    bool contradictive = false;

    HashSet<Point> interestingPoints;

    // Start is called before the first frame update
    void Start()
    {
        squaresDrawn = new Dictionary<Point, GameObject>();
        transpose(input);
        Random.InitState(41);
        patterns = new Dictionary<int[,], int>(new Dictionary<int[,], int>(), new MyEqualityComparer());

        wave = new Dictionary<int[,], bool>[outputSizeX, outputSizeY];
        foreach (var permutationp in new List<List<List<int>>> { input, rotate90(input), rotate180(input), rotate270(input), flip(input), flip(rotate90(input)) })
        {
            var permutation = Make2DArray(permutationp);

            foreach (int x in Enumerable.Range(0, permutation.GetLength(0) - n + 1))
            {
                foreach (int y in Enumerable.Range(0, permutation.GetLength(1) - n + 1))
                {
                    var l = new int[n, n];
                    foreach (int xi in Enumerable.Range(0, n))
                    {
                        foreach (int yi in Enumerable.Range(0, n))
                        {
                            l[xi, yi] = permutation[x + xi, y + yi];
                        }
                    }
                    if (patterns.ContainsKey(l))
                    {
                        patterns[l] += 1;
                    }
                    else
                    {
                        patterns[l] = 1;
                    }
                }
            }
        }

        foreach (int x in Enumerable.Range(0, outputSizeX))
        {
            foreach (int y in Enumerable.Range(0, outputSizeY))
            {
                wave[x, y] = new Dictionary<int[,], bool>(new Dictionary<int[,], bool>(), new MyEqualityComparer());
                foreach (var pattern in patterns)
                {
                    wave[x,y][pattern.Key] = true;
                }
            }
        }

        foreach (int rendery in Enumerable.Range(0, input.Count))
        {
            foreach (int renderx in Enumerable.Range(0, input[0].Count))
            {

                GameObject sq = Instantiate(whiteSquare);
                sq.GetComponent<SpriteRenderer>().color = colors[input[rendery][renderx]];
                sq.transform.position = new Vector3(renderx - outputSizeX / 2 - input.Count - 1, -rendery + outputSizeX / 2 + input[0].Count, 0);
            }
        }



        foreach (var square in squaresDrawn.Values)
        {
            Destroy(square);
        }
        foreach (var x in Enumerable.Range(0, outputSizeX))
        {

            foreach (var y in Enumerable.Range(0, outputSizeX))
            {
                squaresDrawn[new Point(x, y)] = Instantiate(whiteSquare);
                squaresDrawn[new Point(x, y)].transform.position = new Vector3(x, y);
            }
        }

        return;
    }

    // Update is called once per frame
    void Update()
    {
        if (autorun)
        {
            step();
        }
    }

    public void step()
    {
        if (contradictive) return;
        steps++;

        interestingPoints = new HashSet<Point>();

        void detectContradictions(Dictionary<int[,], bool>[,] wave, out bool d, out Point maxNeg)
        {
            // Find the position with the most negentropy
            d = true;
            var ps = new List<Point>();
            var maxscore = 0;
            var choiceIsValid = false;
            foreach (int x in Enumerable.Range(0, wave.GetLength(0)))
            {
                foreach (int y in Enumerable.Range(0, wave.GetLength(1)))
                {
                    var numberOfFalses = 0;
                    var numberOfTrues = 0;
                    foreach (var possibility in wave[x,y])
                    {
                        if (possibility.Value == false)
                        {
                            numberOfFalses += 1;
                        }
                        else
                        {
                            numberOfTrues += 1;
                        }
                    }
                    if (numberOfTrues == 0)
                    {
                        contradictive = true;
                        Debug.Log("We have encountered a contradiction at " + x.ToString() + ", " + y.ToString() + "!");
                    }
                    if (numberOfTrues > 1)
                    {
                        if (!choiceIsValid || numberOfFalses > maxscore)
                        {
                            d = false;
                            ps = new List<Point>();
                            ps.Add(new Point(x, y));
                            maxscore = numberOfFalses;
                            choiceIsValid = true;
                        }
                        else if (numberOfFalses == maxscore)
                        {
                            ps.Add(new Point(x, y));
                        }
                    }
                }
            }
            maxNeg = new Point(0, 0);
            if (choiceIsValid)
            {
                maxNeg = ps[Random.Range(0, ps.Count)];
            }

        }
        Point toCollapse;
        bool done;
        detectContradictions(wave, out done, out toCollapse);
        offsetx = offsety = (int)Mathf.Floor(n / 2);

        if (!done)
        {
            // collapse the superposition
            Debug.Log("collapsing wavefunction at " + toCollapse.x.ToString() + ", " + toCollapse.y.ToString());
            var onesToMaybeKeep = (from entry in wave[toCollapse.x, toCollapse.y]
                                   where entry.Value
                                   select entry.Key).ToList();
            var total = (from key in onesToMaybeKeep
                         select patterns[key]).Sum();

            var oneToKeep = Random.Range(0, total - 1);
            int i = 0;
            var patternToKeep = onesToMaybeKeep.ToList()[0];
            foreach (var maybe in onesToMaybeKeep)
            {
                i += patterns[maybe];
                if (i >= oneToKeep)
                {
                    patternToKeep = maybe;
                }
            }


            foreach (var possibility in wave[toCollapse.x, toCollapse.y].Keys.ToArray())
            {
                wave[toCollapse.x, toCollapse.y][possibility] = possibility == patternToKeep;
                detectContradictions(wave, out _, out _);
            }
            interestingPoints.UnionWith(generatePointsInSquare(toCollapse));


            // propagate updates
            while (interestingPoints.Count() != 0)
            {
                detectContradictions(wave, out _, out _);
                foreach (Point p in interestingPoints.ToList())
                {
                    interestingPoints.Remove(p);
                    if (p.x < 0 || p.y < 0 || p.x >= wave.GetLength(0) || p.y >= wave.GetLength(1))
                    {
                        continue;
                    }


                    var truePossibilities = from entry in wave[p.x, p.y]
                                            where entry.Value
                                            select entry.Key;
                    int c = truePossibilities.Count();
                    if (c < 2)
                    {
                        continue;
                    }
                    foreach (var possibility in truePossibilities.ToArray())
                    {
                        bool possibilityValid = true;
                        foreach (int yr in Enumerable.Range(0, n))
                        {
                            foreach (int xr in Enumerable.Range(0, n))
                            {
                                var posx = p.x + xr;
                                var posy = p.y + yr;
                                var color = possibility[xr, yr];
                                foreach (int xi in Enumerable.Range(0, n))
                                {
                                    foreach (int yi in Enumerable.Range(0, n))
                                    {
                                        if ((posx - xi) >= 0 && (posy - yi) >= 0 && (posx - xi) < wave.GetLength(0) && (posy - yi) < wave.GetLength(1))
                                        {
                                            bool foundOne = false;
                                            foreach (var possibility2 in wave[posx - xi, posy - yi])
                                            {
                                                if (possibility2.Value && possibility2.Key[xi, yi] == color)
                                                {
                                                    foundOne = true;
                                                    break;
                                                }
                                            }
                                            if (!foundOne)
                                            {
                                                possibilityValid = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!possibilityValid)
                        {
                            wave[p.x, p.y][possibility] = false;
                            interestingPoints.UnionWith(generatePointsInSquare(p));

                            if ((from w in wave[p.x, p.y]
                                 where w.Value
                                 select w.Value).Count() == 0)
                            {
                                Debug.Log("Found a contradiction at " + p.x.ToString() + ", " + p.y.ToString() + ".");
                                contradictive = true;
                                squaresDrawn[p].GetComponent<SpriteRenderer>().color = Color.red;
                                return;
                            }
                        }
                    }
                }
            }

            var pointsToDraw = new HashSet<Point>(from x in Enumerable.Range(0, wave.GetLength(0))
                                                  from y in Enumerable.Range(0, wave.GetLength(1))
                                                  select new Point(x, y));
            /*foreach (var p in pointsToDraw)
            {
                if (squaresDrawn.ContainsKey(p))
                {
                    if ((from w in wave[p.y][p.x]
                         where w.Value
                         select w.Value).Count() <= 1)
                    {
                        pointsToDraw.Remove(p);
                    }
                }
            }*/

            foreach (var p in pointsToDraw)
            {
                var possibilities = (from entry in wave[p.x, p.y]
                                     where entry.Value
                                     select new LABColor(colors[entry.Key[offsetx, offsety]])).ToList();
                if (possibilities.Count() > 0)
                {
                    var l = (from c in possibilities
                             select c.l).Sum() / possibilities.Count();
                    var a = (from c in possibilities
                             select c.a).Sum() / possibilities.Count();
                    var b = (from c in possibilities
                             select c.b).Sum() / possibilities.Count();
                    var color = (new LABColor(l, a, b)).ToColor();
                    if (possibilities.Count() > 1)
                    {
                        color.a = Mathf.Lerp(.7f, .1f, possibilities.Count() / 5);
                    }
                    else
                    {
                        color.a = 1f;
                    }

                    squaresDrawn[p].GetComponent<SpriteRenderer>().color = color;
                    squaresDrawn[p].transform.position = new Vector3(p.x + transform.position.x, -p.y + transform.position.y, 0);
                }
            }
        }
    }

    List<List<int>> transpose(List<List<int>> input)
    {
        var output = new List<List<int>>();

        foreach (var value in input[0])
        {
            output.Add(new List<int>());
        }

        foreach (int y in Enumerable.Range(0, input.Count))
        {
            foreach (int x in Enumerable.Range(0, input[0].Count))
            {
                output[x].Add(input[y][x]);
            }
        }
        return output;
    }

    List<List<int>> flip(List<List<int>> input)
    {
        var output = new List<List<int>>();
        foreach (int y in Enumerable.Range(0, input.Count))
        {
            output.Add(input[y].AsEnumerable().Reverse().ToList());
        }
        return output;
    }

    List<List<int>> rotate90(List<List<int>> input)
    {
        return flip(transpose(input));
    }
    List<List<int>> rotate180(List<List<int>> input)
    {
        return rotate90(rotate90(input));
    }
    List<List<int>> rotate270(List<List<int>> input)
    {
        return rotate90(rotate90(rotate90(input)));
    }

    IEnumerable<Point> generatePointsInSquare(Point p)
    {
        var a = from x in Enumerable.Range(-n + 1, n * 2)
                from y in Enumerable.Range(-n + 1, n * 2)
                select new Point(x + p.x, y + p.y);
        var b = a.ToList();
        return a;
    }

    int[,] Make2DArray(List<List<int>> i)
    {
        int height = i.Count();
        int width = i[0].Count();
        var o = new int[width, height];
        foreach (var x in Enumerable.Range(0, width))
        {
            foreach (var y in Enumerable.Range(0, height))
            {
                o[x, y] = i[y][x];
            }
        }
        return o;
    }

}