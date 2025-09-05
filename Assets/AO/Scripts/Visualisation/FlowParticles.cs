using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Provides the base class for objects which manage a particle system that is affected by the flow.
    /// </summary>
    public class FlowParticles : FlowAffected
    {
        /// <summary>
        /// How will the particles know which fluid zones to collect velocities from. Collider will use the attached collider to sense fluid zones whereas list will refer to the collection assigned to the particles.
        /// </summary>
        //[Tooltip("How will the particles know which fluid zones to collect velocities from. Collider will use the attached collider to sense fluid zones whereas list will refer to the collection assigned to the particles.")]
        //public FlowDetectionType FlowDetectionType { get; set; } = FlowDetectionType.Collider;

        public bool enableParticleTrails = true;
        public float particleSpawnRate = 100;
        public float particleSize = 0.1f;
        public float particleLife = 3f;

        // This event is called for each particle to calculate its velocity based on any effectors
        //public Func<ParticleSystem.Particle, ParticleSystem.Particle> ParticleVelocityEvent;
        //public delegate void ActionRef<T>(ref T item);
        //public ActionRef<ParticleSystem.Particle> ParticleVelocityEvent;

        protected ParticleSystem m_ParticleSystem;
        protected ParticleSystem.TrailModule trailModule;
        protected ParticleSystem.MainModule mainModule;
        protected ParticleSystem.EmissionModule emissionModule;
        protected ParticleSystemRenderer particleSystemRenderer;
        protected ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1];
        [HideInInspector] public int nParticles;

        List<VelocityEventHandler> globalFluidVelocityFunctions = new List<VelocityEventHandler>();
        List<VelocityEventHandler> fluidVelocityFunctions = new List<VelocityEventHandler>();

        public virtual void OnValidate()
        {
            particleSpawnRate = Mathf.Max(0, particleSpawnRate);
            particleSize = Mathf.Max(0, particleSize);
            particleLife = Mathf.Max(0, particleLife);
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            GetParticleComponents();
            UpdateParticleSystemSettings();
        }

        public virtual void GetParticleComponents()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            trailModule = m_ParticleSystem.trails;
            emissionModule = m_ParticleSystem.emission;
            particleSystemRenderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();

            // Make sure the particle system is using world space instead of local!
            mainModule = m_ParticleSystem.main;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        public virtual void OnEnable()
        {
            if (m_ParticleSystem != null)
            {
                emissionModule.enabled = true;
            }
        }

        public virtual void OnDisable()
        {
            if (m_ParticleSystem != null)
            {
                emissionModule.enabled = false;
            }
        }

        public virtual void UpdateParticleSystemSettings()
        {
            mainModule.startSize = particleSize / transform.lossyScale.magnitude;
            mainModule.startLifetime = particleLife;

            emissionModule.rateOverTime = particleSpawnRate;

            trailModule.enabled = enableParticleTrails;
            particleSystemRenderer.renderMode = enableParticleTrails ? ParticleSystemRenderMode.None : ParticleSystemRenderMode.Billboard;
        }

        public virtual void CheckParticleArrayLength()
        {
            // This might be better as a != rather than a < but for now I'm just going with
            // what they have on the Unity Documentation
            if (particles == null || particles.Length < mainModule.maxParticles)
            {
                particles = new ParticleSystem.Particle[mainModule.maxParticles];
            }
        }

        private void LateUpdate()
        {
            UpdateParticleSystemSettings();

            UpdateFluidVolumes();

            // Will only update the array if we have fewer particles than we need
            CheckParticleArrayLength();

            // Get all the particles currently in the system
            nParticles = m_ParticleSystem.GetParticles(particles);

            // I'm not happy about doing this but it's hard to tell when we can actually set the velocity
            // of the particles instead of adding to their velocities, so for simplicity I'm just setting
            // all their velocities to zero and then going through and adding to their velocities from global and fluid volumes
            ResetParticleVelocities();

            // Add the global flow primitive velocities to the particles
            if (affectedByGlobalFluid)
            {
                globalFluidVelocityFunctions.Clear();
                GlobalFluid.GetInteractingVelocityFunctions(interactionID, globalFluidVelocityFunctions);

                if (globalFluidVelocityFunctions.Count > 0)
                {
                    for (int i = 0; i < nParticles; i++)
                    {
                        particle = particles[i];

                        for (int j = 0; j < globalFluidVelocityFunctions.Count; j++)
                        {
                            particle.velocity += globalFluidVelocityFunctions[j](particle.position);
                        }

                        particles[i] = particle;
                    }
                }
            }

            // Add the fluid volume velocities to the particles
            for (int volumeIdx = 0; volumeIdx < localFluidVolumes.Count; volumeIdx++)
            {
                fluidVolume = localFluidVolumes[volumeIdx];

                if (!fluidVolume.isActiveAndEnabled)
                {
                    continue;
                }

                interactingPrimitives.Clear();
                fluidVolume.GetInteractingPrimitives(interactionID, interactingPrimitives);

                for (int particleIdx = 0; particleIdx < nParticles; particleIdx++)
                {
                    particle = particles[particleIdx];

                    if (fluidVolume.IsPositionInsideZone(particle.position))
                    {
                        for (int primitiveIdx = 0; primitiveIdx < interactingPrimitives.Count; primitiveIdx++)
                        {
                            particle.velocity += interactingPrimitives[primitiveIdx].VelocityFunction(particle.position);
                        }
                    }

                    particles[particleIdx] = particle;
                }
            }

            // Apply the changes to the particles
            m_ParticleSystem.SetParticles(particles, nParticles);

        }

        void ResetParticleVelocities()
        {
            for (int i = 0; i < nParticles; i++)
            {
                particles[i].velocity = new Vector3(0, 0, 0);
            }
        }

        List<FlowPrimitive> interactingPrimitives = new List<FlowPrimitive>();
        ParticleSystem.Particle particle;
        FluidVolume fluidVolume;
    }
}