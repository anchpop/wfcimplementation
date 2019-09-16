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
        new List<int> { 1, 0, 0, 0, 0 },
        new List<int> { 1, 0, 0, 0, 0 },
        new List<int> { 1, 0, 0, 0, 0 },
    };

    private Dictionary<List<List<int>>, int> patterns;
    private List<List<Dictionary<List<List<int>>,bool>>> wave;

    public GameObject blackSquare;
    public GameObject whiteSquare;
    List<GameObject> squares;

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
        // Find the position with the most negentropy
        var maxx = 0;
        var maxy = 0;
        var maxscore = 0;
        var choiceIsValid = false;
        foreach (int x in Enumerable.Range(0, wave.Count))
        {
            foreach (int y in Enumerable.Range(0, wave[0].Count))
            {
                var numberOfFalses = 0;
                var numberOfTrues = 0;
                foreach (var possibility in wave[x][y])
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
                    Debug.Log("We have encountered a contradiction!");
                }
                if (numberOfTrues > 1 && (!choiceIsValid || numberOfFalses > maxscore))
                {
                    maxx = x;
                    maxy = y;
                    maxscore = numberOfFalses;
                    choiceIsValid = true;
                }
            }
        }

        // collapse the superposition
        var oneToKeep = Random.Range(0, wave[maxx][maxy].Count - 1);
        int i = -1;
        bool alreadySet = false;
        foreach (var possibility in wave[maxx][maxy].Keys.ToArray())
        {
            i++;
            if (i >= oneToKeep && wave[maxx][maxy][possibility] && !alreadySet)
            {
                alreadySet = true;
                wave[maxx][maxy][possibility] = true;
                continue;
            }
            wave[maxx][maxy][possibility] = false;
        }

        // propagate updates
        var fdsaf = 0;
        while (true)
        {
            fdsaf += 1;
            bool madeAChange = false;
            foreach (int x in Enumerable.Range(0, wave.Count))
            {
                foreach (int y in Enumerable.Range(0, wave[0].Count))
                {
                    foreach (var possibility in wave[x][y].Keys.ToArray())
                    {
                        if (wave[x][y][possibility])
                        {
                            foreach (int xr in Enumerable.Range(0, n))
                            {
                                foreach (int yr in Enumerable.Range(0, n))
                                {
                                    var posx = x + xr;
                                    var posy = y + yr;
                                    var color = possibility[xr][yr];
                                    bool colorCanStay = true;
                                    foreach (int xi in Enumerable.Range(0, n))
                                    {
                                        foreach (int yi in Enumerable.Range(0, n))
                                        {
                                            if ((posx - xi) >= 0 && (posy - yi) >= 0 && (posx - xi) < possibility.Count && (posy - yi) < possibility[0].Count)
                                            {
                                                bool foundOne = false;
                                                foreach (var possibility2 in wave[posx - xi][posy - yi])
                                                {
                                                    if (possibility2.Value && possibility2.Key[xi][yi] == color)
                                                    {
                                                        foundOne = true;
                                                        break;
                                                    }
                                                }
                                                if (!foundOne)
                                                {
                                                    colorCanStay = false;
                                                }
                                            }
                                        }
                                    }
                                    if (!colorCanStay)
                                    {
                                        wave[x][y][possibility] = false;
                                        madeAChange = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!madeAChange || fdsaf > 100) break;
        }

        foreach(var square in squares)
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
                            singlePossibility = possibility.Key[0][0];
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

        Debug.Log(wave);
        Debug.Log(s.Replace("\n", System.Environment.NewLine));
    }
}
