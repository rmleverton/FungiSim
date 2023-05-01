using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class Tile : MonoBehaviour
{
    //class tileValComparer : IComparer
    //{
    //    public int Compare(object x, object y)
    //    {
    //        return (new Comparer()).Compare(((GameObject)x.(GameObject)y.GetComponent<Tile>().val);
    //    }
    //}
    public int id;
    public int parentNodeId;
    public int parentId;

    public float val;
    private float startingVal;

    public string species;

    public int idx, idy;

    private bool dormant;

    public Color colour;


    public List<GameObject> neighbours, seekers;

    private Vector2 pos;
    private Vector2 dir = new Vector2(0,0);

    public int tileRes = 10;
    public int tileSize = 10;

    [SerializeField]
    private GameManager gm;
    [SerializeField]
    private float nodeThreshold = 2.0f;
    private float nodeVal;

    [SerializeField]
    private Material mat;
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private MeshRenderer meshRenderer;

    public void Construct(int id, float val, string species, int parentNodeId, int tileSize, int tileRes)
    {
        this.id = id;

        this.startingVal = val;

        this.species = species;
        this.parentNodeId = parentNodeId;

        this.tileSize = tileSize;
        this.tileRes = tileRes;

    }
    // Start is called before the first frame update
    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        dormant = false;

        idx = (int)(id / tileRes);
        idy = id % tileRes;
        pos = new Vector2(idx * tileSize, idy * tileSize);

        neighbours = new List<GameObject>();
        FindNeighbours();

        seekers = new List<GameObject>();

        mat = new Material(mat);
        //GetComponent<MeshFilter>().mesh = mesh;
        //GetComponent<MeshRenderer>();
        //GetComponent<Material>();

        nodeVal = nodeThreshold;
        colour = Color.blue;

        val = startingVal;
    }

    // Update is called once per frame
    void Update()
    {

        switch (species)
        {
            case "node":
                {
                    GetComponent<MeshRenderer>().material.color = colour;// Color.red;
                    break;
                }
            case "seeker":
                {
                    GetComponent<MeshRenderer>().material.color = colour;// Color.blue;// * val;
                    break;
                }
            case "bg":
                {
                    GetComponent<MeshRenderer>().material.color = Color.black;// new Color(val, val, val);
                    break;
                }
            default:
                {
                    GetComponent<MeshRenderer>().material.color = Color.black;//
                    break;
                }
        }
        WakeSeeker();
        Develop();
        CreateSeeker();

        
        if (species != "bg")
        {
            dormant = CheckDormant();
        }


    }



    void FindNeighbours()
    {
        int num = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }
                int nxId = idx + x;
                int nyId = idy + y;
                int nId = nxId * tileRes + nyId;

                if (nxId < 0 || nxId >= tileRes || nyId < 0 || nyId >= tileRes)
                {
                    //console.log("This: " + this.idx + "," + this.idy + " Skip: " + nxId + "," + nyId);
                    continue;
                }
                else
                {
                    neighbours.Add(gm.tiles[nId]);
                    num++;
                }

            }

        }
        //Debug.Log(neighbours[0].GetComponent<Tile>().val);
        //Array.Sort(neighbours, delegate (GameObject a, GameObject b) { return a.GetComponent<Tile>().val.CompareTo(b.GetComponent<Tile>().val); });
        //Array.Reverse(neighbours);
        //neighbours = neighbours.OrderByDescending(c => c.GetComponent<Tile>().val).ToList();
        //Debug.Log(neighbours);
        //neighbours = sorted.Clone();
        //log("ID: " + this.id + " Neighbours: " + this._neighbours);
        //log("ID: " + this.id + " Neighbours: " + this._neighbours);
        //console.log(this._neighbours);
    }

    void CreateSeeker()
    {
        if (species != "bg" && species != "dormantSeeker" && dormant == false)
        {
            if (species == "seeker" && seekers.Count > 1)
            {
                return;
            }
            if (val - seekers.Count > 0f)
            {
                //Debug.Log("ID: " + id + " Val: " + val + " Seekers: " + seekers.Count + " val-seekers: " + (val - seekers.Count));

                //Find the highest value neighbour. If that neighbour is not bg
                //Find the next highest value. If none, then become dormant.
                if (idx + dir.x < tileRes && idx + dir.x >= 0 && idy + dir.y < tileRes && idy + dir.y >= 0)
                {
                    int nIdFromVec = (int)((idx + dir.x) * tileRes + (idy + dir.y));

                    List<GameObject> nDir = new List<GameObject>(gm.tiles[nIdFromVec].GetComponent<Tile>().neighbours);
                    var dirNeighbours = nDir.Intersect(neighbours).ToList();
                    //Debug.Log("ID: " + id + " dirNeighbours Length: " + dirNeighbours.Count);

                    dirNeighbours = dirNeighbours.OrderByDescending(c => c.GetComponent<Tile>().val).ToList();


                    for (int i = 0; i < dirNeighbours.Count; i++)
                    {
                        Tile nTile = dirNeighbours[i].GetComponent<Tile>();

                        if (nTile.species != "bg")
                        {
                            continue;
                        }
                        else if (nTile.species == "bg")
                        {

                            if (species == "node")
                            {
                                nTile.parentNodeId = id;

                            }
                            else if (species == "seeker")
                            {
                                nTile.parentNodeId = parentNodeId;
                                //gm.tiles[parentNodeId].GetComponent<Tile>().val += nTile.val;
                                gm.tiles[parentNodeId].GetComponent<Tile>().seekers.Add(dirNeighbours[i]);
                            }

                            seekers.Add(dirNeighbours[i]);
                            nTile.parentId = id;
                            //val += nTile.val;
                            nTile.species = "dormantSeeker";
                            nTile.dir = new Vector2(nTile.idx - idx, nTile.idy - idy);
                            nTile.colour = colour;

                           // Debug.Log("ID: " + id + "nID: " + nTile.id + " , " + Time.frameCount);
                            //console.log("ID: " + this.id + " Seeker id: " + this._neighbours[i].id);
                            break;
                        }
                    }
                }


            }
            //console.log(this._neighbours);
           

        }
    }

    void WakeSeeker()
    {
        if(species == "dormantSeeker")
        {
            species = "seeker";
        }
    }

    bool CheckDormant()
    {
        bool isDormant = false;
        if(val <= 0)
        {
            isDormant = true;
        }
        //if (seekers.Count == 0)
        //{
        //    isDormant = true;
        //    //console.log(this._neighbours);
        //    for (int i = 0; i < neighbours.Count; i++)
        //    {
        //        Tile nTile = neighbours[i].GetComponent<Tile>();

        //        if (nTile.species != "bg")
        //        {
        //            isDormant = true;
        //        }
        //        else if (nTile.species == "bg")
        //        {
        //            isDormant = false;
        //            //console.log("ID: " + this.id + "NOT DORMANT");
        //            return isDormant;
        //        }
        //    }
        //}


        return isDormant;
    }

    List<GameObject> FindChildren(int qID)
    {
        List<GameObject> children = new List<GameObject>();

        
        
        foreach (GameObject child in seekers)
        {
            Tile childTile = child.GetComponent<Tile>();
            List<GameObject> childSeekers = childTile.seekers;

            if (childSeekers.Count > 0)
            {
                List<GameObject> found = childTile.FindChildren(qID);

                children.Add(child);

                if(found.Count > 0)
                {
                    foreach(GameObject foundChild in found)
                    {
                        children.Add(foundChild);
                    }
                }
            }
        }

        //Debug.Log("ID: " + id + " qID: " + qID + " Seekers Length: " + seekers.Count + " Children: " + children.Count);
        return children;
    }

    void Develop()
    {
        if (species != "bg")
        {
            //Debug.Log("ID: " + id + " Species: " + species + " Val: " + val + " nodeVal: " + nodeVal);
            if (species == "seeker")
            {
                if (val >= nodeVal)
                {
                    if (!neighbours.Any(neighbour => neighbour.GetComponent<Tile>().species == "node"))
                    {
                        species = "node";
                        seekers = FindChildren(id);

                        Color seekerCol = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                        foreach (GameObject tile in seekers)
                        {
                            Tile parentNode = gm.tiles[parentNodeId].GetComponent<Tile>();
                            Tile seeker = tile.GetComponent<Tile>();

                            if (parentNode.seekers.Contains(tile))
                            {
                                parentNode.seekers.Remove(tile);
                                //Debug.Log(seeker.id);
                            }


                            seeker.parentNodeId = id;
                            seeker.colour = seekerCol;
                            colour = seekerCol;
                        }
                        //List<GameObject> parentSeekersList = gm.tiles[parentNodeId].GetComponent<Tile>().seekers;
                        ////var parentSeekers = gm.tiles[parentNodeId].GetComponent<Tile>().seekers.Concat(seekers.Where(f => !seekers.Contains(f))).ToList();
                        //var parentSeekers = seekers.Concat(parentSeekersList.Except(seekers)).ToList();

                        //gm.tiles[parentNodeId].GetComponent<Tile>().seekers = parentSeekers;
                        parentNodeId = id;
                    }
                }



            }

            //if(species == "node")
            //{
            //    if(val <= 0)
            //    {
            //        val = startingVal;
            //        species = "bg";
            //    }
            //}

            if (!dormant)
            {
                gm.tiles[parentNodeId].GetComponent<Tile>().val += startingVal;// * 0.1f;
                val -= startingVal / 2;// 0.9f;
            }

            if (dormant)
            {
                //species = "bg";
                //val -= 0.1f;
                //colour = colour;// Color.black;
                val = startingVal;
                species = "bg";
                //gm.tiles[parentNodeId].GetComponent<Tile>().seekers.Remove(gm.tiles[id]);
                //gm.tiles[parentId].GetComponent<Tile>().seekers.Remove(gm.tiles[id]);
                //seekers.Clear();

            }
            //if (this.species == "node")
            //{

            //    float lastVal = seekers[seekers.Count - 1].GetComponent<Tile>().val;


            //    for (int i = 0; i < seekers.Count - 1; i++)
            //    {

            //        seekers[i].GetComponent<Tile>().val += lastVal;
            //    }
            //}
        }
    }

}
