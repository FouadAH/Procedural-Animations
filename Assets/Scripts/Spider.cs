using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Spider : MonoBehaviour
{
    public Transform spiderController;

    public Transform spiderBody;
    public Transform orientation;

    [Header("Leg Settings")]
    public int legCount = 6;

    public Transform[] legTargetPoints;
    public Transform[] legTransforms;
    public Transform[] legIKTargets;

    [Header("Leg Movement Settings")]
    public float velocityMultiplier;
    public float smoothness = 5;
    public float stepSize = 1f;
    public float stepHeight = 0.5f;
    public float maxStepHeight = 0.5f;

    [Header("Cycle Settings")]
    public double[] footTimings = new double[4];
    public float cycleSpeed;
    public float cycleSpeedModifier;
    public float timingOffset = 0.25f;
    public float[] timingOf = new float[4];

    public float cycleLimit;

    [Header("Collision Detection Settings")]

    public LayerMask groundMask;

    public int raycastCount = 5;

    public float castOriginOffsetY = 1f;
    public float castDistance = 100f;

    [Header("Cone Cast Settings")]
    public float raycastAngleY = 45;
    public float raycastAngleX = 45;
    
    public float raycastAngleOffsetY = 45;
    public float raycastAngleOffsetX = 45;

    [Header("Gizmos Flags")]
    public bool debugCastGizmo;
    public bool debugNormalGizmo;
    public bool debugTargetPositionGizmo;
    public bool debugDesiredPositionGizmo;

    //[Header("Flags")]
    //public bool stickToHitPosition;

    float averageLegHeight;

    Vector3 lastBodyPosition;
    Vector3 velocity;
    Vector3 lastBodyUp;

    Vector3[] lastLegPositions = new Vector3[4];
    Vector3[] targetPosition = new Vector3[4];
    Vector3[] desiredPosition = new Vector3[4];
    Vector3[] hitPosition = new Vector3[4];


    bool[] legState = new bool[4];
    int indexToMove = -1;
    RaycastHit hit;

    private void Start()
    {
        lastLegPositions = new Vector3[legCount];
        targetPosition = new Vector3[legCount];
        desiredPosition = new Vector3[legCount];
        hitPosition = new Vector3[legCount];
        
        legState = new bool[legCount];

        for (int i = 0; i < legIKTargets.Length; i++)
        {
            lastLegPositions[i] = legIKTargets[i].position;
        }

        lastBodyUp = Vector3.up;
    }

    void FixedUpdate()
    {
        LegIK();
        MoveBody();
    }

    void LegIK()
    {
        velocity = transform.position - lastBodyPosition;

        float time = Time.deltaTime;
        Vector3 castDirection = -transform.up;

        for (int i = 0; i < legTargetPoints.Length; i++)
        {
            var hit = LegRaycast(legTargetPoints[i].position, castDirection);
            if (hit.collider)
            {
                hitPosition[i] = hit.point;
                desiredPosition[i] = hit.point;
                //desiredPosition[i] = new Vector3(legTargetPoints[i].position.x, hit.point.y, legTargetPoints[i].position.z);
            }

            targetPosition[i] = desiredPosition[i];

            //Foot movement timing
            footTimings[i] += time * (cycleSpeed * cycleSpeedModifier);
            if (footTimings[i] >= cycleLimit + timingOf[i])
            {
                footTimings[i] = timingOf[i];
                indexToMove = i;
            }

            //Check if should move foot, then move it
            if (indexToMove != -1 && legState[indexToMove] == false)
            {
                float distance = Vector3.ProjectOnPlane((legTargetPoints[i].position) - lastLegPositions[i], Vector3.up).magnitude;
                if (distance > stepSize || velocity.magnitude > 0.01f)
                {
                    StartCoroutine(PerformStep(indexToMove, targetPosition[indexToMove]));
                }
            }
        }

        for (int i = 0; i < legCount; ++i)
        {
            if (i != indexToMove)
            {
                legIKTargets[i].position = lastLegPositions[i];
            }
        }
    }

    RaycastHit LegRaycast(Vector3 legTargetPosition, Vector3 direction)
    {
        Vector3 castOffset = transform.up * castOriginOffsetY;
        Vector3 stepHeightOffset = transform.up * stepHeight;
        Vector3 velocityOffset = velocity * velocityMultiplier;

        Vector3 origin = legTargetPosition + velocityOffset + castOffset + stepHeightOffset;
        Physics.SphereCast(origin, 0.01f, direction, out hit, castDistance, groundMask);

        if (debugCastGizmo)
        {
            Debug.DrawRay(origin, direction * castDistance, Color.blue);
            //ConeRaycast(origin, generalDirection, raycastCount, raycastAngleOffsetX, raycastAngleOffsetY);
        }
       
        return hit;
    }

    void MoveBody()
    {
        CalculateAverageHeight();
        CalculateNormal();

        orientation.position = new Vector3((float)spiderBody.position.x, averageLegHeight, (float)spiderBody.position.z);
        spiderController.position = Vector3.Lerp(spiderBody.position, new Vector3(spiderBody.position.x, averageLegHeight, spiderBody.position.z), 1f / (float)(smoothness + 1));
    }

    void CalculateAverageHeight()
    {
        lastBodyPosition = transform.position;

        averageLegHeight = 0;
        foreach (Vector3 legTransform in desiredPosition)
        {
            averageLegHeight += legTransform.y;
        }
        averageLegHeight /= legCount;
    }

    void CalculateNormal()
    {
        lastBodyPosition = transform.position;
        Vector3 v1 = desiredPosition[0] - desiredPosition[3];
        Vector3 v2 = desiredPosition[1] - desiredPosition[2];
        Vector3 normal = Vector3.Cross(v1, v2).normalized;

        Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));

        Vector3 forwardProjected = Vector3.ProjectOnPlane(orientation.forward, up).normalized;
        spiderController.rotation = Quaternion.LookRotation(forwardProjected, up);
        transform.rotation = Quaternion.LookRotation(forwardProjected, up);
        lastBodyUp = up;

        if (debugNormalGizmo)
        {
            Debug.DrawRay(desiredPosition[3], v1, Color.red);
            Debug.DrawRay(desiredPosition[2], v2, Color.red);
            Debug.DrawRay(transform.position, normal, Color.magenta);
            Debug.DrawRay(transform.position, orientation.forward, Color.magenta);
            Debug.DrawRay(transform.position, forwardProjected, Color.yellow);
        }
    }

    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        //Debug.Log("PerformStep");
        if (legState[index] == false)
        {
            legState[index] = true;

            Vector3 startPos = legIKTargets[index].position;
            for (int i = 1; i <= smoothness; ++i)
            {
                targetPoint = targetPosition[index] + velocity * velocityMultiplier;
                //Debug.Log($"target pos: {targetPoint}, velocity: {velocity * velocityMultiplier}");
                legIKTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (float)(smoothness + 1f));
                legIKTargets[index].position += Mathf.Sin(i / (float)(smoothness + 1f) * Mathf.PI) * stepHeight * transform.up;
                yield return new WaitForFixedUpdate();
            }

            legIKTargets[index].position = targetPoint;
            lastLegPositions[index] = legIKTargets[index].position;

            if (indexToMove == index)
            {
                indexToMove = -1;
            }

            legState[index] = false;
        }
    }

    void ConeRaycast(Vector3 origin, Vector3 direction, int count, float angleOffsetX, float angleOffsetY)
    {
        float angleX = raycastAngleX;
        float angleY = raycastAngleY;

        for (int i = 0; i < count; i++)
        {
            Quaternion rayAngleOffsetX = Quaternion.AngleAxis(angleX % 360, orientation.right.normalized);
            Quaternion rayAngleOffsetY = Quaternion.AngleAxis(angleY % 360, -orientation.up);

            Vector3 rayDirection = rayAngleOffsetX * direction;
            rayDirection = rayAngleOffsetY * rayDirection;

            Debug.DrawRay(origin, rayDirection, Color.red);

            angleX += angleOffsetX;
            angleY += angleOffsetY;
        }

        angleX = raycastAngleX;
        angleY = raycastAngleY;

        for (int i = 0; i < count; i++)
        {
            Quaternion rayAngleOffsetX = Quaternion.AngleAxis(angleX % 360, orientation.right.normalized);
            Quaternion rayAngleOffsetY = Quaternion.AngleAxis(angleY % 360, -orientation.up);

            Vector3 rayDirection = rayAngleOffsetX * direction;
            rayDirection = rayAngleOffsetY * rayDirection;

            Debug.DrawRay(origin, rayDirection, Color.red);

            angleX -= angleOffsetX;
            angleY -= angleOffsetY;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (debugTargetPositionGizmo)
        {
            foreach (Transform legTarget in legTargetPoints)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(legTarget.position, stepSize);
            }
        }

        if (debugDesiredPositionGizmo)
        {
            foreach (var pos in desiredPosition)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(pos, 0.05f);
            }

            //foreach (var pos in hitPosition)
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawWireSphere(pos, 0.05f);
            //}
        }
    }
}
