using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainWorld : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Seed")]
    public int seed = 12345;
    public bool randomizeSeed = false;

    [Header("Chunk Settings")]
    public int chunkSize = 10000;
    public int heightmapResolution = 257;
    public int alphamapResolution = 256;
    public int baseMapResolution = 1024;
    public int detailResolution = 1024;
    public int terrainHeight = 2000;

    [Header("Streaming")]
    public bool autoCalculateVisibleRadiusFromFarClip = true;
    public float playerFarClipDistance = 30000f;
    public int extraChunkBuffer = 1;
    public int visibleRadiusInChunks = 4;

    [Header("Map Bounds")]
    public bool useFiniteMap = false;
    public int mapWidthInTiles = 9;
    public int mapLengthInTiles = 9;

    [Header("Base Terrain Noise")]
    public float terrainNoiseScale = 60000f;
    public int terrainOctaves = 5;
    public float terrainPersistence = 0.5f;
    public float terrainLacunarity = 2f;
    public Vector2 terrainNoiseOffset;
    public float baseHeightPower = 1.05f;

    [Header("Biome Noise")]
    public float biomeNoiseScale = 250000f;
    [Range(0f, 1f)] public float oceanThreshold = 0.3f;
    [Range(0f, 1f)] public float plainsThreshold = 0.68f;
    [Range(0.001f, 0.5f)] public float biomeBlendRange = 0.14f;
    [Range(0f, 1f)] public float seaLevel = 0.2f;

    [Header("Plains Shaping")]
    public float plainsHeightMultiplier = 0.18f;
    public float plainsSecondaryNoiseScale = 90000f;
    public float plainsSecondaryStrength = 0.025f;

    [Header("Mountain Shaping")]
    public float mountainHeightMultiplier = 1.65f;
    public float mountainDetailNoiseScale = 22000f;
    public float mountainDetailStrength = 0.08f;
    public float mountainRidgeNoiseScale = 30000f;
    public float mountainRidgeStrength = 0.14f;

    [Header("Terrain Layers")]
    public TerrainLayer plainsLayer;
    public TerrainLayer oceanLayer;
    public TerrainLayer mountainLayer;

    [Header("Terrain Rendering")]
    public Material terrainMaterial;
    public bool drawInstanced = true;
    public float heightmapPixelError = 10f;

    Dictionary<Vector2Int, Terrain> activeChunks = new Dictionary<Vector2Int, Terrain>();
    Vector2[] terrainOctaveOffsets;
    float maxPossibleTerrainNoiseHeight;
    Vector2Int currentPlayerChunk;
    int minChunkX;
    int maxChunkX;
    int minChunkZ;
    int maxChunkZ;

    struct BiomeWeights
    {
        public float ocean;
        public float plains;
        public float mountain;

        public void Normalize()
        {
            float total = ocean + plains + mountain;

            if (total <= 0f)
            {
                plains = 1f;
                ocean = 0f;
                mountain = 0f;
                return;
            }

            ocean /= total;
            plains /= total;
            mountain /= total;
        }
    }

    void Start()
    {
        if (randomizeSeed)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        if (autoCalculateVisibleRadiusFromFarClip)
        {
            visibleRadiusInChunks = Mathf.CeilToInt(playerFarClipDistance / chunkSize) + extraChunkBuffer;
        }

        CalculateMapBounds();
        BuildTerrainOctaveOffsets();
        CalculateMaxPossibleTerrainNoiseHeight();
        currentPlayerChunk = GetPlayerChunkCoord();
        UpdateVisibleChunks();
    }

    void Update()
    {
        Vector2Int newPlayerChunk = GetPlayerChunkCoord();

        if (newPlayerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = newPlayerChunk;
            UpdateVisibleChunks();
        }
    }

    void CalculateMapBounds()
    {
        int halfWidth = mapWidthInTiles / 2;
        int halfLength = mapLengthInTiles / 2;

        if (mapWidthInTiles % 2 == 0)
        {
            minChunkX = -halfWidth;
            maxChunkX = halfWidth - 1;
        }
        else
        {
            minChunkX = -halfWidth;
            maxChunkX = halfWidth;
        }

        if (mapLengthInTiles % 2 == 0)
        {
            minChunkZ = -halfLength;
            maxChunkZ = halfLength - 1;
        }
        else
        {
            minChunkZ = -halfLength;
            maxChunkZ = halfLength;
        }
    }

    void BuildTerrainOctaveOffsets()
    {
        terrainOctaveOffsets = new Vector2[terrainOctaves];
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < terrainOctaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + terrainNoiseOffset.x;
            float offsetY = prng.Next(-100000, 100000) + terrainNoiseOffset.y;
            terrainOctaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
    }

    void CalculateMaxPossibleTerrainNoiseHeight()
    {
        maxPossibleTerrainNoiseHeight = 0f;
        float amplitude = 1f;

        for (int i = 0; i < terrainOctaves; i++)
        {
            maxPossibleTerrainNoiseHeight += amplitude;
            amplitude *= terrainPersistence;
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        if (player == null)
        {
            return Vector2Int.zero;
        }

        int chunkX = Mathf.FloorToInt(player.position.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(player.position.z / chunkSize);
        return new Vector2Int(chunkX, chunkZ);
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();

        for (int z = -visibleRadiusInChunks; z <= visibleRadiusInChunks; z++)
        {
            for (int x = -visibleRadiusInChunks; x <= visibleRadiusInChunks; x++)
            {
                Vector2Int coord = new Vector2Int(currentPlayerChunk.x + x, currentPlayerChunk.y + z);

                if (IsWithinMapBounds(coord))
                {
                    neededChunks.Add(coord);

                    if (!activeChunks.ContainsKey(coord))
                    {
                        CreateChunk(coord);
                    }
                }
            }
        }

        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, Terrain> pair in activeChunks)
        {
            if (!neededChunks.Contains(pair.Key))
            {
                chunksToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < chunksToRemove.Count; i++)
        {
            RemoveChunk(chunksToRemove[i]);
        }

        StitchNeighbors();
    }

    bool IsWithinMapBounds(Vector2Int coord)
    {
        if (!useFiniteMap)
        {
            return true;
        }

        if (coord.x < minChunkX) return false;
        if (coord.x > maxChunkX) return false;
        if (coord.y < minChunkZ) return false;
        if (coord.y > maxChunkZ) return false;
        return true;
    }

    void CreateChunk(Vector2Int coord)
    {
        if (activeChunks.ContainsKey(coord))
        {
            return;
        }

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.baseMapResolution = baseMapResolution;
        terrainData.SetDetailResolution(detailResolution, 8);
        terrainData.size = new Vector3(chunkSize, terrainHeight, chunkSize);

        if (HasAllTerrainLayers())
        {
            terrainData.terrainLayers = GetTerrainLayers();
        }

        float[,] heights = GenerateHeights(coord, heightmapResolution);
        terrainData.SetHeights(0, 0, heights);

        if (HasAllTerrainLayers())
        {
            float[,,] alphamaps = GenerateAlphamaps(coord, terrainData.alphamapResolution);
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }

        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "Terrain_Chunk_" + coord.x + "_" + coord.y;
        terrainObject.transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();

        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }

        terrain.drawInstanced = drawInstanced;
        terrain.heightmapPixelError = heightmapPixelError;
        terrain.basemapDistance = playerFarClipDistance + chunkSize;
        terrainCollider.terrainData = terrainData;

        activeChunks.Add(coord, terrain);
    }

    void RemoveChunk(Vector2Int coord)
    {
        if (!activeChunks.ContainsKey(coord))
        {
            return;
        }

        Terrain terrain = activeChunks[coord];

        if (terrain != null)
        {
            Destroy(terrain.gameObject);
        }

        activeChunks.Remove(coord);
    }

    void StitchNeighbors()
    {
        foreach (KeyValuePair<Vector2Int, Terrain> pair in activeChunks)
        {
            Vector2Int coord = pair.Key;

            Terrain left = activeChunks.ContainsKey(new Vector2Int(coord.x - 1, coord.y)) ? activeChunks[new Vector2Int(coord.x - 1, coord.y)] : null;
            Terrain top = activeChunks.ContainsKey(new Vector2Int(coord.x, coord.y + 1)) ? activeChunks[new Vector2Int(coord.x, coord.y + 1)] : null;
            Terrain right = activeChunks.ContainsKey(new Vector2Int(coord.x + 1, coord.y)) ? activeChunks[new Vector2Int(coord.x + 1, coord.y)] : null;
            Terrain bottom = activeChunks.ContainsKey(new Vector2Int(coord.x, coord.y - 1)) ? activeChunks[new Vector2Int(coord.x, coord.y - 1)] : null;

            pair.Value.SetNeighbors(left, top, right, bottom);
        }
    }

    float[,] GenerateHeights(Vector2Int chunkCoord, int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        float step = (float)chunkSize / (resolution - 1);

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                double worldX = (chunkCoord.x * (double)chunkSize) + (x * step);
                double worldZ = (chunkCoord.y * (double)chunkSize) + (z * step);

                BiomeWeights biomeWeights = GetBiomeWeights(worldX, worldZ);
                float rawHeight = GetRawHeight01(worldX, worldZ);

                float oceanHeight = GetOceanHeight(worldX, worldZ, rawHeight);
                float plainsHeight = GetPlainsHeight(worldX, worldZ, rawHeight);
                float mountainHeight = GetMountainHeight(worldX, worldZ, rawHeight);

                float blendedHeight =
                    (oceanHeight * biomeWeights.ocean) +
                    (plainsHeight * biomeWeights.plains) +
                    (mountainHeight * biomeWeights.mountain);

                heights[z, x] = Mathf.Clamp01(blendedHeight);
            }
        }

        return heights;
    }

    float[,,] GenerateAlphamaps(Vector2Int chunkCoord, int resolution)
    {
        float[,,] alphamaps = new float[resolution, resolution, 3];
        float step = (float)chunkSize / (resolution - 1);

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                double worldX = (chunkCoord.x * (double)chunkSize) + (x * step);
                double worldZ = (chunkCoord.y * (double)chunkSize) + (z * step);

                BiomeWeights biomeWeights = GetBiomeWeights(worldX, worldZ);

                float plainsWeight = biomeWeights.plains;
                float oceanWeight = biomeWeights.ocean;
                float mountainWeight = biomeWeights.mountain;

                float total = plainsWeight + oceanWeight + mountainWeight;

                if (total <= 0f)
                {
                    plainsWeight = 1f;
                    total = 1f;
                }

                alphamaps[z, x, 0] = plainsWeight / total;
                alphamaps[z, x, 1] = oceanWeight / total;
                alphamaps[z, x, 2] = mountainWeight / total;
            }
        }

        return alphamaps;
    }

    BiomeWeights GetBiomeWeights(double worldX, double worldZ)
    {
        float sample = Mathf.PerlinNoise(
            (float)(worldX / biomeNoiseScale) + 1000.137f + SeedOffset01(11) * 5000f,
            (float)(worldZ / biomeNoiseScale) + 2000.271f + SeedOffset01(23) * 5000f
        );

        float ocean = 1f - SmoothBand(sample, oceanThreshold - biomeBlendRange, oceanThreshold + biomeBlendRange);
        float mountain = SmoothBand(sample, plainsThreshold - biomeBlendRange, plainsThreshold + biomeBlendRange);
        float plainsLow = SmoothBand(sample, oceanThreshold - biomeBlendRange, oceanThreshold + biomeBlendRange);
        float plainsHigh = 1f - SmoothBand(sample, plainsThreshold - biomeBlendRange, plainsThreshold + biomeBlendRange);
        float plains = plainsLow * plainsHigh;

        BiomeWeights weights;
        weights.ocean = Mathf.Clamp01(ocean);
        weights.plains = Mathf.Clamp01(plains);
        weights.mountain = Mathf.Clamp01(mountain);
        weights.Normalize();
        return weights;
    }

    float SmoothBand(float value, float min, float max)
    {
        if (max <= min)
        {
            return value >= max ? 1f : 0f;
        }

        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    }

    float GetRawHeight01(double worldX, double worldZ)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float safeNoiseScale = terrainNoiseScale <= 0f ? 0.0001f : terrainNoiseScale;

        for (int i = 0; i < terrainOctaves; i++)
        {
            float sampleX = (float)(worldX / safeNoiseScale * frequency) + terrainOctaveOffsets[i].x;
            float sampleZ = (float)(worldZ / safeNoiseScale * frequency) + terrainOctaveOffsets[i].y;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
            noiseHeight += perlinValue * amplitude;

            amplitude *= terrainPersistence;
            frequency *= terrainLacunarity;
        }

        float normalized = (noiseHeight + maxPossibleTerrainNoiseHeight) / (maxPossibleTerrainNoiseHeight * 2f);
        normalized = Mathf.Clamp01(normalized);
        normalized = Mathf.Pow(normalized, baseHeightPower);
        return normalized;
    }

    float GetOceanHeight(double worldX, double worldZ, float rawHeight)
    {
        return seaLevel;
    }

    float GetPlainsHeight(double worldX, double worldZ, float rawHeight)
    {
        float secondary = Mathf.PerlinNoise(
            (float)(worldX / plainsSecondaryNoiseScale) + 3000.41f + SeedOffset01(37) * 2000f,
            (float)(worldZ / plainsSecondaryNoiseScale) + 4000.79f + SeedOffset01(41) * 2000f
        );

        float plainsBase = Mathf.Lerp(seaLevel, rawHeight, plainsHeightMultiplier);
        float plainsDetail = (secondary - 0.5f) * plainsSecondaryStrength;
        return Mathf.Clamp01(plainsBase + plainsDetail);
    }

    float GetMountainHeight(double worldX, double worldZ, float rawHeight)
    {
        float mountainBase = Mathf.Clamp01(rawHeight * mountainHeightMultiplier);

        float mountainDetail = Mathf.PerlinNoise(
            (float)(worldX / mountainDetailNoiseScale) + 5000.13f + SeedOffset01(53) * 2000f,
            (float)(worldZ / mountainDetailNoiseScale) + 6000.61f + SeedOffset01(59) * 2000f
        );
        mountainDetail = (mountainDetail - 0.5f) * mountainDetailStrength;

        float ridge = Mathf.PerlinNoise(
            (float)(worldX / mountainRidgeNoiseScale) + 7000.87f + SeedOffset01(61) * 2000f,
            (float)(worldZ / mountainRidgeNoiseScale) + 8000.19f + SeedOffset01(67) * 2000f
        );
        ridge = Mathf.Abs(ridge - 0.5f) * 2f;
        ridge = Mathf.Pow(ridge, 1.5f) * mountainRidgeStrength;

        return Mathf.Clamp01(mountainBase + mountainDetail + ridge);
    }

    bool HasAllTerrainLayers()
    {
        return plainsLayer != null &&
               oceanLayer != null &&
               mountainLayer != null;
    }

    TerrainLayer[] GetTerrainLayers()
    {
        return new TerrainLayer[]
        {
            plainsLayer,
            oceanLayer,
            mountainLayer
        };
    }

    float SeedOffset01(int salt)
    {
        return Hash01(seed, salt, salt * 17 + 3);
    }

    float Hash01(int x, int z, int salt)
    {
        uint h = (uint)(x * 374761393);
        h += (uint)(z * 668265263);
        h += (uint)(salt * 2246822519u);
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0x00FFFFFF) / 16777215f;
    }
}