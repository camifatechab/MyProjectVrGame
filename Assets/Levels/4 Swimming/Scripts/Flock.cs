using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [SerializeField] private GameObject flockUnitPrefab;
    [SerializeField] private int flockSize = 100;
    [SerializeField] private Vector3 spawnBounds = new Vector3(90, 10, 90);
    
    [Header("Speed Settings")]
    [Range(0, 10)] [SerializeField] private float _minSpeed = 2;
    [Range(0, 10)] [SerializeField] private float _maxSpeed = 5;
    
    [Header("Detection Distances")]
    [Range(0, 10)] [SerializeField] private float _cohesionDistance = 2.5f;
    [Range(0, 10)] [SerializeField] private float _avoidanceDistance = 1f;
    [Range(0, 10)] [SerializeField] private float _alignmentDistance = 2f;
    [Range(0, 100)] [SerializeField] private float _boundsDistance = 20f;
    [Range(0, 50)] [SerializeField] private float _obstacleDistance = 15f;

    [Header("Behaviour Weights")]
    [Range(0, 10)] [SerializeField] private float _cohesionWeight = 1.5f;
    [Range(0, 10)] [SerializeField] private float _avoidanceWeight = 1f;
    [Range(0, 10)] [SerializeField] private float _alignmentWeight = 1f;
    [Range(0, 10)] [SerializeField] private float _boundsWeight = 3f;
    [Range(0, 50)] [SerializeField] private float _obstacleWeight = 25f;
    
    public float cohesionDistance { get { return _cohesionDistance; } }
    public float avoidanceDistance { get { return _avoidanceDistance; } }
    public float alignmentDistance { get { return _alignmentDistance; } }
    public float boundsDistance { get { return _boundsDistance; } }
    public float obstacleDistance { get { return _obstacleDistance; } }
    public float cohesionWeight { get { return _cohesionWeight; } }
    public float avoidanceWeight { get { return _avoidanceWeight; } }
    public float alignmentWeight { get { return _alignmentWeight; } }
    public float boundsWeight { get { return _boundsWeight; } }
    public float obstacleWeight { get { return _obstacleWeight; } }
    public float minSpeed { get { return _minSpeed; } }
    public float maxSpeed { get { return _maxSpeed; } }
    
    public float speed;

    // References
    public GameObject[] allUnits { get; set; }

    private void Start()
    {
        GenerateUnits();
    }

    private void Update()
    {
        if (allUnits != null)
        {
            for (int i = 0; i < allUnits.Length; i++)
            {
                if (allUnits[i] != null)
                {
                    FlockUnit unit = allUnits[i].GetComponent<FlockUnit>();
                    if (unit != null)
                    {
                        unit.MoveUnit();
                    }
                }
            }
        }
    }

private void GenerateUnits()
    {
        allUnits = new GameObject[flockSize];
        for (int i = 0; i < flockSize; i++)
        {
            // Spawn fish throughout the entire water container
            // Water container: X=±25, Z=±25, Y from -20 (floor) to 0 (surface)
            // Keep fish away from walls and floor/surface
            float randomX = UnityEngine.Random.Range(-22f, 22f);  // Inside walls
            float randomY = UnityEngine.Random.Range(-18f, -2f);  // Throughout water depth
            float randomZ = UnityEngine.Random.Range(-22f, 22f);  // Inside walls
            
            var spawnPosition = transform.position + new Vector3(randomX, randomY, randomZ);
            var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            allUnits[i] = Instantiate(flockUnitPrefab, spawnPosition, rotation);
            
            FlockUnit unit = allUnits[i].GetComponent<FlockUnit>();
            if (unit != null)
            {
                unit.AssignFlock(this);
                unit.InitializeSpeed(UnityEngine.Random.Range(minSpeed, maxSpeed));
            }
            
            Rigidbody rb = allUnits[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearDamping = 1f;
            }
        }
    }
}