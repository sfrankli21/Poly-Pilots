using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Spawns particles which are affected by the flow in a volume.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Visualisation/Flow Field Particles")]
    public class FlowFieldParticles : FlowParticles
    {
        public bool isBounded = false;

        ParticleSystem.CollisionModule collisionModule;

        public override void OnValidate()
        {
            base.OnValidate();
            particleSpawnRate = Mathf.Max(0, particleSpawnRate);
            particleSize = Mathf.Max(0, particleSize);
            particleLife = Mathf.Max(0, particleLife);
        }

        //public override void Start()
        //{
        //    base.Start();

        //    UpdateParticleSystemSettings();
        //}

        public override void GetParticleComponents()
        {
            base.GetParticleComponents();

            collisionModule = m_ParticleSystem.collision;
        }

        public override void UpdateParticleSystemSettings()
        {
            mainModule.startSize = particleSize / transform.lossyScale.magnitude;
            mainModule.startLifetime = particleLife;

            emissionModule.rateOverTime = particleSpawnRate;

            trailModule.enabled = enableParticleTrails;
            if (enableParticleTrails)
            {
                particleSystemRenderer.renderMode = ParticleSystemRenderMode.None;
                collisionModule.lifetimeLoss = 0;
            }
            else
            {
                particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                collisionModule.lifetimeLoss = 1;
            }

            collisionModule.enabled = isBounded;
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
