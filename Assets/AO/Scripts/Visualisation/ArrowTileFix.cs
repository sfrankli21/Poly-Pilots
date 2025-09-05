using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Adjusts the texture of an arrow to prevent it being stretched as the arrow's size changes.
    /// </summary>
    public class ArrowTileFix : MonoBehaviour
    {
        public float scale = 1f;

        Material material;

        private void Start()
        {
            material = GetComponent<MeshRenderer>().material;
        }

        // Update is called once per frame
        void Update()
        {
            material.mainTextureScale = new Vector2(1f, scale * transform.lossyScale.y);
        }
    }
}
