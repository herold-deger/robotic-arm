using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class RandomJoint : MonoBehaviour
{
    public float speed = 1000f;
    public float stiffness = 100f;
    public float damping = 20000000000000000000000000f;
    public float forceLimit =100000000000f;
    public int numberOfIterations = 50000;
    public Transform endEffector;

    public Transform armPosition; 
    public GameObject plateau;

    private ArticulationBody[] joints;
    private List<DataPoint> data = new List<DataPoint>();

    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;
    private Vector3 initialRootPosition;
    private Quaternion initialRootRotation;


    public class DataPoint
    {
        public List<float> jointTargets = new List<float>();
        public Vector3 endEffectorPosition;

        public Vector3 position_arm;
        public bool collisionOccurred;
    }

    public string csvFileName = "joint_data.csv";

    void Start()
    {
        // Génère un nom de fichier unique si non défini manuellement
        if (string.IsNullOrEmpty(csvFileName) || csvFileName == "joint_data.csv")
        {
            int rnd = Random.Range(1000, 10000); // 4 chiffres
            csvFileName = $"joint_data_{rnd}.csv";
        }

        joints = GetComponentsInChildren<ArticulationBody>();

      
        initialPositions = new Vector3[joints.Length];
        initialRotations = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            initialPositions[i] = joints[i].transform.localPosition;
            initialRotations[i] = joints[i].transform.localRotation;
        }
        initialRootPosition = transform.position;
        initialRootRotation = transform.rotation;

        foreach (var joint in joints)
        {
           
            ArticulationDrive drive = joint.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;

           
            if (joint.GetComponent<CollisionReporter>() == null)
                joint.gameObject.AddComponent<CollisionReporter>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(GenerateDataPoints(numberOfIterations));
        }
    }

    IEnumerator GenerateDataPoints(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return StartCoroutine(MoveJointsSequentially());
            
            Debug.Log($"Point {i}/{count}");
        }

        SaveData();
        Debug.Log("Données générées et sauvegardées !");
    }

    IEnumerator MoveJointsSequentially()
    {
        List<float> currentTargets = new List<float>();

        
        for (int i = 1; i < joints.Length; i++)
        {
            ArticulationBody joint = joints[i];
            ArticulationDrive drive = joint.xDrive;
            float newTarget = Random.Range(drive.lowerLimit, drive.upperLimit);
            drive.target = newTarget;
            joint.xDrive = drive;
            yield return new WaitForSeconds(0.1f);
            currentTargets.Add(newTarget);
        }

        
        yield return new WaitForSeconds(0.1f);

        
        bool anyCollision = false;
        foreach (var joint in joints)
        {
            var reporter = joint.GetComponent<CollisionReporter>();
            if (reporter != null && reporter.collisionDetected)
                anyCollision = true;
            reporter?.ResetCollision();
        }

        DataPoint dp = new DataPoint();
        dp.jointTargets = currentTargets;
        dp.endEffectorPosition = endEffector.position;
        dp.position_arm = armPosition != null ? armPosition.position : Vector3.zero; 
        dp.collisionOccurred = anyCollision;
        data.Add(dp);

        
        if (anyCollision)
        {
            Debug.Log("Collision détectée, reset complet !");
            ResetArm();
            yield return new WaitForSeconds(1f);
        }

 
        
    }

    void SaveData()
    {
        string path = Application.dataPath + "/" + csvFileName;

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("j1,j2,j3,j4,end_effector_x,end_effector_y,end_effector_z,collision");

            foreach (var dp in data)
            {
                string targets = string.Join("/", dp.jointTargets);
                string pos = $"{dp.endEffectorPosition.x}/{dp.endEffectorPosition.y}/{dp.endEffectorPosition.z}";
                string position_arm = $"{dp.position_arm.x}/{dp.position_arm.y}/{dp.position_arm.z}";
                string col = dp.collisionOccurred ? "1" : "0";
                writer.WriteLine($"{position_arm}||{targets}||{pos}||{col}");
            }
        }

        Debug.Log($"Données sauvegardées dans : {path}");
    }

    void ResetArm()
    {
        
        transform.position = initialRootPosition;
        transform.rotation = initialRootRotation;

        
        
      
        if (plateau != null)
        {
            var collider = plateau.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false; 
        }

       
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].transform.localPosition = initialPositions[i];
            joints[i].transform.localRotation = initialRotations[i];
            joints[i].linearVelocity = Vector3.zero;
            joints[i].angularVelocity = Vector3.zero;

            
            var drive = joints[i].xDrive;
            drive.target = 0f;
            joints[i].xDrive = drive;
        }

        
        if (plateau != null)
        {
            var collider = plateau.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
        }
    }
}
