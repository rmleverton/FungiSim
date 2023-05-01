using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;


public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject tilePrefab2D, tilePrefab3D;
    public GameObject[] tiles;
    //public Tile[] tiles;
    [SerializeField]
    private int targetFrameRate = 1;

    public int width = 400, height, tileSize, tileRes = 10;
    private int ssnum = 0;

    public bool _3D = false;
    public bool recordAnimation = false;
    public bool tentacular = false;

    public int noiseSeed;

    // Start is called before the first frame update
    void Start()
    {
        int centreId = (int)((tileRes * tileRes) / 2 + tileRes / 2); 

        if (recordAnimation)
        {
            tileRes = (int)UnityEngine.Random.Range(10, 25);
            centreId = (int)UnityEngine.Random.Range(10, (tileRes * tileRes));
        }
        Application.targetFrameRate=targetFrameRate;

        noiseSeed = UnityEngine.Random.Range(0, 1000000);

        if (!_3D)
        {
            tileSize = width / tileRes;
            int numTiles = tileRes * tileRes;

            tiles = new GameObject[numTiles];

            for (int i = 0; i < numTiles; i++)
            {
                int x = i / tileRes;// * width;
                int z = i % tileRes;// * width;
                var tile = Instantiate(tilePrefab2D, new Vector3(x - tileRes / 2, 0, z - tileRes / 2), Quaternion.identity);
                tile.name = i.ToString();
                tiles[i] = tile;
                
                tile.GetComponent<Tile_2D>().Construct(i, Mathf.PerlinNoise((float)(x + 0.1f + noiseSeed), (float)(z + 0.1f + noiseSeed)), "bg", -1, tileSize, tileRes);
                //Debug.Log("x: " + x + " z: " + z + " noise: " + Mathf.PerlinNoise((float)(x + 0.5f), (float)(z + 0.5f)));

            }

            
            tiles[centreId].GetComponent<Tile_2D>().species = "node";
            tiles[centreId].GetComponent<Tile_2D>().val = 1;
            tiles[centreId].GetComponent<Tile_2D>().parentNodeId = centreId;
            tiles[centreId].GetComponent<Tile_2D>().parentId = centreId;

            foreach (GameObject tile in tiles)
            {
                tile.GetComponent<Tile_2D>().enabled = true;
            }
        }
        else
        {
            tileSize = width / tileRes;
            int numTiles = tileRes * tileRes * tileRes;

            tiles = new GameObject[numTiles];

            for (int i = 0; i < numTiles; i++)
            {
                int x = (i / tileRes) % tileRes;// * width;
                int y = i % tileRes;
                int z = i / (tileRes*tileRes);// * width;
                var tile = Instantiate(tilePrefab3D, new Vector3(x - tileRes / 2, y-tileRes/2, z - tileRes / 2), Quaternion.identity);
                tile.name = i.ToString();
                tiles[i] = tile;
                //tile.gameObject.tag = "Tile";
                //tile.layer = 6;
                tile.GetComponent<Tile3D>().Construct(i, PerlinNoise3D((float)(x + 0.1f),(float)(y + 0.01f), (float)(z + 0.1f)), "bg", -1, tileSize, tileRes);
                //Debug.Log("x: " + x + " z: " + z + " noise: " + Mathf.PerlinNoise((float)(x + 0.5f), (float)(z + 0.5f)));

            }



            //centreId = (int)(tileRes * tileRes * tileRes)/2 + (tileRes * tileRes) /2 + tileRes/2;
            centreId = (int)UnityEngine.Random.Range(0, tileRes * tileRes * tileRes);
            tiles[centreId].GetComponent<Tile3D>().species = "node";
            tiles[centreId].GetComponent<Tile3D>().val = 1;

            foreach (GameObject tile in tiles)
            {
                tile.GetComponent<Tile3D>().enabled = true;
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (recordAnimation)
        {
            if (_3D)
            {
                string name = "Frame_" + Time.frameCount.ToString() + "_" + tileRes.ToString() + "_" + noiseSeed.ToString()  + "_3D";
                ScreenCapture.CaptureScreenshot(name + ".png", 4);
                Debug.Log(name + "Screenshot Captured");
                new WaitForSeconds(0.01f);
            }
            
            else
            {
                string name = "Frame_" + Time.frameCount.ToString();
                int scalar = 20;
                Texture2D outputTex = new Texture2D((int)(tileRes * scalar), (int)(tileRes * scalar));
                for (int i = 0; i < tiles.Length; i++)
                {
                    int x = i / tileRes;
                    int y = i % tileRes;

                    float colourR = tiles[i].GetComponent<Tile_2D>().colour.r;
                    float colourG = tiles[i].GetComponent<Tile_2D>().colour.g;
                    float colourB = tiles[i].GetComponent<Tile_2D>().colour.b;
                    //colour += "\n";
                    //coloursString.Add(colourR+"," + colourG+ "," +colourB);
                    Color myCol = new Color(colourR, colourG, colourB, 1);
                    for (int k = 0; k < scalar; k++)
                    {
                        for (int l = 0; l < scalar; l++)
                        {
                            outputTex.SetPixel(x * scalar + k, y * scalar + l, myCol);
                        }
                    }

                }

                byte[] bytes = outputTex.EncodeToPNG();

                File.WriteAllBytes(name + ".png", bytes);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DateTime theTime = DateTime.Now;
            string date = theTime.ToString("yyyy-MM-dd\\Z");
            string time = theTime.ToString("HH:mm:ss\\Z");
            string datetime = theTime.ToString("yyyy-MM-dd\\THH-mm-ss\\Z");

            if (_3D)
            {
                

                ScreenCapture.CaptureScreenshot(datetime + ".png", 10);
                Debug.Log(datetime + "Screenshot Captured");
            }
            else
            {
                ssnum++;

                List<string> coloursString = new List<string>();

                int scalar = 50;
                Texture2D outputTex = new Texture2D((int)(tileRes * scalar), (int)(tileRes * scalar));
                for (int i = 0; i < tiles.Length; i++)
                {
                    int x = i / tileRes;
                    int y = i % tileRes;

                    float colourR = tiles[i].GetComponent<Tile_2D>().colour.r;
                    float colourG = tiles[i].GetComponent<Tile_2D>().colour.g;
                    float colourB = tiles[i].GetComponent<Tile_2D>().colour.b;
                    //colour += "\n";
                    //coloursString.Add(colourR+"," + colourG+ "," +colourB);
                    Color myCol = new Color(colourR, colourG, colourB, 1);
                    for (int k = 0; k < scalar; k++)
                    {
                        for (int l = 0; l < scalar; l++)
                        {
                            outputTex.SetPixel(x * scalar + k, y * scalar + l, myCol);
                        }
                    }

                }

                byte[] bytes = outputTex.EncodeToPNG();

                File.WriteAllBytes(datetime + "_texs.png", bytes);
                //File.WriteAllLines(datetime + ".txt", coloursString);
            }

        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Print");
            List<string> posVal = new List<string>();
            for (int i = 0; i < tiles.Length; i++)
            {
                int x = (i / tileRes) % tileRes;
                int y = i % tileRes;
                int z = i / (tileRes * tileRes);

                float val = tiles[i].GetComponent<Tile3D>().val;

                Vector4 thisPosVal = new Vector4(x, y, z, val);

                if (tentacular)
                {
                    if (tiles[i].GetComponent<Tile3D>().species == "node")
                    {
                        List<GameObject> tileSeekers = tiles[i].GetComponent<Tile3D>().seekers;
                        string tentacle = "";
                        for (int j = 0; j < tileSeekers.Count; j++)
                        {
                            string[] seeker = new string[4];
                            seeker[0] = tileSeekers[j].GetComponent<Tile3D>().idx.ToString();
                            seeker[1] = tileSeekers[j].GetComponent<Tile3D>().idy.ToString();
                            seeker[2] = tileSeekers[j].GetComponent<Tile3D>().idz.ToString();
                            seeker[3] = tileSeekers[j].GetComponent<Tile3D>().val.ToString();

                            tentacle+= seeker[0] + "," + seeker[1] + "," + seeker[2] + "," + seeker[3] + ":";

                            //posVal.Add(seeker[0].ToString() + "," + seeker[1].ToString() + "," + seeker[2].ToString() + "," + seeker[3].ToString());
                        }

                        posVal.Add(tentacle);
                        //sposVal.Add("OMG END");
                    }
                }
                else if (tiles[i].GetComponent<Tile3D>().species != "bg")
                {
                    posVal.Add(x.ToString() + "," + y.ToString() + "," + z.ToString() + "," + val.ToString());// thisPosVal.ToString());
                }

            }
                string name = "Frame_" + tileRes.ToString() + "_" + noiseSeed.ToString() + "_" + Time.frameCount.ToString() + "_3D";
            File.WriteAllLines(name + ".txt", posVal);
            Debug.Log(posVal);
           
        }


            if (Input.GetKeyDown(KeyCode.R))
        {
            for(int i = 0; i < tiles.Length; i++)
            {
                Destroy(tiles[i]);
            }
            Start();
        }
    }

    public static float PerlinNoise3D(float x, float y, float z)
    {
        y += 1;
        z += 2;
        float xy = _perlin3DFixed(x, y);
        float xz = _perlin3DFixed(x, z);
        float yz = _perlin3DFixed(y, z);
        float yx = _perlin3DFixed(y, x);
        float zx = _perlin3DFixed(z, x);
        float zy = _perlin3DFixed(z, y);
        return xy * xz * yz * yx * zx * zy;
    }
    static float _perlin3DFixed(float a, float b)
    {
        return Mathf.Sin(Mathf.PI * Mathf.PerlinNoise(a, b));
    }
}
