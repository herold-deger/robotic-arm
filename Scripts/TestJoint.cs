using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TestJoint : MonoBehaviour
{
    public float speed = 1000f;
    public float stiffness = 100f;
    public float damping = 2000000000000000f;
    public float forceLimit = 1000000000f;

    public float L1 = 0.5f;
    public float L2 = 0.5f;

    public float L3 = 0.5f;
    public float L4 = 0.5f;
    public Transform endEffector;
    public Transform armPosition;

    private ArticulationBody[] joints;

    private List<float> jointTargets;

    
    public float[] testAngles = new float[4] { 0f, 0f, 0f, 0f };
    public bool useTestAngles = true;

    void Start()
    {
        joints = GetComponentsInChildren<ArticulationBody>();
        jointTargets = new List<float> { L1, L2, L3, L4 };

        foreach (var joint in joints)
        {
            
            ArticulationDrive drive = joint.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;


        }
    }

    void Update()
    {
        if (useTestAngles)
        {

            for (int i = 1; i < joints.Length && i - 1 < testAngles.Length; i++)
            {
                ArticulationBody joint = joints[i];
                ArticulationDrive drive = joint.xDrive;
                drive.target = testAngles[i - 1];
                joint.xDrive = drive;
            }
        }
    }
}
       

