using UnityEngine;

namespace AerodynamicObjects
{
    public class SurfaceTrails : FlowParticles
    {
        /// <summary>
        /// To spawn surface particles efficiently from a moving object, e.g. debris thrown up by tyres, set Moving Source to true. This changes the particle spawn method to 'over distance', rather than 'over time'. For debris fixed in the world, e.g. mud patch, puddle, etc Moving Source should be false
        /// </summary>
        [Tooltip("To spawn surface particles efficiently from a moving object, e.g. debris thrown up by tyres, set Moving Source to true. This changes the particle spawn method to 'over distance', rather than 'over time'. For debris fixed in the world, e.g. mud patch, puddle, etc Moving Source should be false")]
        public bool movingSource = false;
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

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();

            m_ParticleSystem = GetComponent<ParticleSystem>();
            trailModule = m_ParticleSystem.trails;
            emissionModule = m_ParticleSystem.emission;
            collisionModule = m_ParticleSystem.collision;
            shapeModule = m_ParticleSystem.shape;
            particleSystemRenderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();

            // Make sure the particle system is using world space instead of local!
            mainModule = m_ParticleSystem.main;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

            UpdateParticleSystemSettings();
        }

        public override void UpdateParticleSystemSettings()
        {
            mainModule.startSize = particleSize / transform.lossyScale.magnitude;
            mainModule.startLifetime = particleLife;
            mainModule.gravityModifier = 2; // particles sink with gravity
            if (movingSource)
            {
                emissionModule.rateOverDistance = particleSpawnRate;
            }
            else
            {
                emissionModule.rateOverTime = particleSpawnRate;
            }

            particleSystemRenderer.renderMode = ParticleSystemRenderMode.None;
            collisionModule.lifetimeLoss = 0; // particles dont expire on contact with bounds (floor)
            collisionModule.enabled = true;
            trailModule.enabled = true;
            shapeModule.shapeType = ParticleSystemShapeType.Sphere;
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
