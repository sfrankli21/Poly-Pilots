using AerodynamicObjects.Flow;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Varies the strength scale of any child vortex filaments sinusoidally with time.
    /// </summary>
    public class RingVortexTunnelController : MonoBehaviour
    {
        VortexFilament[] vfs;
        public float amplitdue, frequency;
        float strength;

        // Start is called before the first frame update
        void Start()
        {
            vfs = GetComponentsInChildren<VortexFilament>();
        }

        // Update is called once per frame
        void Update()
        {
            strength = amplitdue * Mathf.Sin(6.2f * frequency * Time.time);
            foreach (VortexFilament v in vfs)
            {
                v.strengthScale = strength;
            }
        }
    }
}
