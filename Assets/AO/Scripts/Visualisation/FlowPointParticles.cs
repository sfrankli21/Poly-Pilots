using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Spawns particles which are affected by the flow from the position of the GameObject.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Visualisation/Flow Point Particles")]
    public class FlowPointParticles : FlowParticles
    {
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

            emissionModule.rateOverTime = particleSpawnRate;

            trailModule.enabled = enableParticleTrails;
            particleSystemRenderer.renderMode = enableParticleTrails ? ParticleSystemRenderMode.None : ParticleSystemRenderMode.Billboard;
        }
    }
}
