using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockUnits : MonoBehaviour
{
    [SerializeField] private float FOVAngle;
    [SerializeField] private float smoothDamp;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Vector3[] directionsToCheckWhenAvoidingObstacles;

    private List<FlockUnits> cohesionNeighbours = new List<FlockUnits>();
    private List<FlockUnits> avoidanceNeighbours = new List<FlockUnits>();
    private List<FlockUnits> alignmentNeighbours = new List<FlockUnits>();
    private Flock assignedFlock;
    private Vector3 currentVelocity;
    private Vector3 currentObstacleAvoidanceVector;
    private float speed;
    public Transform Mytransform { get; set; }


    private void Awake()
    {
        Mytransform = transform;
    }

    public void InitializeSpeed(float speed)
    {
        this.speed = speed;
    }

    public void MoveUnits()
    {
        FindNeighbours();
        CalculateSpeed();
        var cohesionVector = CalculateCohesionVector() * assignedFlock.cohesionWeight;
        var avoidanceVector = CalculateAvoidanceVector() * assignedFlock.avoidanceWeight;
        var alignmentVector = CalculateAlignmentVector() * assignedFlock.alignmentWeight;
        var boundsVector = CalculateBoundsVector() * assignedFlock.boundsWeight;
        var obstacleAvoidVector = ObstacleAvoidanceVector() * assignedFlock.obstacleAvoidanceWeight;


        var moveVector = cohesionVector + avoidanceVector + alignmentVector + boundsVector + obstacleAvoidVector;
        moveVector = Vector3.SmoothDamp(Mytransform.forward, moveVector, ref currentVelocity, smoothDamp);
        moveVector = moveVector.normalized * speed;
        if (moveVector == Vector3.zero)
            moveVector = transform.forward;
        Mytransform.forward = moveVector;
        Mytransform.position += moveVector * Time.deltaTime;
    }



    private void CalculateSpeed()
    {
        if (cohesionNeighbours.Count == 0)
            return;
        speed = 0;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            speed += cohesionNeighbours[i].speed;
        }
        speed /= cohesionNeighbours.Count;
        speed = Mathf.Clamp(speed, assignedFlock.minSpeed, assignedFlock.maxSpeed);
    }

    public void AssignFlock(Flock flock)
    {
        assignedFlock = flock;
    }

    private void FindNeighbours()
    {
        cohesionNeighbours.Clear();
        avoidanceNeighbours.Clear();
        alignmentNeighbours.Clear();
        var allUnits = assignedFlock.allUnits;
        for (int i = 0; i < allUnits.Length; i++)
        {
            var currentUnit = allUnits[i];
            if (currentUnit != this)
            {
                float currentNeighboursDistanceSqr = Vector3.SqrMagnitude(currentUnit.Mytransform.position - Mytransform.position);
                if (currentNeighboursDistanceSqr <= assignedFlock.cohesionDistance * assignedFlock.cohesionDistance)
                {
                    cohesionNeighbours.Add(currentUnit);
                }

                if (currentNeighboursDistanceSqr <= assignedFlock.avoidanceDistance * assignedFlock.avoidanceDistance)
                {
                    avoidanceNeighbours.Add(currentUnit);
                }

                if (currentNeighboursDistanceSqr <= assignedFlock.alignmentDistance * assignedFlock.alignmentDistance)
                {
                    alignmentNeighbours.Add(currentUnit);
                }
            }
        }
    }

    private Vector3 CalculateCohesionVector()
    {
        var cohesionVector = Vector3.zero;
        int neighboursinFOV = 0;
        if (cohesionNeighbours.Count == 0)
            return cohesionVector;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            if (IsInFOV(cohesionNeighbours[i].Mytransform.position))
            {
                neighboursinFOV++;
                cohesionVector += cohesionNeighbours[i].Mytransform.position;
            }
        }

        cohesionVector /= neighboursinFOV;
        cohesionVector -= Mytransform.position;
        cohesionVector = cohesionVector.normalized;
        return cohesionVector;
    }

    private bool IsInFOV(Vector3 position)
    {
        return Vector3.Angle(Mytransform.forward, position - Mytransform.position) <= FOVAngle;
    }


    private Vector3 CalculateAlignmentVector()
    {
        var alignmentVector = Mytransform.forward;

        if (alignmentNeighbours.Count == 0)
            return Mytransform.forward;

        int neighBoudsIFOV = 0;

        for (int i = 0; i < alignmentNeighbours.Count; i++)
        {
            if (IsInFOV(alignmentNeighbours[i].Mytransform.position))
            {
                neighBoudsIFOV++;
                alignmentVector += alignmentNeighbours[i].Mytransform.forward;
            }
        }

        alignmentVector /= neighBoudsIFOV;
        alignmentVector = alignmentVector.normalized;
        return alignmentVector;
    }

    private Vector3 CalculateAvoidanceVector()
    {
        var avoidanceVector = Vector3.zero;

        if (avoidanceNeighbours.Count == 0)
            return Vector3.zero;

        int neighBoudsIFOV = 0;

        for (int i = 0; i < avoidanceNeighbours.Count; i++)
        {
            if (IsInFOV(avoidanceNeighbours[i].Mytransform.position))
            {
                neighBoudsIFOV++;
                avoidanceVector += (Mytransform.position - avoidanceNeighbours[i].Mytransform.position);
            }
        }

        avoidanceVector /= neighBoudsIFOV;
        avoidanceVector = avoidanceVector.normalized;
        return avoidanceVector;
    }


    private Vector3 CalculateBoundsVector()
    {
        var offsetToCenter = assignedFlock.transform.position - Mytransform.position;
        bool isNearCenter = (offsetToCenter.magnitude >= assignedFlock.boundsDistance * 0.9f);
        return isNearCenter ? offsetToCenter.normalized : Vector3.zero;
    }

    private Vector3 ObstacleAvoidanceVector()
    {
        var obstacleVector = Vector3.zero;

        RaycastHit hit;

        if (Physics.Raycast(Mytransform.position, Mytransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
        {
            obstacleVector = FindBestDirectorToAvoidObstacle();
        }
        else
        {
            currentObstacleAvoidanceVector = Vector3.zero;
        }

        return obstacleVector;
    }

    private Vector3 FindBestDirectorToAvoidObstacle()
    {

        if(currentObstacleAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;
            if(Physics.Raycast(Mytransform.position, Mytransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
            {
                return currentObstacleAvoidanceVector;
            }
        }
        float maxDistance = int.MinValue;
        var selectedDirection = Vector3.zero;

        for (int i = 0; i < directionsToCheckWhenAvoidingObstacles.Length; i++)
        {
            RaycastHit hit;
            var currentDirection= Mytransform.TransformDirection(directionsToCheckWhenAvoidingObstacles[i].normalized);
            if(Physics.Raycast(Mytransform.position, currentDirection,out hit,  assignedFlock.obstacleDistance, obstacleMask))
            {
                float currentDistance = (hit.point - Mytransform.position).sqrMagnitude;
                if(currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    selectedDirection = currentDirection;
                }
            }
            else
            {
                selectedDirection = currentDirection;
                currentObstacleAvoidanceVector = currentDirection.normalized;
                return selectedDirection.normalized;
            }
        }
        return selectedDirection.normalized;

    }
}
