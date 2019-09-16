using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WaveformcCollapse : MonoBehaviour
{
    public int n = 3;
    public int maxVal = 1;
    public int outputSizeX = 20;
    public int outputSizeY = 20;
    public List<List<int>> input = new List<List<int>>  {
        new List<int> { 1, 1, 1, 1, 1 },
        new List<int> { 1, 0, 0, 0, 0 },
        new List<int> { 1, 0, 1, 0, 0 },
        new List<int> { 1, 0, 0, 0, 0 },
        new List<int> { 1, 0, 0, 0, 0 },
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

    // Start is called before the first frame update
    void Start()
    {
        Random.seed = 42;
        squares = new List<GameObject>();
        patterns = new Dictionary<List<List<int>>, int>();
        wave = new List<List<Dictionary<List<List<int>>, bool>>>();
        foreach (int x in Enumerable.Range(0, input.Count - n + 1))
        {
            foreach (int y in Enumerable.Range(0, input[0].Count - n + 1))
            {
                var l = new List<List<int>>();
                foreach (int xi in Enumerable.Range(0, n))
                {
                    l.Add(new List<int>());
                    foreach (int yi in Enumerable.Range(0, n))
                    {
                        l[xi].Add(input[x + xi][y + yi]);
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
        steps++;
        void detectContradictions(List<List<Dictionary<List<List<int>>, bool>>> wave, out bool d, out int mx, out int my)
        {
            // Find the position with the most negentropy
            d = true;
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
                        Debug.Log("We have encountered a contradiction at " + x.ToString() +", " + y.ToString() + "!");
                    }
                    if (numberOfTrues > 1 && (!choiceIsValid || numberOfFalses > maxscore))
                    {
                        d = false;
                        mx = x;
                        my = y;
                        maxscore = numberOfFalses;
                        choiceIsValid = true;
                    }
                }
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
            var oneToKeep = Random.Range(0, wave[maxy][maxx].Count - 1);
            int i = -1;
            bool alreadySet = false;
            foreach (var possibility in wave[maxy][maxx].Keys.ToArray())
            {
                detectContradictions(wave, out _, out _, out _);
                i++;
                if (i >= oneToKeep && wave[maxy][maxx][possibility] && !alreadySet)
                {
                    alreadySet = true;
                    wave[maxy][maxx][possibility] = true;
                    detectContradictions(wave, out _, out _, out _);
                    continue;
                }
                wave[maxy][maxx][possibility] = false;
                detectContradictions(wave, out _, out _, out _);
            }

            // propagate updates
            var fdsaf = 0;
            while (true)
            {
                detectContradictions(wave, out _, out _, out _);
                fdsaf += 1;
                bool madeAChange = false;
                foreach (int y in Enumerable.Range(0, wave.Count))
                {
                    foreach (int x in Enumerable.Range(0, wave[0].Count))
                    {
                        var truePossibilities = from entry in wave[y][x]
                                                where entry.Value
                                                select entry.Key;
                        if (truePossibilities.Count() < 2)
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
                                detectContradictions(wave, out _, out _, out _);
                                wave[y][x][possibility] = false;
                                detectContradictions(wave, out _, out _, out _);
                                madeAChange = true;
                            }
                        }
                    }
                }
                if (!madeAChange || fdsaf > 100) break;

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
}
