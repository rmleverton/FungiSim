using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class Tile_2D : MonoBehaviour
{

    public int id;
    public int parentNodeId;
    public int parentId;

    public float val;
    private float startingVal;

    public string species;

    public int idx, idy, idz;

    private bool dormant;

    public Color colour;


    public List<GameObject> neighbours, seekers;

    private Vector3 pos;
    private Vector3 dir = new Vector3(0, 0, 0);

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

        idx = (id / tileRes) % tileRes;// * width;
        idy = id % tileRes;
        idz = id / (tileRes * tileRes);
        pos = new Vector3(idx * tileSize, idy * tileSize, idz * tileSize);

        neighbours = new List<GameObject>();
        FindNeighbours();

        seekers = new List<GameObject>();

        mat = new Material(mat);


        nodeVal = nodeThreshold;
        colour = Color.black;

        val = startingVal;
    }

    // Update is called once per frame
    void Update()
    {
        float sz = Mathf.Log10(val + 1) / 3;
        //map(sz, 0, 5, 0, 1);
        //transform.localScale = new Vector3(sz, sz, sz);

        switch (species)
        {
            case "node":
                {
                    //sGetComponent<MeshRenderer>().enabled = true;
                    GetComponent<MeshRenderer>().material.color = colour;// Color.red;
                    break;
                }
            case "seeker":
                {

                    //GetComponent<MeshRenderer>().enabled = true;
                    GetComponent<MeshRenderer>().material.color = colour; // Color.blue;// * val;
                    break;
                }
            case "bg":
                {
                    GetComponent<MeshRenderer>().material.color = colour;// new Color(0, 0, 0, 0);
                    break;
                }
            default:
                {
                    //GetComponent<MeshRenderer>().enabled = false;
                    GetComponent<MeshRenderer>().material.color = colour;//  Color.white; //black;//
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
                //for (int z = -1; z <= 1; z++)
                //
                    if (x == 0 && y == 0)// && z == 0)
                    {
                        continue;
                    }
                    int nxId = idx + x;
                    int nyId = idy + y;
                // int nzId = idz + z;
                int nId = nxId * tileRes + nyId;// + (nzId * tileRes * tileRes);

                    if (nxId < 0 || nxId >= tileRes || nyId < 0 || nyId >= tileRes)// || nzId < 0 || nzId >= tileRes)
                    {
                        //console.log("This: " + this.idx + "," + this.idy + " Skip: " + nxId + "," + nyId);
                        continue;
                    }
                    else
                    {
                        neighbours.Add(gm.tiles[nId]);
                        num++;
                    }
                //}
            }

        }

    }

    void CreateSeeker()
    {
        if (species != "bg" && species != "dormantSeeker" && dormant == false)
        {
            //if (species == "seeker" && seekers.Count > 1)
            //{
            //    return;
            //}
            if (val - seekers.Count > 0.2f)
            {
                //Debug.Log("ID: " + id + " Val: " + val + " Seekers: " + seekers.Count + " val-seekers: " + (val - seekers.Count));

                //Find the highest value neighbour. If that neighbour is not bg
                //Find the next highest value. If none, then become dormant.
                if (idx + dir.x < tileRes && idx + dir.x >= 0 && idy + dir.y < tileRes && idy + dir.y >= 0)// && idz + dir.z < tileRes && idz + dir.z >= 0)
                {
                    int nIdFromVec = (int)((idx + dir.x) * tileRes + (idy + dir.y));// + (idz + dir.z) * (tileRes * tileRes));

                    List<GameObject> nDir = new List<GameObject>(gm.tiles[nIdFromVec].GetComponent<Tile_2D>().neighbours);
                    var dirNeighbours = nDir.Intersect(neighbours).ToList();
                    //Debug.Log("ID: " + id + " dirNeighbours Length: " + dirNeighbours.Count);

                    dirNeighbours = dirNeighbours.OrderByDescending(c => c.GetComponent<Tile_2D>().val).ToList();

                    for (int i = 0; i < dirNeighbours.Count; i++)
                    {
                        Tile_2D nTile = dirNeighbours[i].GetComponent<Tile_2D>();

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
                                gm.tiles[parentNodeId].GetComponent<Tile_2D>().seekers.Add(dirNeighbours[i]);
                            }

                            seekers.Add(dirNeighbours[i]);
                            nTile.parentId = id;
                            //val += nTile.val;
                            nTile.species = "dormantSeeker";
                            nTile.dir = new Vector2(nTile.idx - idx, nTile.idy - idy);// nTile.idz - idz);
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
        if (species == "dormantSeeker")
        {
            species = "seeker";
        }
    }

    bool CheckDormant()
    {
        bool isDormant = false;
        if (val <= 0)
        {
            isDormant = true;
        }

        return isDormant;
    }

    List<GameObject> FindChildren(int qID)
    {
        List<GameObject> children = new List<GameObject>();



        foreach (GameObject child in seekers)
        {
            Tile_2D childTile = child.GetComponent<Tile_2D>();
            List<GameObject> childSeekers = childTile.seekers;

            if (childSeekers.Count > 0)
            {
                List<GameObject> found = childTile.FindChildren(qID);

                children.Add(child);

                if (found.Count > 0)
                {
                    foreach (GameObject foundChild in found)
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
                    if (!neighbours.Any(neighbour => neighbour.GetComponent<Tile_2D>().species == "node"))
                    {
                        species = "node";
                        seekers = FindChildren(id);

                        Color seekerCol = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                        foreach (GameObject tile in seekers)
                        {
                            Tile_2D parentNode = gm.tiles[parentNodeId].GetComponent<Tile_2D>();
                            Tile_2D seeker = tile.GetComponent<Tile_2D>();

                            if (parentNode.seekers.Contains(tile))
                            {
                                parentNode.seekers.Remove(tile);
                                //Debug.Log(seeker.id);
                            }


                            seeker.parentNodeId = id;
                            seeker.colour = seekerCol;
                            colour = seekerCol;
                        }

                        parentNodeId = id;
                    }
                }



            }



            if (!dormant)
            {
                gm.tiles[parentId].GetComponent<Tile_2D>().val += startingVal;// * 0.1f;
                val -= startingVal * 0.5f;// 0.9f;
            }

            if (dormant)
            {

                //colour = Color.black;
                val = startingVal;
                species = "bg";


            }


        }
    }


    float map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

}
