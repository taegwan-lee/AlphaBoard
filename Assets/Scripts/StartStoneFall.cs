using UnityEngine;
using System.Collections.Generic;

public class StartStoneFall : MonoBehaviour
{
    public GameObject[] stonePrefabs; 
    public float spawnRate = 0.3f;
    public float fallSpeed = 0.3f;
    public float destroyY = -10f;
    public float rotationSpeedMin = 30f;
    public float rotationSpeedMax = 90f;

    private List<StoneData> activeStones = new List<StoneData>();

    class StoneData
    {
        public GameObject obj;
        public Vector3 rotationAxis;
        public float rotationSpeed;
    }

    void Start()
    {
        InvokeRepeating(nameof(SpawnStone), 0f, spawnRate);
    }

    void Update()
    {
        for (int i = activeStones.Count - 1; i >= 0; i--)
        {
            var stoneData = activeStones[i];
            var stone = stoneData.obj;

            if (stone == null) continue;

            // 떨어뜨리기
            stone.transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // 회전
            stone.transform.Rotate(stoneData.rotationAxis, stoneData.rotationSpeed * Time.deltaTime);

            // 제거 조건
            if (stone.transform.position.y < destroyY)
            {
                Destroy(stone);
                activeStones.RemoveAt(i);
            }
        }
    }

    void SpawnStone()
    {
        // 돌이 떨어질 중심 위치 (월드 좌표 기준)
        float centerX = 0f;     // 화면 중앙
        float rangeX = 1f;      // 좌우로 퍼지는 범위 제한
        float spawnY = 7.5f;     // 높이
        float spawnZ = -9f;     // 

        Vector3 spawnPos = new Vector3(
            Random.Range(centerX - rangeX, centerX + rangeX), // -2 ~ +2
            spawnY,
            spawnZ
        );

        GameObject prefab = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
        GameObject stone = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 리스트 추가 등 기존 기능 이어서 사용
        float randomRotationSpeed = Random.Range(rotationSpeedMin, rotationSpeedMax);
        Vector3 randomAxis = Random.onUnitSphere;

        activeStones.Add(new StoneData
        {
            obj = stone,
            rotationAxis = randomAxis,
            rotationSpeed = randomRotationSpeed
        });
    }
}
