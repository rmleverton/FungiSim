using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using SimplexNoise;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class CellManagerGPU : MonoBehaviour
{
    // Cells array
    //private Cell[] cells_array;
    private Cell_Struct[] cells_array;
    //private List<Cell> active_cells;
    private List<Cell_Struct> active_cells;

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
    [SerializeField]
    private int targetFrameRate = 10;

    // Appearence
    [SerializeField]
    private Mesh cell_mesh;
    [SerializeField]
    private Material cell_material;

    // Grid setup values
    private float WSx, WSy, WSz; // World Space XYZ size of the grid
    [SerializeField]
    private int grid_res_x, grid_res_y, grid_res_z;

    private Vector3[] cell_positions_array;
    private int[] cell_type_array;
    private float[] cell_value_array;
    private int[] cell_neighbours_array;
    private int[] cell_children_array;
    private int[] cell_parent_array;
    private Vector4[] cell_colour_array;

    [SerializeField]
    private ComputeShader cell_shader;
    private ComputeBuffer cell_positions_buffer, cell_type_buffer, cell_value_buffer, cell_neighbours_buffer, cell_children_buffer, cell_parent_buffer, cell_colour_buffer, arg_buffer;
    private int kernel_1, kernel_2;

    Bounds bounds;

    private Matrix4x4[] transform_array;

    private int num_tiles;
    struct Cell_Struct
    {
        public int cell_id;
        public int cell_type;
        public float cell_value;
        public int[] cell_neighbours;
        public int[] cell_child;
        public int cell_parent;
        public float[] cell_colour;
        //int[3] cell_position;


        public Cell_Struct(int cell_id, int cell_type, float cell_value, int[] cell_neighbours)
        {
            this.cell_id = cell_id;
            this.cell_type = cell_type; //0=background, 1=node, 2=child, 3=bone
            this.cell_value = cell_value;  
            this.cell_neighbours = cell_neighbours;
            this.cell_child = Enumerable.Repeat(-1, 26).ToArray();
            this.cell_parent = -1;
            this.cell_colour = new float[4] { 0, 0, 0, 1 };
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //Application.targetFrameRate = targetFrameRate;

        num_tiles = grid_res_x * grid_res_y * grid_res_z;

        transform_array = new Matrix4x4[num_tiles];

        cells_array = new Cell_Struct[num_tiles];
        active_cells = new List<Cell_Struct>();


        cell_positions_array = new Vector3[num_tiles];

        cell_type_array = new int[num_tiles];
        cell_value_array = new float[num_tiles];
        cell_neighbours_array = new int[num_tiles * 26];
        cell_children_array = new int[num_tiles * 26];
        cell_parent_array = new int[num_tiles];
        cell_colour_array = new Vector4[num_tiles];


        parameters = new float[] { growth_value, parent_pass_scalar, value_change_scalar, growth_scalar, cell_upkeep_value, node_transform_value };

        Vector3 centre_position = new Vector3((float)(grid_res_x / 2), (float)(grid_res_y / 2), (float)(grid_res_z / 2));
        float max_distance = Vector3.Distance(centre_position, new Vector3(grid_res_x, grid_res_y, grid_res_z));
        for (int i = 0; i < num_tiles; i++)
        {

            //currentSlice* totalRows *totalColumns + currentRow * totalColumns + currentColumn + 1
            //column = (id - 1) % totalColumns
            //row = ((id - column - 1) / totalColumns) % totalRows
            //slice = (((id - column - 1) / totalColumns) - row) / totalRows

            int cell_pos_y = i % grid_res_y;
            int cell_pos_x = ((i - cell_pos_y) / grid_res_y) % grid_res_x;
            int cell_pos_z = (((i - cell_pos_y) / grid_res_y) - cell_pos_x) / grid_res_x;

            Vector3 cell_position = new Vector3(cell_pos_x - grid_res_x / 2, cell_pos_y - grid_res_y / 2, cell_pos_z - grid_res_z / 2);

            transform_array[i] = Matrix4x4.TRS(cell_position, Quaternion.identity, Vector3.one);
            cell_positions_array[i] = cell_position;
            float generated_noise = SimplexNoise.Noise.CalcPixel3D(cell_pos_x, cell_pos_y, cell_pos_z, noise_scale);


            float init_cell_value = SimplexNoise.Noise.Map(generated_noise, 0.0f, 256.0f, 0.0f, 1.0f);

            float distance = Vector3.Distance(Vector3.zero, cell_position);


            distance = max_distance - distance;

            distance /= max_distance;

            distance = -Mathf.Log(distance);


            if (distance > 2.71828f)
            {

                distance = 2.71828f;
            }
            distance /= 2.71828f;
            distance = 1 - distance;
            init_cell_value *= distance;

            int[] cell_neighbours = Enumerable.Repeat(-1, 26).ToArray(); 

            int count = 0;
            // Iterate over all possible neighboring points
            for (int x = cell_pos_x - 1; x <= cell_pos_x + 1; x++)
            {
                if (x < 0 || x >= grid_res_x)
                {
                    cell_neighbours[count] = -1;
                    count++;
                    continue;
                }
                for (int y = cell_pos_y - 1; y <= cell_pos_y + 1; y++)
                {
                    if (y < 0 || y >= grid_res_y)
                    {
                        cell_neighbours[count] = -1;
                        count++;
                        continue;
                    }
                    for (int z = cell_pos_z - 1; z <= cell_pos_z + 1; z++)
                    {
                        if (z < 0 || z >= grid_res_z)
                        {
                            cell_neighbours[count] = -1;
                            count++;
                            continue;
                        }
                        // Skip the current point
                        if (x == cell_pos_x && y == cell_pos_y && z == cell_pos_z)
                        {
                            continue;
                        }
                        int nId = (int)(x * grid_res_y + y + (z * grid_res_x * grid_res_y));

                        // Add the neighboring point to the list
                        cell_neighbours[count] = nId;
                        count++;
                    }
                }
            }

            Cell_Struct new_cell = new Cell_Struct(i, 0, init_cell_value, cell_neighbours);

            cells_array[i] = new_cell;

            cell_type_array[i] = 0;
            cell_value_array[i] = init_cell_value;
            for (int j = 0; j < 26; j++)
            {
                int num = i * 26 + j;
                cell_neighbours_array[num] = cell_neighbours[j];
                cell_children_array[num] = -1;
            }
            cell_parent_array[i] = -1;
            cell_colour_array[i] = new Vector4( 0, 0, 0, 0 );


        }

        int source_x = (int)(UnityEngine.Random.Range(0, grid_res_x / 2) + grid_res_x / 4);
        int source_y = (int)(UnityEngine.Random.Range(0, grid_res_y / 2) + grid_res_y / 4);
        int source_z = (int)(UnityEngine.Random.Range(0, grid_res_z / 2) + grid_res_z / 4);
        int source_id = (int)(source_x * grid_res_y + source_y + (source_z * grid_res_x * grid_res_y));

        Debug.Log("X: " + source_x + ", Y: " + source_y + ", Z: " + source_z);

        //cells_array[source_id].cell_type = 1;
        cell_type_array[source_id] = 1;
        cell_value_array[source_id] = 2.0f;
        //cells_array[source_id].cell_value = 2.0f;
        //active_cells.Add(cells_array[source_id]);


        kernel_1 = cell_shader.FindKernel("CSMain");
        kernel_2 = cell_shader.FindKernel("CSNeighbours");



        cell_type_buffer = new ComputeBuffer(num_tiles, sizeof(int));
        cell_type_buffer.SetData(cell_type_array);

        cell_value_buffer = new ComputeBuffer(num_tiles, sizeof(float));
        cell_value_buffer.SetData(cell_value_array);

        cell_neighbours_buffer = new ComputeBuffer(num_tiles * 26, sizeof(int));
        cell_neighbours_buffer.SetData(cell_neighbours_array);

        cell_children_buffer = new ComputeBuffer(num_tiles * 26, sizeof(int));
        cell_children_buffer.SetData(cell_children_array);

        cell_parent_buffer = new ComputeBuffer(num_tiles, sizeof(int));
        cell_parent_buffer.SetData(cell_parent_array);

        cell_colour_buffer = new ComputeBuffer(num_tiles, sizeof(float) * 4);
        //cell_colour_buffer.SetData(cell_colour_array);

        arg_buffer = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)cell_mesh.GetIndexCount(0);
        args[1] = (uint)cell_type_buffer.count;
        args[2] = (uint)cell_mesh.GetIndexStart(0);
        args[3] = (uint)cell_mesh.GetBaseVertex(0);
        arg_buffer.SetData(args);



        bounds = new Bounds(Vector3.zero, new Vector3(grid_res_x, grid_res_y, grid_res_z));
        

        int[] grid_res = new int[3] { grid_res_x, grid_res_y, grid_res_z };
        cell_shader.SetInts("grid_res", grid_res);

        cell_shader.SetBuffer(kernel_1, "cell_type_buffer", cell_type_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_value_buffer", cell_value_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_neighbours_buffer", cell_neighbours_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_children_buffer", cell_children_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_parent_buffer", cell_parent_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_colour_buffer", cell_colour_buffer);


        cell_shader.SetFloat("grow_cell_value", parameters[0]);
        cell_shader.SetFloat("cell_upkeep_value", parameters[4]);
        cell_shader.SetFloat("parent_pass_scalar", parameters[1]);
        cell_shader.SetFloat("node_transform_value", parameters[5]);
        cell_shader.SetBuffer(kernel_1, "initial_cell_values", cell_value_buffer);

        cell_shader.Dispatch(kernel_1, grid_res_x, grid_res_y, grid_res_z);


        int[] temp_neighbours = new int[26];
        temp_neighbours = Enumerable.Repeat(-1, 26).ToArray();
        float[] neighbour_values = new float[26];
        neighbour_values = Enumerable.Repeat(0.0f, 26).ToArray();

        ComputeBuffer temp_neighbours_buffer = new ComputeBuffer(26, sizeof(int));
        ComputeBuffer temp_neighbour_values_buffer = new ComputeBuffer(26, sizeof(float));
        cell_shader.SetBuffer(kernel_2, "my_neighbours", temp_neighbours_buffer);
        cell_shader.SetBuffer(kernel_2, "neighbour_values", temp_neighbour_values_buffer);
        //cell_shader.SetBuffer(kernel_2, "cell_colour_buffer", cell_colour_buffer);
        cell_shader.SetBuffer(kernel_2, "cell_value_buffer", cell_value_buffer);
        cell_shader.SetBuffer(kernel_2, "cell_neighbours_buffer", cell_neighbours_buffer);

        cell_shader.Dispatch(kernel_2, grid_res_x, grid_res_y, grid_res_z);
        int[] sortedNeighbours = new int[num_tiles * 26];
        cell_neighbours_buffer.GetData(sortedNeighbours);

        temp_neighbours_buffer.Release();
        temp_neighbour_values_buffer.Release();

        Debug.Log("Done Neighbours: " + Time.realtimeSinceStartup + "s");
        //for (int i = 0; i < num_tiles * 26; i++)
        //{
        //    Debug.Log("Cell: " + i/26 + " N: " + sortedNeighbours[i]);
        //}


        //cell_positions_buffer = new ComputeBuffer(num_tiles, sizeof(float) * 3);
        //cell_positions_buffer.SetData(cell_positions_array);


        //cell_material.SetBuffer("position_buffer", cell_positions_buffer);


    }

    // Update is called once per frame
    void Update()
    {
        cell_type_buffer.Release();
        cell_value_buffer.Release();
        cell_neighbours_buffer.Release();
        cell_children_buffer.Release();
        cell_parent_buffer.Release();
        cell_colour_buffer.Release();
        //cell_positions_buffer.Release();
        arg_buffer.Release();

        cell_type_buffer = new ComputeBuffer(num_tiles, sizeof(int));
        cell_type_buffer.SetData(cell_type_array);

        cell_value_buffer = new ComputeBuffer(num_tiles, sizeof(float));
        cell_value_buffer.SetData(cell_value_array);

        cell_neighbours_buffer = new ComputeBuffer(num_tiles * 26, sizeof(int));
        cell_neighbours_buffer.SetData(cell_neighbours_array);

        cell_children_buffer = new ComputeBuffer(num_tiles * 26, sizeof(int));
        cell_children_buffer.SetData(cell_children_array);

        cell_parent_buffer = new ComputeBuffer(num_tiles, sizeof(int));
        cell_parent_buffer.SetData(cell_parent_array);

        cell_colour_buffer = new ComputeBuffer(num_tiles, sizeof(float) * 4);
        cell_colour_buffer.SetData(cell_colour_array);

        arg_buffer = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)cell_mesh.GetIndexCount(0);
        args[1] = (uint)cell_type_buffer.count;
        args[2] = (uint)cell_mesh.GetIndexStart(0);
        args[3] = (uint)cell_mesh.GetBaseVertex(0);
        arg_buffer.SetData(args);

        int[] grid_res = new int[3] { grid_res_x, grid_res_y, grid_res_z };
        cell_shader.SetInts("grid_res", grid_res);

        cell_shader.SetBuffer(kernel_1, "cell_type_buffer", cell_type_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_value_buffer", cell_value_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_neighbours_buffer", cell_neighbours_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_children_buffer", cell_children_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_parent_buffer", cell_parent_buffer);
        cell_shader.SetBuffer(kernel_1, "cell_colour_buffer", cell_colour_buffer);


        cell_shader.SetFloat("grow_cell_value", parameters[0]);
        cell_shader.SetFloat("cell_upkeep_value", parameters[4]);
        cell_shader.SetFloat("parent_pass_scalar", parameters[1]);
        cell_shader.SetFloat("node_transform_value", parameters[5]);
        cell_shader.SetBuffer(kernel_1, "initial_cell_values", cell_value_buffer);

        cell_shader.Dispatch(kernel_1, grid_res_x, grid_res_y, grid_res_z);

        cell_type_buffer.GetData(cell_type_array);

        cell_value_buffer.GetData(cell_value_array);

        cell_neighbours_buffer.GetData(cell_neighbours_array);

        cell_children_buffer.GetData(cell_children_array);

        cell_parent_buffer.GetData(cell_parent_array);
        cell_colour_buffer.GetData(cell_colour_array);

        List<Vector3> pos_list = new List<Vector3>();

        //cell_type_buffer.GetData(cell_type_array);
        for (int i = 0; i < cell_type_array.Length; i++)
        {
            if (cell_type_array[i] != 0)
            {
                pos_list.Add(cell_positions_array[i]);
            }
        }

        cell_positions_buffer = new ComputeBuffer(pos_list.Count, sizeof(float) * 3);
        cell_positions_buffer.SetData(pos_list);


        cell_material.SetBuffer("position_buffer", cell_positions_buffer);


        cell_material.SetBuffer("colour_buffer", cell_colour_buffer);
        Graphics.DrawMeshInstancedIndirect(cell_mesh, 0, cell_material, bounds, arg_buffer);
        //for (int i = 0; i < active_cells.Count; i++)
        //{
        //    active_cells[i].Update_Cell();
        //}

        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    Create_Tentacle_List();
        //}
        cell_positions_buffer.Release();

        Debug.Log("Cell: " + 0 + " T: " + pos_list.Count);

        //for (int i = 0; i < num_tiles; i++)
        //{
        //    Debug.Log("Cell: " + i + " T: " + cell_type_array[i]);
        //}
    }

    void OnDisable()
    {
        cell_type_buffer.Release();
        cell_value_buffer.Release();
        cell_neighbours_buffer.Release();
        cell_children_buffer.Release();
        cell_parent_buffer.Release();
        cell_colour_buffer.Release();
        cell_positions_buffer.Release();
        arg_buffer.Release();
        cell_type_buffer = null;
        cell_value_buffer = null;
        cell_neighbours_buffer = null;
        cell_children_buffer = null;
        cell_parent_buffer = null;
        cell_colour_buffer = null;
        cell_positions_buffer = null;
        arg_buffer = null;
    }
    //private void Create_Tentacle_List()
    //{
    //    //Debug.Log(active_cells.Count);
    //    //List<Vector3>[] tentacle_array = new List<Vector3>[active_cells.Count];

    //    List<Tentacle> tentacle_list = new List<Tentacle>();

    //    for (int i = 0; i < active_cells.Count; i++)
    //    {
    //        //List<int> cell_id_list = new List<int>();
    //        //active_cells[i].Cell_Report(ref cell_id_list);

    //        //List<Vector3> cell_vector_list = new List<Vector3>();
    //        //active_cells[i].Cell_Report(ref cell_vector_list);

    //        List<Tentacle_Cell> cell_list = new List<Tentacle_Cell>();

    //        active_cells[i].Cell_Report(ref cell_list);

    //        //tentacle_array[i] = cell_vector_list;
    //        //Debug.Log(cell_id_list.Count);

    //        Tentacle current_tentacle = new Tentacle
    //        {
    //            tentacle_id = i,
    //            tentacle_cells = cell_list

    //        };

    //        tentacle_list.Add(current_tentacle);
    //    }



    //    string json = JsonConvert.SerializeObject(tentacle_list, Formatting.Indented, new JsonSerializerSettings
    //    {
    //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    //    });
    //    File.WriteAllText("D:/Unity/FungiSim/SavedStructureData/testjson.JSON", json);
    //}

    //public Vector3 Grid_Resolution
    //{
    //    get
    //    {
    //        return new Vector3(grid_res_x, grid_res_y, grid_res_z);
    //    }
    //}

    //public Cell[] Cells_Array
    //{
    //    get
    //    {
    //        return cells_array;
    //    }
    //}

    //public void Add_Node(Cell new_node)
    //{
    //    active_cells.Add(new_node);
    //}
}

