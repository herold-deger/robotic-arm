using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;

// <summary>
/// Generates random joint trajectories (like RandomJoint) but refactored following the
/// efficient structure of testPoints.  It validates that each trajectory passes below a
/// Y‑threshold in the robot‑base frame, stores all useful data, and aborts early if the
/// arm seems blocked (little motion over a sliding window).
/// </summary>
public class RandomTrajectoryTester : MonoBehaviour
{
    [Header("Robot references")]
    public Transform robotBase;          // origin of local frame
    public Transform endEffector;        // TCP / pen tip
    public ArticulationBody[] joints;    // 0 = fixed root, 1..n = actuated axes

    [Header("Motion parameters")]
    private int iterations      = 1;   // how many random poses to test
    public float moveDuration  = 1f;  // seconds to reach pose
    [SerializeField] private float settleTime = 120f;  // seconds to let physics settle

    [Header("Drive settings")]
    public float stiffness  = 100f;
    private float damping    = 1e8f;
    public float forceLimit = 1e5f;

    [Header("Validation")]
    public float yThreshold           = -50f;  // local‑Y must be below this (mm)
    public int   windowSize           = 5;     // # samples to detect blockage
    public float minMovementThreshold = 400f;  // mm : span under which we say "blocked"

    [Header("Output")]
    public string csvFileName = "random_trajectories.csv";

    // ──────────────────────────────────────────────────────────────── internal ─────
    class DataPoint
    {
        public List<float> jointTargets = new();
        public Vector3 endEffectorWorld;
        public bool passedUnderThreshold;
        public bool collision;
    }

    readonly List<DataPoint> data        = new();
    readonly Queue<Vector3>  lastSamples = new();
    Coroutine runner;

    // ──────────────────────────────── setup ──────────────────────────────────────
    void Awake()
    {
        if (joints == null || joints.Length == 0)
            joints = GetComponentsInChildren<ArticulationBody>();

        // Uniform drive settings & attach collision reporters
        foreach (var j in joints)
        {
            var d = j.xDrive;
            d.stiffness  = stiffness;
            d.damping    = damping;
            d.forceLimit = forceLimit;
            j.xDrive = d;

            if (!j.TryGetComponent(out CollisionReporter _))
                j.gameObject.AddComponent<CollisionReporter>();
        }

        if (string.IsNullOrEmpty(csvFileName) || csvFileName == "random_trajectories.csv")
            csvFileName = $"une_trajectoire_{Random.Range(1000,9999)}.csv";
    }

    void OnEnable()  => runner = StartCoroutine(RunTests());
    void OnDisable() { if (runner != null) StopCoroutine(runner); }

    // ─────────────────────────────── main loop ───────────────────────────────────
    IEnumerator RunTests()
    {
        for (int i = 0; i < iterations; i++)
        {
            // 1. Pick random joint targets
            var currentTargets = new List<float>();
            for (int j = 1; j < joints.Length; j++)
            {
                var drv = joints[j].xDrive;
                float tgt = Random.Range(drv.lowerLimit, drv.upperLimit);
                drv.target = tgt;
                joints[j].xDrive = drv;
                currentTargets.Add(tgt);
            }

            // 2. Wait for motion and physics to stabilise
            yield return new WaitForSeconds(moveDuration + settleTime);

            // 3. Collision check
            bool anyCollision = false;
            foreach (var joint in joints)
            {
                var rep = joint.GetComponent<CollisionReporter>();
                if (rep && rep.collisionDetected) anyCollision = true;
                rep?.ResetCollision();
            }

            // 4. Position & threshold check
            Vector3 worldPos  = endEffector.position;
            Vector3 localPos  = robotBase.InverseTransformPoint(worldPos);
            bool    underArm  = localPos.y < yThreshold;

            data.Add(new DataPoint
            {
                jointTargets          = currentTargets,
                endEffectorWorld      = worldPos,
                passedUnderThreshold  = underArm,
                collision             = anyCollision
            });

            // 5. Blockage detection over sliding window
            lastSamples.Enqueue(worldPos);
            if (lastSamples.Count > windowSize) lastSamples.Dequeue();

            if (lastSamples.Count == windowSize)
            {
                float maxSpan = CalcMaxDistance(lastSamples);
                if (maxSpan < minMovementThreshold * 0.001f)   // convert mm→m
                {
                    Debug.LogWarning($"[Tester] Arm blocked (span {maxSpan*1000f:F1} mm) → abort.");
                    break;
                }
            }

            // Ajoute 1 seconde d'attente entre chaque essai
            yield return new WaitForSeconds(1f);
        }

        SaveCsv();
        Debug.Log("[Tester] ✅ Data generation finished.");
    }

    // ────────────────────────────── utils ────────────────────────────────────────
    static float CalcMaxDistance(IEnumerable<Vector3> pts)
    {
        float max = 0f;
        var arr = pts.ToArray();
        for (int a = 0; a < arr.Length; a++)
            for (int b = a + 1; b < arr.Length; b++)
                max = Mathf.Max(max, Vector3.Distance(arr[a], arr[b]));
        return max;
    }

    void SaveCsv()
    {
        string path = Path.Combine(Application.dataPath, csvFileName);
        using var sw = new StreamWriter(path);
        sw.WriteLine("j_targets,end_x,end_y,end_z,under_thresh,collision");
        foreach (var dp in data)
        {
            string targets = string.Join("/", dp.jointTargets.Select(f => f.ToString(CultureInfo.InvariantCulture)));
            Vector3 p = dp.endEffectorWorld - robotBase.position; // position relative à la base
            sw.WriteLine($"{targets},{p.x.ToString(CultureInfo.InvariantCulture)},{p.y.ToString(CultureInfo.InvariantCulture)},{p.z.ToString(CultureInfo.InvariantCulture)},{(dp.passedUnderThreshold?1:0)},{(dp.collision?1:0)}");
        }
        Debug.Log($"[Tester] CSV saved to {path}");
    }
}
