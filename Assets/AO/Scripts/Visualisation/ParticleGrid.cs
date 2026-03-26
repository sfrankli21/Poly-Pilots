using UnityEngine;

namespace AerodynamicObjects
{
    public class ParticleGrid : FlowParticles
    {
        /// <summary>
        /// Number of rows in grid
        /// </summary>
        [Tooltip("Number of rows in grid")]
        public int rowCount = 5;
        /// <summary>
        /// Number of columns in grid
        /// </summary>
        [Tooltip("Number of columns in grid")]
        public int columnCount = 5;
        /// <summary>
        /// Number of points in each row or column
        /// </summary>
        [Tooltip("Number of points in each row or column")]
        public int pointCount = 20;
        Mesh mesh;
        Vector3[] vertices;

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
            CreateParticleGridMesh(); //Unreliable - created once and added manually in inspector for now. // Create the mesh that defines where particles are spawned

            m_ParticleSystem = GetComponent<ParticleSystem>();
            emissionModule = m_ParticleSystem.emission;
            emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, pointCount * (rowCount + columnCount)) });
            emissionModule.rateOverTime = 0;
            shapeModule = m_ParticleSystem.shape;
            shapeModule.shapeType = ParticleSystemShapeType.Mesh;
            shapeModule.mesh = mesh;
            //shapeModule.mesh = Resources.Load("ParticleGridMesh") as Mesh; // This is unreliable for some reason - mesh added manually in inspector
            shapeModule.meshSpawnMode = ParticleSystemShapeMultiModeValue.Loop; //spawn in sequence to get full converage
            shapeModule.meshSpawnSpeed = 10f;
            particleSystemRenderer = m_ParticleSystem.GetComponent<ParticleSystemRenderer>();

            mainModule = m_ParticleSystem.main;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.maxParticles = vertices.Length;

            UpdateParticleSystemSettings();
        }

        public override void UpdateParticleSystemSettings()
        {
            mainModule.startSize = particleSize / transform.lossyScale.magnitude;
            mainModule.startLifetime = particleLife;

            //emissionModule.rateOverTime = particleSpawnRate;

            //if (mainModule.duration != 1f / particleSpawnRate)
            //{
            //    emissionModule.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, pointCount * (rowCount + columnCount)) });
            //    m_ParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            //    mainModule.duration = 1f / particleSpawnRate;
            //    particles = null;
            //    m_ParticleSystem.Play(true);
            //}
        }

        void CreateParticleGridMesh()
        {

            vertices = new Vector3[pointCount * (rowCount + columnCount)];
            int k = 0;
            mesh = new Mesh();

            //create rows
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < pointCount; j++)
                {
                    vertices[k] = new Vector3((float)j / (pointCount - 1), (float)i / (rowCount - 1), 0);
                    k++;
                }
            }
            //create columns
            for (int i = 0; i < columnCount; i++)
            {
                for (int j = 0; j < pointCount; j++)
                {
                    vertices[k] = new Vector3((float)i / (rowCount - 1), (float)j / (pointCount - 1), 0);
                    k++;
                }
            }

            //mesh.vertices = vertices;
            mesh.SetVertices(vertices);
            //If there is a mesh creation error check that that directory structure below matches project structure
            //AssetDatabase.CreateAsset(mesh, "Assets/AO-2/Core/Resources/ParticleGridMesh.asset");
        }
    }
}
