#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    [InitializeOnLoad]
    public static class GizmoMeshLoader
    {
        [SerializeField]
        static Mesh ellipsoidMesh;

        [SerializeField]
        static Mesh cubeMesh;
        public static Mesh EllipsoidMesh
        {
            get
            {
                if (ellipsoidMesh == null)
                {
                    CreateEllipsoidMesh();
                }

                return ellipsoidMesh;
            }
        }

        public static Mesh CubeMesh
        {
            get
            {
                if (cubeMesh == null)
                {
                    CreateCubeMesh();
                }

                return cubeMesh;
            }
        }

        static GizmoMeshLoader()
        {
            if (ellipsoidMesh == null)
            {
                CreateEllipsoidMesh();
            }

            if (cubeMesh == null)
            {
                CreateCubeMesh();
            }
        }

        private static void CreateCubeMesh()
        {
            cubeMesh = new Mesh();

            Vector3[] verts = new Vector3[]
            {
                new Vector3(-0.5f,-0.5f,0.5f),   // bottom front left
                new Vector3(0.5f, -0.5f, 0.5f), // bottom front right
                new Vector3(0.5f, 0.5f, 0.5f), // top front right    
                new Vector3(-0.5f,0.5f,0.5f),   // top front left   
                new Vector3(-0.5f,0.5f,-0.5f),  // top back left   
                new Vector3(0.5f,0.5f,-0.5f),  // top back right     
                new Vector3(0.5f,-0.5f,-0.5f),  // bottom back right
                new Vector3(-0.5f,-0.5f,-0.5f),  // bottom back left
            };

            int[] tris = new int[]
            {
                0, 3, 1, //face front
	            1, 3, 2,
                2, 3, 4, //face top
	            2, 4, 5,
                1, 2, 6, //face right
	            2, 5, 6,
                0, 7, 4, //face left
	            0, 4, 3,
                5, 4, 7, //face back
	            5, 7, 6,
                0, 6, 7, //face bottom
	            0, 1, 6
            };

            cubeMesh.vertices = verts;
            cubeMesh.triangles = tris;
            cubeMesh.RecalculateBounds();
            cubeMesh.RecalculateNormals();
        }

        private static void CreateEllipsoidMesh()
        {
            ellipsoidMesh = new Mesh();

            float x = Mathf.Sqrt(0.5f * 0.5f * 0.5f);

            Vector3[] verts = new Vector3[]
            {
            new Vector3(0,0.5f,0),

            new Vector3(x,x,0),
            new Vector3(0,x,x),
            new Vector3(-x,x,0),
            new Vector3(0,x,-x),

            new Vector3(0.5f,0,0),
            new Vector3(x,0,x),
            new Vector3(0,0,0.5f),
            new Vector3(-x,0,x),
            new Vector3(-0.5f,0,0),
            new Vector3(-x,0,-x),
            new Vector3(0,0,-0.5f),
            new Vector3(x,0,-x),

            new Vector3(x,-x,0),
            new Vector3(0,-x,x),
            new Vector3(-x,-x,0),
            new Vector3(0,-x,-x),

            new Vector3(0,-0.5f,0),
            };

            int[] tris = new int[]
            {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 1,

            1, 5, 6,
            1, 6, 2,
            2, 6, 7,
            2, 7, 8,
            2, 8, 3,
            3, 8, 9,
            3, 9, 10,
            3, 10, 4,
            4, 10, 11,
            4, 11, 12,
            4, 12, 1,
            1, 12, 5,

            13, 5, 6,
            13, 6, 14,
            14, 6, 7,
            14, 7, 8,
            14, 8, 15,
            15, 8, 9,
            15, 9, 10,
            15, 10, 16,
            16, 10, 11,
            16, 11, 12,
            16, 12, 13,
            13, 12, 5,

            17, 13, 14,
            17, 14, 15,
            17, 15, 16,
            17, 16, 13
            };

            ellipsoidMesh.vertices = verts;
            ellipsoidMesh.triangles = tris;
            ellipsoidMesh.RecalculateBounds();
            ellipsoidMesh.RecalculateNormals();
        }
    }
}
#endif
