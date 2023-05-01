using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceTests : MonoBehaviour
{
    [SerializeField]
    private int grid_res_x, grid_res_y, grid_res_z;

    [SerializeField]
    private float noise_scale = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        int num_tiles = grid_res_x * grid_res_y * grid_res_z;

        for (int i = 0; i < num_tiles; i++)
        {

            

            int cell_pos_y = i % grid_res_y;
            int cell_pos_x = ((i - cell_pos_y) / grid_res_y) % grid_res_x;
            int cell_pos_z = (((i - cell_pos_y) / grid_res_y) - cell_pos_x) / grid_res_x;

            Vector3 cell_position = new Vector3(cell_pos_x - grid_res_x / 2, cell_pos_y - grid_res_y / 2, cell_pos_z - grid_res_z / 2);

            GameObject cell_object = new GameObject();
            cell_object.name = "cell_" + i.ToString() + "_" + cell_pos_x.ToString() + "_" + cell_pos_y.ToString() + "_" + cell_pos_z.ToString();

            Cell new_cell = cell_object.AddComponent<Cell>();
            //new_cell.Cell_Mesh = cell_mesh;
            //new_cell.Cell_Material = cell_material;
            //new_cell.Cell_XYZ = new int[] { cell_pos_x, cell_pos_y, cell_pos_z };

            float generated_noise = SimplexNoise.Noise.CalcPixel3D(cell_pos_x, cell_pos_y, cell_pos_z, noise_scale);


            float init_cell_value = SimplexNoise.Noise.Map(generated_noise, 0.0f, 256.0f, 0.0f, 1.0f);
            ////Debug.Log(init_cell_value);

            //new_cell.Create(i, cell_position, init_cell_value, parameters);


            //cells_array[i] = new_cell;



        }

        Debug.Log("Done: " + Time.realtimeSinceStartup + "s");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
