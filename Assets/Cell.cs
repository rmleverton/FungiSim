/* Name: Cell
 * Date: 01/05/23
 * Author: Robin Leverton
 * Description: A cell object for a fungal simulation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Cell : MonoBehaviour
{
    // Constants
    private int my_id;
    private Vector3 my_position;
    [SerializeField]
    private int[] my_xyz;
    [SerializeField]
    private float my_initial_value;
    [SerializeField]
    private float my_distance_from_centre;
    private Mesh my_mesh;
    private Material my_material;
    private MeshFilter mesh_filter;
    private MeshRenderer mesh_renderer;
    private CellManager cell_manager;
    [SerializeField]
    private List<Cell> my_neighbours;
    private List<int> my_neighbours_ids;

    // Variables
    [SerializeField]
    private string my_species;
    [SerializeField]
    private float my_cell_value;
    [SerializeField]
    private Cell my_cell_parent;
    [SerializeField]
    private List<Cell> my_children;

    // Parameters
    private float grow_cell_value;
    private float my_parent_pass_scalar;
    private float my_growth_scalar;
    private float my_value_change_scalar;
    private float cell_upkeep_value;
    private float node_transform_value;


    // Function to pass in and setup the intial state of each cell
    public void Create(int id, Vector3 position, float init_cell_value, CellManager passed_cell_manager, float[] parameters, float distance_from_centre)
    {
        

        my_id = id;
        my_position = position;
        my_cell_value = init_cell_value;
        my_initial_value = init_cell_value;

        grow_cell_value = parameters[0];
        my_parent_pass_scalar = parameters[1];
        my_growth_scalar = parameters[2];
        my_value_change_scalar = parameters[3];
        cell_upkeep_value = parameters[4];
        node_transform_value = parameters[5];

        my_distance_from_centre = distance_from_centre;

        mesh_renderer = gameObject.AddComponent<MeshRenderer>();
        mesh_filter = gameObject.AddComponent<MeshFilter>();

        mesh_filter.mesh = my_mesh;
        mesh_renderer.material = my_material;

        gameObject.transform.position = my_position;

        cell_manager = passed_cell_manager;

        Cell_Species = "background";

        my_neighbours = new List<Cell>();
        my_neighbours_ids = new List<int>();
        my_children = new List<Cell>();

        //Debug.Log("Cell Create: " + my_id);


    }

    // Update here deals only with orphans and cells that approach infinite values
    private void Update()
    {
        
        if (Cell_Parent == null && Cell_Species != "background")
        {
            for (int i = 0; i < my_children.Count; i++)
            {
                if (my_children[i].Cell_Species == "background")
                {
                    Remove_Child(my_children[i]);
                    continue;
                }
                my_children[i].Update_Cell();
            }

            if (my_cell_value <= 0)
            {
                Cell_Death();
            }

            my_cell_value *= my_initial_value;
            my_cell_value -= cell_upkeep_value;
        }

        if (Cell_Value > 1000000)
        {
            Cell_Value = 1000000f;
        }
    }

    // Update is called once per frame
    public void Update_Cell()
    {
        
        for (int i = 0; i < my_children.Count; i++)
        {
            // If a child cell has died, remove it from children
            if (my_children[i].Cell_Species == "background")
            {
                Remove_Child(my_children[i]);
                continue;
            }

            // Update all children
            my_children[i].Update_Cell();
        }

       
        //  Are we dead, do this first
        if (my_cell_value <= 0)
        {
            Cell_Death();
            return;
        }

        // Update accordingly to species
        if (my_species == "node")
        {
            Update_Node();
                
        }
        else if(my_species == "child")
        {
            Update_Child();
                
        }

        // Cell upkeep, important to remember this is only occuring for living cells
        my_cell_value *= my_initial_value;
        my_cell_value -= cell_upkeep_value;
    }

    private void Update_Node()
    {

        // Are we big enough to grow another tentacle?
        if (my_cell_value > grow_cell_value)
        {
            Create_Child(this);
        }

    }

    private void Update_Child()
    {


        //  Are we big enough to add a child?, do we not have any children
        if (my_cell_value > grow_cell_value && my_children.Count < 1)
        {
            Create_Child(this);
        }

        //  Give value to parent
        my_cell_parent.Cell_Value += my_cell_value * my_parent_pass_scalar;
        
        //  Are we big enough to become a node
        if(my_cell_value >= node_transform_value)
        {
            Create_Node();
        }

    }

    // Check what species we are and act accodingly
    private void Cell_Death()
    {
        if (Cell_Species == "child")
        {
            Cell_Species = "background";

            for (int i = 0; i < my_children.Count; i++)
            {
                my_children[i].Cell_Parent = null;
            }
            my_children.Clear();

            Cell_Parent = null;

        }
        if(Cell_Species == "node")
        {
            Cell_Species = "bone";
        }
        
        //my_cell_parent.Remove_Child(this);
    }

    private void Create_Child(Cell cell_parent)
    {
        // find the highest positive value neighbour that is still a background cell
        foreach(Cell neighbour in my_neighbours)
        {
            if(neighbour.Cell_Species == "background" && neighbour.Cell_Value > 0)
            {
                neighbour.Cell_Species = "child";
                neighbour.Cell_Parent = cell_parent;
                neighbour.Cell_Value += my_cell_value * 0.5f;
                my_cell_value *= 0.5f;
                my_children.Add(neighbour);
                
                break;

                
            }
        }

        // increase the cost of creating a new child
        grow_cell_value *= my_growth_scalar;
    }

    private void Create_Node()
    {
        // If any of our neighbours are a node or bone then return
        foreach(Cell neighbour in my_neighbours)
        {
            if(neighbour.Cell_Species == "node" || neighbour.Cell_Species == "bone")
            {
                return;
            }
        }

    // Make this a node, its parent a bone and remove this from its parent's children
        Cell_Species = "node";
        my_cell_parent.Make_Bone();
        my_cell_parent.Remove_Child(this);
        Cell_Parent = null;
        cell_manager.Add_Node(this);
    }

    // Check the case and act appropriately based on species
    private void Change_Colour()
    {
        switch (my_species)
        {
            case "node":
                {
                    GetComponent<MeshRenderer>().enabled = true;
                    GetComponent<MeshRenderer>().material.color = Color.red;
                    break;
                }
            case "child":
                {

                    GetComponent<MeshRenderer>().enabled = true;
                    GetComponent<MeshRenderer>().material.color = Color.blue;
                    break;
                }
            case "background":
                {
                    GetComponent<MeshRenderer>().enabled = false;
                    GetComponent<MeshRenderer>().material.color = new Color(1* my_distance_from_centre, 1 * my_distance_from_centre, 1 * my_distance_from_centre, 1);
  

                    break;
                }
            case "bone":
                {
                    GetComponent<MeshRenderer>().enabled = true;
                    GetComponent<MeshRenderer>().material.color = Color.white; 
                    break;
                }
            default:
                {
                    GetComponent<MeshRenderer>().enabled = false;
                    GetComponent<MeshRenderer>().material.color = Color.white;
                    break;
                }
        }
    }

    // Go through all possible neighbours on a 3D grid
    // Skip if we are at the sides
    public void Find_Neighbours()
    {
        for (int x = -1; x <= 1; x++)
        {
            int nxId = (int)my_xyz[0] + x;
            if (nxId < 0 || nxId >= cell_manager.Grid_Resolution.x)
            {
                continue;
            }
            else
            {
                for (int y = -1; y <= 1; y++)
                {
                    int nyId = (int)my_xyz[1] + y;
                    if (nyId < 0 || nyId >= cell_manager.Grid_Resolution.y)
                    {
                        continue;
                    }
                    else
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            if (x == 0 && y == 0 && z == 0)
                            {
                                continue;
                            }

                            int nzId = (int)my_xyz[2] + z;

                            if (nzId < 0 || nzId >= cell_manager.Grid_Resolution.z)
                            {

                                continue;
                            }
                            else
                            {
                                // Find the id of the neighbour
                                int nId = (int)(nxId * cell_manager.Grid_Resolution.y + nyId + (nzId * cell_manager.Grid_Resolution.x * cell_manager.Grid_Resolution.y));

                                // Add that cell to our neighbours list
                                Cell new_neighbour = cell_manager.Cells_Array[nId];
                                my_neighbours.Add(new_neighbour);
                            }
                        }
                    }
                }
            }

        }


        Organise_Neighbours();
    }

    // Order neighbours by descending intial cell value
    private void Organise_Neighbours()
    {

        my_neighbours = my_neighbours.OrderByDescending(c => c.Cell_Value).ToList();
        
    }

    // Add ourselves to the tentacle list then continue to our child
    public void Cell_Report(ref List<Tentacle_Cell> current_tentacle)
    {
        Tentacle_Cell current_cell = new Tentacle_Cell
        {
            cell_id = my_id,
            cell_position = my_xyz,
            cell_value = my_cell_value
        };

        current_tentacle.Add(current_cell);

        //cell_vector_list.Add(Cell_XYZ);

        if(my_children.Count > 0)
        {
            for(int i = 0; i < my_children.Count; i++)
            {
                if(my_children[i].Cell_Parent == this)
                {
                    my_children[i].Cell_Report(ref current_tentacle);
                }
            }
        }
    }

    public void Remove_Child(Cell child)
    {
        my_children.Remove(child);
    }
     
    public void Make_Bone()
    {
        if(Cell_Species != "child")
        {
            return;
        }
        else
        {
            Cell_Species = "bone";
            my_cell_parent.Make_Bone();
        }
    }

    public string Cell_Species {
        get
        {
            return my_species;
        }
        set
        {
            my_species = value;
            Change_Colour();
        }
    }
    
    public Mesh Cell_Mesh
    {
        get
        {
            return my_mesh;
        }

        set
        {
            my_mesh = value;
        }
    }

    public Material Cell_Material
    {
        get
        {
            return my_material;
        }

        set
        {
            my_material = value;
        }
    }

    public int[] Cell_XYZ
    {
        get
        {
            return my_xyz;
        }
        set
        {
            my_xyz = value;
        }
    }

    public float Cell_Value
    {
        get
        {
            return my_cell_value;
        }
        set
        {
            my_cell_value = value;
        }
    }

    public Cell Cell_Parent
    {
        get
        {
            return my_cell_parent;
        }
        set
        {
            my_cell_parent = value;
        }
    }

    public List<int> Cell_Neighbours_ID
    {

        set
        {
            my_neighbours_ids = value;

        }
    }
}
