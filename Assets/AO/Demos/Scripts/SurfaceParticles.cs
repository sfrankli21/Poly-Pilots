using UnityEngine;
using UnityEngine.Rendering;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// An experimental script used to manage particles which are spawned on a surface like the ground.
    /// </summary>
    public class SurfaceParticles : FlowParticles
    {
        /// <summary>
        /// To spawn surface particles efficiently from a moving object, e.g. debris thrown up by tyres, set Moving Source to true. This changes the particle spawn method to 'over distance', rather than 'over time'. For debris fixed in the world, e.g. mud patch, puddle, etc Moving Source should be false
        /// </summary>
        [Tooltip("To spawn surface particles efficiently from a moving object, e.g. debris thrown up by tyres, set Moving Source to true. This changes the particle spawn method to 'over distance', rather than 'over time'. For debris fixed in the world, e.g. mud patch, puddle, etc Moving Source should be false")]
        public bool movingSource = false;
        /// <summary>
        /// Sets the rate at which particles fall back to the floor after being disturbed. Default value is 1.
        /// </summary>
        [Tooltip("Sets the rate at which particles fall back to the floor after being disturbed. Default value is 1.")]
        public float gravityModifier = 1;
        public bool isBounded = true;

        ParticleSystem.CollisionModule collisionModule;
        ParticleSystem.ShapeModule shapeModule;

        public override void OnValidate()
        {
            base.OnValidate();
            particleSpawnRate = Mathf.Max(0, particleSpawnRate);
            particleSize = Mathf.Max(0, particleSize);
            particleLife = Mathf.Max(0, particleLife);
        }

        public override void GetParticleComponents()
        {
            base.GetParticleComponents();
            collisionModule = m_ParticleSystem.collision;

            shapeModule = m_ParticleSystem.shape;
            particleSystemRenderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();
        }

        public override void UpdateParticleSystemSettings()
        {
            mainModule.startSize = particleSize / transform.lossyScale.magnitude;
            mainModule.startLifetime = particleLife;
            mainModule.gravityModifier = gravityModifier; // particles sink with gravity
            if (movingSource)
            {
                emissionModule.rateOverDistance = particleSpawnRate;
            }
            else
            {
                emissionModule.rateOverTime = particleSpawnRate;
            }

            particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleSystemRenderer.SetMeshes(new Mesh[] { Resources.GetBuiltinResource<Mesh>("Sphere.fbx") });
            collisionModule.enabled = true;

            collisionModule.lifetimeLoss = new ParticleSystem.MinMaxCurve(0); // particles dont expire on contact with bounds (floor)
            //shapeModule.shapeType = ParticleSystemShapeType.Sphere;
            particleSystemRenderer.shadowCastingMode = ShadowCastingMode.On;
            //trailModule.enabled = enableParticleTrails;
            //if (enableParticleTrails)
            //{
            //    particleSystemRenderer.renderMode = ParticleSystemRenderMode.None;
            //}
            //else
            //{
            //    particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            //}
        }

        private void OnDrawGizmosSelected()
        {
            if (isBounded)
            {
                Gizmos.DrawWireCube(transform.position, transform.lossyScale);
            }
        }
    }
}
