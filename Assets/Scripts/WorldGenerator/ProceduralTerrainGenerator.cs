using UnityEngine;

public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Size")]
    public int heightmapResolution = 513;
    public int alphamapResolution = 512;
    public int baseMapResolution = 1024;
    public int detailResolution = 1024;
    public int terrainWidth = 2000;
    public int terrainLength = 2000;
    public int terrainHeight = 300;

    [Header("Seed Settings")]
    public bool randomizeSeed = true;
    public int seed = 0;
    public int generatedSeed = 0;

    [Header("Noise Settings")]
    public float noiseScale = 150f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 noiseOffset;

    [Header("Height Shaping")]
    [Range(0.1f, 10f)]
    public float heightMultiplier = 1.5f;
    [Range(0f, 1f)]
    public float flattenStrength = 0.0f;

    [Header("Placement")]
    public Vector3 terrainPosition = Vector3.zero;
    public bool generateOnStart = true;
    public bool destroyExistingTerrainObject = false;

    Terrain generatedTerrain;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateTerrain();
        }
    }

    public void GenerateTerrain()
    {
        if (randomizeSeed)
        {
            generatedSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        else
        {
            generatedSeed = seed;
        }

        if (destroyExistingTerrainObject)
        {
            Terrain existingTerrain = FindObjectOfType<Terrain>();
            if (existingTerrain != null)
            {
                Destroy(existingTerrain.gameObject);
            }
        }

        Terrain terrain = FindObjectOfType<Terrain>();

        if (terrain == null)
        {
            TerrainData newTerrainData = new TerrainData();
            GameObject terrainObject = Terrain.CreateTerrainGameObject(newTerrainData);
            terrainObject.name = "Procedural Terrain";
            terrainObject.transform.position = terrainPosition;
            terrain = terrainObject.GetComponent<Terrain>();
        }

        generatedTerrain = terrain;

        TerrainData terrainData = terrain.terrainData;
        terrainData.heightmapResolution = Mathf.ClosestPowerOfTwo(heightmapResolution - 1) + 1;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.baseMapResolution = baseMapResolution;
        terrainData.SetDetailResolution(detailResolution, 8);
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        float[,] heights = GenerateHeights(terrainData.heightmapResolution, terrainData.heightmapResolution);
        terrainData.SetHeights(0, 0, heights);

        Debug.Log("Terrain generated with seed: " + generatedSeed);
    }

    float[,] GenerateHeights(int width, int height)
    {
        float[,] heights = new float[height, width];

        System.Random prng = new System.Random(generatedSeed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + noiseOffset.x;
            float offsetY = prng.Next(-100000, 100000) + noiseOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (noiseScale <= 0f)
        {
            noiseScale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = ((x - halfWidth) / noiseScale) * frequency + octaveOffsets[i].x;
                    float sampleY = ((y - halfHeight) / noiseScale) * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }

                if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                heights[y, x] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heights[y, x]);
                normalizedHeight = Mathf.Pow(normalizedHeight, heightMultiplier);

                if (flattenStrength > 0f)
                {
                    normalizedHeight = Mathf.Lerp(normalizedHeight, 0.5f, flattenStrength);
                }

                heights[y, x] = Mathf.Clamp01(normalizedHeight);
            }
        }

        return heights;
    }
}