using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ArticulationJointController : MonoBehaviour
{
    public float speed = 1000f;
    public float stiffness = 100f;
    public float damping =20000f;
    public float forceLimit = 100000f;

    private ArticulationBody[] joints;
  
    private int selectedIndex = 0;

    void Start()
    {
        joints = GetComponentsInChildren<ArticulationBody>();
        for (int i = 0; i < joints.Length; i++)
        {   
            
            ArticulationBody joint = joints[i];
            var collider = joint.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            ArticulationDrive drive = joint.xDrive;
            drive.stiffness = 100f;      
            drive.damping = 20000000f;     
            drive.forceLimit = 1000000f;
            drive.target = 0f;
            joint.xDrive = drive;
        }
    }
    void LockUnselectedJoints()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            if (i == selectedIndex) continue;

            var joint = joints[i];
            var drive = joint.xDrive;
            float currentPosition = 0f;
            if (joint.jointPosition.dofCount > 0)
                currentPosition = joint.jointPosition[0];

            drive.target = currentPosition;
            drive.stiffness = 1000000000f;  
            drive.damping = 10f;
            drive.forceLimit = 1f;

            joint.xDrive = drive;
        }
    }



    void Update()

    {
        ArticulationBody joint = joints[selectedIndex];
        ArticulationDrive drive = joint.xDrive;
        float position = joint.jointPosition.dofCount > 0 ? joint.jointPosition[0] : 0f;
        Debug.Log($"[Move] {joint.name}: target={drive.target}, position={position}");

       
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex = (selectedIndex + 1) % joints.Length;
            Debug.Log($"Joint {selectedIndex}: {joints[selectedIndex].name}");
            
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex = (selectedIndex - 1 + joints.Length) % joints.Length;
            Debug.Log($"Joint {selectedIndex}: {joints[selectedIndex].name}");
        
        }

        float direction = Input.GetAxis("Vertical"); 
        if (Mathf.Abs(direction) > 0.1f)
        {
            MoveSelectedJoint(direction);

        }
       
    }

    void MoveSelectedJoint(float direction)
    {   
        
        ArticulationBody joint = joints[selectedIndex];
        ArticulationDrive drive = joint.xDrive;
        drive.stiffness = 100f;      
        drive.damping = 200000000f;     
        drive.forceLimit = 10000000f; 
        
        float delta = direction * speed * Time.deltaTime;
        float newTarget = drive.target + delta;
        newTarget = Mathf.Clamp(newTarget, drive.lowerLimit, drive.upperLimit);
        drive.target = newTarget;
        joint.xDrive = drive;

        float position = joint.jointPosition.dofCount > 0 ? joint.jointPosition[0] : 0f;
        Debug.Log($"[Move] {joint.name}: target={drive.target}, position={position}");
    }  
}
