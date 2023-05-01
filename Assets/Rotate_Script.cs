using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Rotate_Script : MonoBehaviour
{
    [SerializeField]
    private float rotate_rate;
    [SerializeField]
    private float anim_length = 30;
    [SerializeField]
    private float anim_frame_rate = 30;
    [SerializeField]
    private float total_frames;
    [SerializeField]
    private int num_orbits = 1;

    // Start is called before the first frame update
    void OnEnable()
    {
        total_frames = anim_frame_rate * anim_length;
        rotate_rate = (num_orbits * 360) / total_frames;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(0, rotate_rate, 0);
    }

    public float Total_Frames
    {
        get
        {
            return total_frames;
        }

    }
}
