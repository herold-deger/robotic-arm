using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text; // Ajoute en haut du fichier si pas déjà présent
/// <summary>
/// Fait suivre le bras à la position d'un GameObject cible
/// en piochant, dans un CSV issu de ton DataFrame, la ligne
/// (x_rel, y_rel, z_rel) la plus proche et en appliquant les
/// angles j1-j4 correspondants.
/// </summary>
public class testPoints : MonoBehaviour
{
    [Header("Références")]
    public Transform target;          // GameObject mobile à suivre
    public Transform robotBase;       // Point origine du robot (optionnel)
    public ArticulationBody[] joints; // racine + 4 articulations (0 = base fixe)
    [SerializeField] private Transform endEffector; // <-- Ajout ici

    [Header("Paramètres CSV")]
    [Tooltip("Chemin relatif ou absolu vers le CSV (en-têtes : x_rel,y_rel,z_rel,j1,j2,j3,j4)")]
    public string csvPath = "inverse_kinematics.csv";

    [Header("Mouvement")]
    public float stiffness   = 100f;
    public float damping     = 10000f;
    public float forceLimit  = 1e6f;
    public float updateRate  = 0.05f;     // s, ré-échantillonnage de la cible
    public float moveTime    = 0.5f;      // s, durée de transition vers la nouvelle pose

    // ────────────────────────── internes ──────────────────────────
    private readonly List<Vector3> xyzList   = new();  // (x_rel,y_rel,z_rel)
    private readonly List<float[]> jointsList = new(); // [j1,j2,j3,j4]
    private Coroutine followRoutine;

    private List<int> randomIndices = new();
    private List<Vector3> effectorPositions = new(); // Liste pour stocker les positions de l'end effector

    void Awake()
    {
        if (joints == null || joints.Length < 5)       // base + 4 axes
            joints = GetComponentsInChildren<ArticulationBody>();

        InitDrives();
        LoadCsv();
        PickRandomIndices();
    }

    void OnEnable()
    {
        followRoutine = StartCoroutine(FollowRandomPoints());
    }

    void OnDisable()
    {               
        if (followRoutine != null) StopCoroutine(followRoutine);
    }

    // ────────────── Sélectionne 100 indices aléatoires ──────────────
    void PickRandomIndices()
    {
        randomIndices.Clear();
        int n = xyzList.Count;
        int count = Mathf.Min(500, n);
        System.Random rng = new System.Random();
        HashSet<int> chosen = new HashSet<int>();
        while (chosen.Count < count)
        {
            int idx = rng.Next(n);
            if (!chosen.Contains(idx))
                chosen.Add(idx);
        }
        randomIndices.AddRange(chosen);
    }

    // ──────────────────────── CSV → List ──────────────────────────
    void LoadCsv()
{
    xyzList.Clear();
    jointsList.Clear();

    var path = csvPath;
    if (!System.IO.File.Exists(path))
    {
        Debug.LogError($"[Follower] CSV introuvable : {path}");
        return;
    }

    using var sr = new System.IO.StreamReader(path);
    var culture  = System.Globalization.CultureInfo.InvariantCulture;

    // ── 1. Entête ──────────────────────────────────────────────
    string header = sr.ReadLine();
    if (header == null)
    {
        Debug.LogError("[Follower] CSV vide !");
        return;
    }

    string[] cols = header.Split(',');
    int xi  = System.Array.IndexOf(cols, "x_rel");
    int yi  = System.Array.IndexOf(cols, "y_rel");
    int zi  = System.Array.IndexOf(cols, "z_rel");
    int j1i = System.Array.IndexOf(cols, "j1");
    int j2i = System.Array.IndexOf(cols, "j2");
    int j3i = System.Array.IndexOf(cols, "j3");
    int j4i = System.Array.IndexOf(cols, "j4");

    int[] idxs = { xi, yi, zi, j1i, j2i, j3i, j4i };
    if (System.Array.Exists(idxs, id => id < 0))
    {
        Debug.LogError($"[Follower] Entête CSV invalide : {header}");
        return;
    }

    
    int maxIdx = idxs.Max();
    // ── 2. Lecture lignes ─────────────────────────────────────
    string line;
    int   nGood = 0, nSkip = 0;

    while ((line = sr.ReadLine()) != null)
    {
        if (string.IsNullOrWhiteSpace(line)) { nSkip++; continue; }

        // Accepte virgule OU point-virgule, supprime espaces
        string[] tok = line.Split(new[] { ',', ';' });
        if (tok.Length <= maxIdx) { nSkip++; continue; }

        try
        {
            float x  = float.Parse(tok[xi].Trim(),  culture);
            float y  = float.Parse(tok[yi].Trim(),  culture);
            float z  = float.Parse(tok[zi].Trim(),  culture);
            float j1 = float.Parse(tok[j1i].Trim(), culture);
            float j2 = float.Parse(tok[j2i].Trim(), culture);
            float j3 = float.Parse(tok[j3i].Trim(), culture);
            float j4 = float.Parse(tok[j4i].Trim(), culture);

            xyzList.Add(new Vector3(x, y, z));
            jointsList.Add(new[] { j1, j2, j3, j4 });
            nGood++;
        }
        catch
        {
            nSkip++;
        }
    }

    Debug.Log($"[Follower] CSV chargé : {nGood} lignes valides, {nSkip} ignorées");
}

    // ───────────────────────── Drives ─────────────────────────────
    void InitDrives()
    {
        foreach (var joint in joints)
        {
            var d = joint.xDrive;
            d.stiffness  = stiffness;
            d.damping    = damping;
            d.forceLimit = forceLimit;
            joint.xDrive = d;
        }
    }

    // ─────────────── Boucle principale sur 1000 points ──────────────
    IEnumerator FollowRandomPoints()
    {
        float threshold = 0.01f; // 1 cm
        List<float> errors = new List<float>();

        foreach (int idx in randomIndices)
        {
            Vector3 targetPos = xyzList[idx];
            float[] angleDeg = jointsList[idx];

            // Applique les angles
            yield return StartCoroutine(BlendToTargets(angleDeg, moveTime));

            // Attend que le bras se stabilise
            yield return new WaitForSeconds(2f);

            // Passe la cible en repère monde
            Vector3 targetWorld = targetPos + robotBase.position;

            // Vérifie la distance entre endEffector et la cible
            Vector3 effectorWorld = endEffector.position;

            // Debug : affiche les positions et le vecteur local
            Debug.Log($"[Debug] endEffector.position = {effectorWorld}, robotBase.position = {robotBase.position}, targetWorld = {targetWorld}, targetPos (local) = {targetPos}");

            float dist = Vector3.Distance(effectorWorld, targetWorld);

            errors.Add(dist);

            if (dist > threshold)
            {
                Debug.LogWarning($"[Test] Point {idx} : Erreur {dist:F4} m (cible {targetWorld}, atteint {effectorWorld})");
            }
            else
            {
                Debug.Log($"[Test] Point {idx} : OK (erreur {dist:F4} m)");
            }

            // Stocke la position de l'end effector
            effectorPositions.Add(effectorWorld);

            yield return new WaitForSeconds(updateRate);
        }

        float meanErrorCm = errors.Average() * 100f;

        // Comptage par catégorie
        int count15 = errors.Count(e => e * 100f <= 15f);
        int count30 = errors.Count(e => e * 100f > 15f && e * 100f <= 30f);
        int countMore = errors.Count(e => e * 100f > 30f);

        Debug.Log($"[Test] Moyenne d'écart sur 1000 points : {meanErrorCm:F2} cm");
        Debug.Log($"[Test] Points à moins de 15 cm : {count15}");
        Debug.Log($"[Test] Points entre 15 et 30 cm : {count30}");
        Debug.Log($"[Test] Points au-delà de 30 cm : {countMore}");
        Debug.Log("[Test] Test terminé sur 100 points.");

        // Génération d'un nom de fichier aléatoire
        string randomName = "test_result_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + ".csv";
        string savePath = Path.Combine(Application.dataPath, randomName);

        // Création du CSV
        var sb = new StringBuilder();
        sb.AppendLine("csv_index,distance_m");

        // On suppose que randomIndices et errors sont dans le même ordre
        for (int i = 0; i < randomIndices.Count; i++)
        {
            if (effectorPositions[i].y >= 30f)
                sb.AppendLine($"{randomIndices[i]},{errors[i].ToString(CultureInfo.InvariantCulture)}");
        }

        File.WriteAllText(savePath, sb.ToString());
        Debug.Log($"[Test] Résultats sauvegardés dans : {savePath}");
    }

    // ──────────────────── Interpolation articulations ─────────────
    IEnumerator BlendToTargets(float[] goalDeg, float duration)
    {
        float t = 0f;
        float[] start = new float[4];

        for (int i = 0; i < 4; i++)
            start[i] = joints[i + 1].xDrive.target; // joints[0] = base fixe

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.01f);
            float s = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < 4; i++)
            {
                var drv = joints[i + 1].xDrive;
                drv.target = Mathf.Lerp(start[i], goalDeg[i], s);
                joints[i + 1].xDrive = drv;
                
            }
            
            yield return null;
        }
    }
}
