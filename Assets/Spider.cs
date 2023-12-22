using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Spider : MonoBehaviour
{
    public Transform spiderBody;

    public SpiderController spiderController;
    public Rigidbody _rigidbody;
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

    public float raycastAngle_1 = 45;
    public float raycastAngle_2 = 45;

    public float raycastAngleX_1 = 45;
    public float raycastAngleX_2 = 45;

    float averageLegHeight;

    Vector3 lastBodyPosition;
    Vector3 velocity;
    Vector3 lastBodyUp;

    Vector3[] lastLegPositions = new Vector3[4];
    Vector3[] targetPosition = new Vector3[4];
    Vector3[] desiredPosition = new Vector3[4];

    bool[] legState = new bool[4];
    int indexToMove = -1;
    RaycastHit hit;

    private void Start()
    {
        lastLegPositions = new Vector3[legCount];
        targetPosition = new Vector3[legCount];
        desiredPosition = new Vector3[legCount];
        legState = new bool[legCount];

        for (int i = 0; i < legIKTargets.Length; i++)
        {
            lastLegPositions[i] = legIKTargets[i].position;
        }

        lastBodyUp = Vector3.up;
    }

    void FixedUpdate()
    {
        velocity = transform.position - lastBodyPosition;

        float time = Time.deltaTime;
        for (int i = 0; i < legTargetPoints.Length; i++)
        {
            //Physics.Raycast(legTargetPoints[i].position + velocity * velocityMultiplier + transform.up * 0.5f + transform.up * stepHeight, -transform.up, out hit, 30, groundMask);
            Physics.SphereCast(legTargetPoints[i].position + velocity * velocityMultiplier + transform.up * 0.5f + transform.up * stepHeight, 0.1f, -transform.up, out hit, 30, groundMask);
            
            //Debug.DrawRay(legTargetPoints[i].position + velocity * velocityMultiplier + transform.up * 0.5f + transform.up * stepHeight, Quaternion.AngleAxis(raycastAngleX_1, orientation.right.normalized)* Quaternion.AngleAxis(raycastAngle_1, orientation.up)  * -Vector3.up, Color.red);
            //Debug.DrawRay(legTargetPoints[i].position + velocity * velocityMultiplier + transform.up * 0.5f + transform.up * stepHeight, Quaternion.AngleAxis(raycastAngleX_2, orientation.right.normalized)*Quaternion.AngleAxis(raycastAngle_2, orientation.up) * -Vector3.up, Color.red);
             
            if (hit.collider)
            {
                desiredPosition[i] = new Vector3(legTargetPoints[i].position.x, hit.point.y, legTargetPoints[i].position.z);
                Debug.DrawRay(desiredPosition[i],Vector3.up * 0.5f, Color.yellow, 0.5f, true);
            }

            targetPosition[i] = desiredPosition[i];
            float distance = Vector3.ProjectOnPlane((legTargetPoints[i].position) - lastLegPositions[i], Vector3.up).magnitude;
            footTimings[i] += time * (cycleSpeed * cycleSpeedModifier);

            if (footTimings[i] >= cycleLimit + timingOf[i])
            {
                footTimings[i] = timingOf[i];
                indexToMove = i;
            }

            if (indexToMove != -1 && legState[indexToMove] == false)
            {
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

        lastBodyPosition = transform.position;

        averageLegHeight = 0;
        foreach (Vector3 legTransform in desiredPosition)
        {
            averageLegHeight += legTransform.y;
        }
        averageLegHeight /= legCount;

        lastBodyPosition = transform.position;
        Vector3 v1 = desiredPosition[0] - desiredPosition[3];
        Vector3 v2 = desiredPosition[1] - desiredPosition[2];
        Vector3 normal = Vector3.Cross(v1, v2).normalized;

        Debug.DrawRay(desiredPosition[3], v1, Color.red);
        Debug.DrawRay(desiredPosition[2], v2, Color.red);
        Debug.DrawRay(transform.position, normal, Color.magenta);
        Debug.DrawRay(transform.position, orientation.forward, Color.magenta);

        Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));

        Vector3 forwardProjected = Vector3.ProjectOnPlane(orientation.forward, up).normalized; 
        Debug.DrawRay(transform.position, forwardProjected, Color.yellow);

        transform.rotation = Quaternion.LookRotation(forwardProjected, up);
        lastBodyUp = up;

        orientation.position = new Vector3((float)spiderBody.position.x, averageLegHeight, (float)spiderBody.position.z);
        spiderBody.position = Vector3.Lerp(spiderBody.position, new Vector3(spiderBody.position.x, averageLegHeight, spiderBody.position.z), 1f / (float)(smoothness + 1));
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

    private void OnDrawGizmosSelected()
    {
        foreach(Transform legTarget in legTargetPoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(legTarget.position, stepSize);
        }
    }
}
