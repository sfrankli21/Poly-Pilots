using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class FloatingOriginPhysicsSafe : MonoBehaviour
{
    public Transform target;
    public float recenterDistance = 2000f;
    public LayerMask excludeLayers = 0;
    public bool autoAssignMainCamera = true;
    public bool includeDontDestroyOnLoad = true;

    public static Vector3 TotalOffset { get; private set; }
    public static event Action<Vector3> OnPreOriginShift;
    public static event Action<Vector3> OnPostOriginShift;

    readonly List<GameObject> rootsBuffer = new List<GameObject>(512);
    static GameObject ddolProbe;

    void Awake()
    {
        if (target == null && autoAssignMainCamera && Camera.main != null) target = Camera.main.transform;
        if (includeDontDestroyOnLoad && ddolProbe == null)
        {
            ddolProbe = new GameObject("__FO_DDOL_Probe");
            DontDestroyOnLoad(ddolProbe);
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        var p = target.position;
        if (Mathf.Abs(p.x) > recenterDistance || Mathf.Abs(p.y) > recenterDistance || Mathf.Abs(p.z) > recenterDistance)
        {
            ShiftOrigin(-p);
        }
    }

    public void ForceRecenter()
    {
        if (target == null) return;
        ShiftOrigin(-target.position);
    }

    void ShiftOrigin(Vector3 offset)
    {
        if (offset == Vector3.zero) return;

        OnPreOriginShift?.Invoke(offset);

        rootsBuffer.Clear();

        var sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.IsValid() || !s.isLoaded) continue;
            rootsBuffer.AddRange(s.GetRootGameObjects());
        }

        if (includeDontDestroyOnLoad && ddolProbe != null)
        {
            var ddolScene = ddolProbe.scene;
            if (ddolScene.IsValid() && ddolScene.isLoaded)
                rootsBuffer.AddRange(ddolScene.GetRootGameObjects());
        }

        var autoSync3D = Physics.autoSyncTransforms;
        var autoSync2D = Physics2D.autoSyncTransforms;
        Physics.autoSyncTransforms = false;
        Physics2D.autoSyncTransforms = false;

        for (int i = 0, n = rootsBuffer.Count; i < n; i++)
        {
            var go = rootsBuffer[i];
            if (go == null || !go.activeInHierarchy) continue;
            if (((1 << go.layer) & excludeLayers.value) != 0) continue;

            var rb3 = go.GetComponent<Rigidbody>();
            var rb2 = go.GetComponent<Rigidbody2D>();
            var cc = go.GetComponent<CharacterController>();

            if (rb3 != null && rb3.gameObject == go)
            {
                rb3.position += offset;
            }
            else if (rb2 != null && rb2.gameObject == go)
            {
                rb2.position += (Vector2)offset;
            }
            else if (cc != null && cc.gameObject == go)
            {
                bool wasEnabled = cc.enabled;
                if (wasEnabled) cc.enabled = false;
                go.transform.position += offset;
                if (wasEnabled) cc.enabled = true;
            }
            else
            {
                go.transform.position += offset;
            }
        }

        Physics.SyncTransforms();
        Physics2D.SyncTransforms();
        Physics.autoSyncTransforms = autoSync3D;
        Physics2D.autoSyncTransforms = autoSync2D;

        TotalOffset += offset;
        OnPostOriginShift?.Invoke(offset);
    }
}
