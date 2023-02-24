using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidAIController : MonoBehaviour
{
    public GameObject boidPrefab;
    [Space]
    public int boidCount = 100;
    public float boidSpawnRadius = 10;
    public float neighborhoodRadius = 5;

    // Array of references of the transform components on each boid.
    private BoidMotor[] boids;

    private List<BoidMotor> cacheFlockmates = new List<BoidMotor>();
    private Collider[] cacheOverlapTest;
    private int cacheOverlapCount;
    
    [Header("Steering")]
    public float separationWeight = 1.0f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.0f;
    [Space]
    public float forwardWeight = 1.0f;
    public float seekWeight = 1.0f;
    [Space]
    public float wanderWeight = 1.0f;
    public float wanderJitter = 1.0f;
    public float wanderRadius = 10.0f;

    private void Awake()
    {
        boids = new BoidMotor[boidCount];
        cacheFlockmates.Capacity = 32;
        cacheOverlapTest = new Collider[32];
    }

    private void Start()
    {
        Vector3 spawnOrigin = transform.position;

        for (int i = 0; i < boidCount; ++i)
        {
            boids[i] = Instantiate(boidPrefab, spawnOrigin + Random.insideUnitSphere * boidSpawnRadius, Quaternion.identity).GetComponent<BoidMotor>();
        }
    }

    private void FixedUpdate()
    {
        Vector3 boidTargetPos = transform.position;

        for (int i = 0; i < boidCount; ++i)
        {
            BoidMotor currentBoid = boids[i];
            Vector3 currentPos = currentBoid.rbody.position;
            Vector3 currentVel = currentBoid.rbody.velocity;

            Vector3 forces = new Vector3(0, 0, 0);

            // gather all of our flockmates
            cacheOverlapCount = Physics.OverlapSphereNonAlloc(currentPos, neighborhoodRadius, cacheOverlapTest);

            cacheFlockmates.Clear();
            for(int j = 0; j < cacheOverlapCount; ++j)
            {
                if(cacheOverlapTest[j].TryGetComponent<BoidMotor>(out var boid))
                {
                    cacheFlockmates.Add(boid);
                }
            }

            if (cacheFlockmates.Count > 0)
            {
                Vector3 sumOfPositions = new Vector3(0, 0, 0);
                Vector3 sumOfDirections = new Vector3(0, 0, 0);
                for (int j = 0; j < cacheFlockmates.Count; ++j)
                {
                    sumOfPositions += cacheFlockmates[j].rbody.position;
                    sumOfDirections += cacheFlockmates[j].rbody.velocity.normalized;
                }

                // Separation - steer to avoid crowding local flockmates
                Vector3 separationForce = Vector3.zero;
                for (int j = 0; j < cacheFlockmates.Count; ++j)
                {
                    separationForce += SteeringMethods.Flee(currentPos, cacheFlockmates[j].rbody.position, currentVel, currentBoid.flySpeed);
                }
                forces += separationForce * separationWeight;

                // Alignment - steer towards the average heading of local flockmates
                Vector3 alignmentForce = sumOfDirections / cacheFlockmates.Count;
                forces += alignmentForce * alignmentWeight;

                // Cohesion - steer to move towards the average position of local flockmates
                Vector3 cohesionForce = sumOfPositions / cacheFlockmates.Count;
                forces += (cohesionForce - currentPos) * cohesionWeight;
            }

            // CUSTOM ADDITIONS

            // always forward
            forces += SteeringMethods.Seek(currentPos, currentPos+currentBoid.CachedTransform.forward, currentVel, currentBoid.flySpeed) * forwardWeight;

            // seek towards target
            forces += SteeringMethods.Seek(currentPos, boidTargetPos, currentVel, currentBoid.flySpeed) * seekWeight;

            // wander
            forces += SteeringMethods.Wander(currentPos, wanderRadius, wanderJitter, currentVel, currentBoid.flySpeed) * wanderWeight;

            // INTEGRATE FORCES
            currentBoid.rbody.AddForce(forces);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, boidSpawnRadius);
    }
}
