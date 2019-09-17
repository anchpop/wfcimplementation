using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WaveformcCollapse : MonoBehaviour
{
    public int n = 2;
    public int maxVal = 1;
    public int outputSizeX = 20;
    public int outputSizeY = 20;
    
    public List<List<int>> input = new List<List<int>>  {
        new List<int> { 0, 0, 0, 0, },
        new List<int> { 0, 1, 1, 1, },
        new List<int> { 0, 1, 0, 1, },
        new List<int> { 0, 1, 1, 1, },
    };

    private Dictionary<List<List<int>>, int> patterns;
    private List<List<Dictionary<List<List<int>>,bool>>> wave;

    public GameObject blackSquare;
    public GameObject whiteSquare;
    List<GameObject> squares;
    public bool autorun = false;
    int offsetx = 0;
    int offsety = 0;
    int steps = 0;
    bool contradictive = false;
    // Start is called before the first frame update
    void Start()
    {
        transpose(input);
        Random.seed = 42;
        squares = new List<GameObject>();
        patterns = new Dictionary<List<List<int>>, int>();
        wave = new List<List<Dictionary<List<List<int>>, bool>>>();
        foreach (var permutation in new List<List<List<int>>> { input, rotate90(input), rotate180(input), rotate270(input), flip(input), flip(rotate90(input)) })
        {
            foreach (int x in Enumerable.Range(0, permutation.Count - n + 1))
            {
                foreach (int y in Enumerable.Range(0, permutation[0].Count - n + 1))
                {
                    var l = new List<List<int>>();
                    foreach (int xi in Enumerable.Range(0, n))
                    {
                        l.Add(new List<int>());
                        foreach (int yi in Enumerable.Range(0, n))
                        {
                            l[xi].Add(permutation[x + xi][y + yi]);
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
            wave.Add(new List<Dictionary<List<List<int>>, bool>> { });
            foreach (int y in Enumerable.Range(0, outputSizeY))
            {
                wave[x].Add(new Dictionary<List<List<int>>, bool> { });
                foreach (var pattern in patterns)
                {
                    wave[x][y][pattern.Key] = true;
                }
            }
        }

        foreach (int renderx in Enumerable.Range(0, input.Count))
        {
            foreach (int rendery in Enumerable.Range(0, input[0].Count))
            {
                GameObject sq = null;
                if (input[renderx][rendery] == 0)
                {
                    sq = Instantiate(blackSquare);
                }
                else if (input[renderx][rendery] == 1)
                {
                    sq = Instantiate(whiteSquare);
                }
                if (sq != null)
                {
                    sq.transform.position = new Vector3(renderx - outputSizeX / 2 - input.Count - 1, -rendery + outputSizeX / 2 + input[0].Count, 0);
                }
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
        void detectContradictions(List<List<Dictionary<List<List<int>>, bool>>> wave, out bool d, out int mx, out int my)
        {
            // Find the position with the most negentropy
            d = true;
            var ps = new List<Vector2>();
            mx = 0;
            my = 0;
            var maxscore = 0;
            var choiceIsValid = false;
            foreach (int y in Enumerable.Range(0, wave.Count))
            {
                foreach (int x in Enumerable.Range(0, wave[0].Count))
                {
                    var numberOfFalses = 0;
                    var numberOfTrues = 0;
                    foreach (var possibility in wave[y][x])
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
                        Debug.Log("We have encountered a contradiction at " + x.ToString() +", " + y.ToString() + "!");
                    }
                    if (numberOfTrues > 1)
                    {
                        if (!choiceIsValid || numberOfFalses > maxscore)
                        {
                            d = false;
                            ps = new List<Vector2>();
                            ps.Add(new Vector2(x, y));
                            maxscore = numberOfFalses;
                            choiceIsValid = true;
                        }
                        else if (numberOfFalses == maxscore)
                        {
                            ps.Add(new Vector2(x, y));
                        }
                    }
                }
            }
            if (choiceIsValid)
            {
                var v = ps[Random.Range(0, ps.Count)];
                mx = (int)v.x;
                my = (int)v.y;
            }
        }
        int maxx;
        int maxy;
        bool done;
        detectContradictions(wave, out done, out maxx, out maxy);
        offsetx = offsety = (int)Mathf.Floor(n / 2);

        if (!done)
        {
            // collapse the superposition

            Debug.Log("collapsing wavefunction at " + maxx.ToString() + ", " + maxy.ToString());
            if (maxx == 8 && maxy == 1)
            {
                Debug.Log("!!!");
            }
            var onesToMaybeKeep = from entry in wave[maxy][maxx]
                                  where entry.Value
                                  select entry.Key;
            var values = from key in onesToMaybeKeep
                        select patterns[key];
            var total = values.Sum();

            var oneToKeep = Random.Range(0, total-1);
            int i = 0;
            var patternToKeep = onesToMaybeKeep.ToList()[0];
            foreach (var maybe in onesToMaybeKeep)
            {
                i += patterns[maybe];
                if (i > oneToKeep)
                {
                    patternToKeep = maybe;
                }
            }


            foreach (var possibility in wave[maxy][maxx].Keys.ToArray())
            {
                if (possibility == patternToKeep)
                {
                    wave[maxy][maxx][possibility] = true;
                }
                else
                {
                    wave[maxy][maxx][possibility] = false;
                }
                detectContradictions(wave, out _, out _, out _);
            }

            // propagate updates
            var gas = 50;
            while (true)
            {
                detectContradictions(wave, out _, out _, out _);
                gas -= 1;
                bool madeAChange = false;
                foreach (int y in Enumerable.Range(0, wave.Count))
                {
                    foreach (int x in Enumerable.Range(0, wave[0].Count))
                    {
                        int deletions = 0;
                        var truePossibilities = from entry in wave[y][x]
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
                                    var posx = x + xr;
                                    var posy = y + yr;
                                    var color = possibility[yr][xr];
                                    foreach (int xi in Enumerable.Range(0, n))
                                    {
                                        foreach (int yi in Enumerable.Range(0, n))
                                        {
                                            if ((posx - xi) >= 0 && (posy - yi) >= 0 && (posx - xi) < wave[0].Count && (posy - yi) < wave.Count)
                                            {
                                                bool foundOne = false;
                                                foreach (var possibility2 in wave[posy - yi][posx - xi])
                                                {
                                                    if (possibility2.Value && possibility2.Key[yi][xi] == color)
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
                                deletions += 1;
                                detectContradictions(wave, out _, out _, out _);
                                wave[y][x][possibility] = false;
                                detectContradictions(wave, out _, out _, out _);
                                madeAChange = true;
                                if (contradictive) return;
                            }
                        }
                    }
                }
                if (!madeAChange || gas < 1) break;

            }

            foreach (var square in squares)
            {
                Destroy(square);
            }

            string s = "";
            int renderx = 0;
            foreach (var row in wave)
            {
                renderx++;
                int rendery = 0;
                foreach (var element in row)
                {
                    rendery++;
                    int singlePossibility = -1;
                    var posibilities = new List<int>();
                    int faffsdf = 0;
                    foreach (var possibility in element)
                    {
                        if (possibility.Value == true)
                        {
                            if (singlePossibility == -1)
                            {
                                singlePossibility = possibility.Key[offsety][offsetx];
                            }
                            else
                            {
                                posibilities.Add(faffsdf);
                            }
                        }
                        faffsdf++;
                    }
                    if (posibilities.Count > 1)
                    {
                        s = s + "s ";
                    }
                    else
                    {
                        GameObject sq = null;
                        if (singlePossibility == 0)
                        {
                            sq = Instantiate(blackSquare);
                        }
                        else if (singlePossibility == 1)
                        {
                            sq = Instantiate(whiteSquare);
                        }
                        if (sq != null)
                        {
                            sq.transform.position = new Vector3(renderx - outputSizeX / 2, -rendery + outputSizeX / 2, 0);
                            squares.Add(sq);
                        }
                        s = s + singlePossibility.ToString() + " ";
                    }
                }
                s = s + "\n";
            }
        
            Debug.Log(s.Replace("\n", System.Environment.NewLine));

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

}
