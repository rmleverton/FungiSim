using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using SimplexNoise;
using Newtonsoft.Json;
using System.IO;
using OpenCover.Framework.Model;

public class CellManager : MonoBehaviour
{
    // Cells array
    private Cell[] cells_array;
    private List<Cell> active_cells;

    //  Parameters
    private float[] parameters;
    [SerializeField]
    private float growth_value = 0.5f;
    [SerializeField]
    private float value_scalar = 0.01f;
    [SerializeField]
    private float parent_pass_scalar = 0.5f;
    [SerializeField]
    private float value_change_scalar = 0.5f;
    [SerializeField]
    private float growth_scalar = 2.0f;
    [SerializeField]
    private float cell_upkeep_value = 0.1f;
    [SerializeField]
    private float node_transform_value = 2.0f;

    // Noise Management
    [SerializeField]
    private float noise_scale = 0.1f;
    private int noise_seed;

    // Appearence
    [SerializeField]
    private Mesh cell_mesh;
    [SerializeField]
    private Material cell_material;

    // Grid setup values
    private float WSx, WSy, WSz; // World Space XYZ size of the grid
    [SerializeField]
    private int grid_res_x, grid_res_y, grid_res_z;

    // Setup Variables
    [SerializeField]
    private int targetFrameRate = 30;
    [SerializeField]
    private bool recordAnimation = false;
    [SerializeField]
    private string animation_name;

    // Camera Management
    [SerializeField]
    private Rotate_Script camera_rotate;
    [SerializeField]
    private bool rotate_camera = true;
    private float total_frames;

    // Start is called before the first frame update
    void Start()
    {
        // Setup the background
        Application.targetFrameRate = targetFrameRate;
        noise_seed = UnityEngine.Random.Range(0, 1000000);

        if (rotate_camera)
        {
            camera_rotate.enabled = true;
            total_frames = camera_rotate.Total_Frames;
            Debug.Log(total_frames);
        }

        // Setup the grid
        int num_tiles = grid_res_x * grid_res_y * grid_res_z;

        cells_array = new Cell[num_tiles];
        active_cells = new List<Cell>();

        parameters = new float[] { growth_value, parent_pass_scalar, value_change_scalar, growth_scalar, cell_upkeep_value, node_transform_value };

        // We move things around the world orgin later so need to calculate the centre
        Vector3 centre_position = new Vector3 ((float)(grid_res_x / 2), (float)(grid_res_y / 2), (float)(grid_res_z / 2) );
        float max_distance = Vector3.Distance(centre_position, new Vector3(grid_res_x , grid_res_y, grid_res_z));

        for(int i = 0; i < num_tiles; i++)
        {
            
            //currentSlice* totalRows *totalColumns + currentRow * totalColumns + currentColumn + 1
            //column = (id - 1) % totalColumns
            //row = ((id - column - 1) / totalColumns) % totalRows
            //slice = (((id - column - 1) / totalColumns) - row) / totalRows

            int cell_pos_y = i % grid_res_y;
            int cell_pos_x = ((i - cell_pos_y) / grid_res_y) % grid_res_x;
            int cell_pos_z = (((i - cell_pos_y) / grid_res_y) - cell_pos_x) / grid_res_x;

            // Centre to the world
            Vector3 cell_position = new Vector3(cell_pos_x - grid_res_x / 2, cell_pos_y - grid_res_y / 2, cell_pos_z - grid_res_z / 2);

            // Create our game object and add the relevant components
            GameObject cell_object = new GameObject();
            cell_object.name = "cell_" + i.ToString() + "_" + cell_pos_x.ToString() + "_" + cell_pos_y.ToString() + "_" + cell_pos_z.ToString();

            Cell new_cell = cell_object.AddComponent<Cell>();
            new_cell.Cell_Mesh = cell_mesh;
            new_cell.Cell_Material = cell_material;
            new_cell.Cell_XYZ = new int[] { cell_pos_x, cell_pos_y, cell_pos_z };

            // Calculate cell values as a number between 0 - 1
            float generated_noise = SimplexNoise.Noise.CalcPixel3D(cell_pos_x + noise_seed, cell_pos_y + noise_seed, cell_pos_z + noise_seed, noise_scale);
            float init_cell_value = SimplexNoise.Noise.Map(generated_noise, 0.0f, 256.0f, 0.0f, 1.0f);

            // Calculate a drop-off
            float distance = Vector3.Distance(Vector3.zero, cell_position) ;
            distance = max_distance-distance;
            distance /= max_distance;
            distance = -Mathf.Log(distance);
            if (distance > 2.71828f)
            {

                distance = 2.71828f;
            }
            distance /= 2.71828f;
            distance = 1 - distance;

            init_cell_value *= distance;

            // Pass all the relevant variables to the create function
            new_cell.Create(i, cell_position, init_cell_value, this, parameters, distance);
            
            cells_array[i] = new_cell;
  
        }

        // Find a random id nearer the middle so it is surrounded by high enough value cells
        int source_x = (int)(UnityEngine.Random.Range(0, grid_res_x / 2) + grid_res_x/4);
        int source_y = (int)(UnityEngine.Random.Range(0, grid_res_y / 2) + grid_res_y / 4);
        int source_z = (int)(UnityEngine.Random.Range(0, grid_res_z / 2) + grid_res_z / 4);
        int source_id = (int)(source_x * grid_res_y + source_y + (source_z * grid_res_x * grid_res_y));

        Debug.Log("X: " + source_x + ", Y: " + source_y + ", Z: " + source_z);

        // Set the relevant cell
        cells_array[source_id].Cell_Species ="node";
        cells_array[source_id].Cell_Value = 2.0f;
        active_cells.Add(cells_array[source_id]);

        // Tell each cell to find its neighbours and order them
        foreach (Cell cell in cells_array)
        {
            cell.Find_Neighbours();
        }

        Debug.Log("Done Neighbours: " + Time.realtimeSinceStartup + "s");

    }

    // Update is called once per frame
    void Update()
    {
        // Update each cell in the active array
        for(int i = 0; i < active_cells.Count; i++)
        {
            active_cells[i].Update_Cell();
        }

        // Create an output list of all our living / ossified cells
        if (Input.GetKeyDown(KeyCode.P))
        {
            Create_Tentacle_List();
        }

        // Option to record the animation to specified folder
        if (recordAnimation)
        {
            if (Time.frameCount < total_frames)
            {
                string name = "D:/In_Organisms/Images/CellRenders/" + animation_name + "/" + noise_seed.ToString() + "Frame_" + Time.frameCount.ToString();
                ScreenCapture.CaptureScreenshot(name + ".png", 4);
                Debug.Log(name + " Screenshot Captured");
                new WaitForSeconds(0.01f); // add a little delay just to make sure it does the screencapture before moving to the next frame
            }
            else
            {
                Debug.Log("Recording Complete");
            }
        }
    }

    private void Create_Tentacle_List()
    {
        // Create a list to store the data in
        List<Tentacle> tentacle_list = new List<Tentacle>();

        for (int i = 0; i < active_cells.Count; i++)
        {
            // create a list for each node / tentacle
            List<Tentacle_Cell> cell_list = new List<Tentacle_Cell>();

            // Pass the list through the whole tentacle
            active_cells[i].Cell_Report(ref cell_list);

            // Cast it into a new Tentacle class
            Tentacle current_tentacle = new Tentacle
            {
                tentacle_id = i,
                tentacle_cells = cell_list

            };

            // Add the tentacle to our list
            tentacle_list.Add(current_tentacle);
        }


        // When the list is complete, turn it into a json file
        string json = JsonConvert.SerializeObject(tentacle_list, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        System.IO.File.WriteAllText("D:/Unity/FungiSim/SavedStructureData/testjson.JSON", json);
    }

    public Vector3 Grid_Resolution
    {
        get
        {
            return new Vector3(grid_res_x, grid_res_y, grid_res_z);
        }
    }

    public Cell[] Cells_Array
    {
        get
        {
            return cells_array;
        }
    }

    public void Add_Node(Cell new_node)
    {
        active_cells.Add(new_node);
    }
}

// A class to hold tentacle cell information
public class Tentacle_Cell
{
    public int cell_id { get; set; }
    public int[] cell_position { get; set; }
    public float cell_value { get; set; }
}

// A class to hold tentacle information
public class Tentacle
{
    public int tentacle_id { get; set; }
    public List<Tentacle_Cell> tentacle_cells { get; set; }
}
