using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Spider : MonoBehaviour
{
    public Transform spiderBody;
    public Transform spiderModel;

    Vector3 lastBodyPosition;

    Vector3 velocity;
    Vector3 lastVelocity;

    Vector3 lastBodyUp;

    public float velocityMultiplier;

    public Transform[] legTargetPoints;

    public Transform[] legTransforms;
    public Transform[] legIKTargets;

    public LayerMask groundMask;
    public float smoothness = 5;

    public float stepSize = 1f;
    public float stepHeight = 0.5f;

    bool isMovingLegTarget = false;

    Vector3[] lastLegPositions = new Vector3[4];
    Vector3[] targetPosition = new Vector3[4];
    Vector3[] desiredPosition = new Vector3[4];

    RaycastHit hit;
    public Rigidbody _rigidbody;
    public Transform orientation;
    int indexToMove = -1;
    int stepCount;


    private void Start()
    {
        for (int i = 0; i < legIKTargets.Length; i++)
        {
            lastLegPositions[i] = legIKTargets[i].position;
        }

        lastBodyUp = Vector3.up;

    }

    float averageLegHeight;
    float lastArgHeight;

    void FixedUpdate()
    {
        velocity = transform.position - lastBodyPosition;
        //velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        

        float maxDistance = stepSize;
        for (int i = 0; i < legTargetPoints.Length; i++)
        {
            Physics.Raycast(targetPosition[i] + transform.up * 0.5f, -transform.up, out hit, Mathf.Infinity, groundMask);
            Debug.DrawRay(targetPosition[i] + transform.up * 0.5f, -transform.up, Color.green);

            if (hit.collider)
            {
                legTargetPoints[i].position = new Vector3(legTargetPoints[i].position.x, hit.point.y, legTargetPoints[i].position.z);
            }

            targetPosition[i] = legTargetPoints[i].position;

            float distance = Vector3.ProjectOnPlane((legTargetPoints[i].position) - lastLegPositions[i], Vector3.up).magnitude;
            //Debug.Log(distance);
            if(distance > maxDistance)
            {
                maxDistance = distance;
                indexToMove = i;
            }
        }

        for (int i = 0; i < 4; ++i)
            if (i != indexToMove)
                legIKTargets[i].position = lastLegPositions[i];
            
        if(indexToMove != -1 && stepCount < 2)
        {
            StartCoroutine(PerformStep(indexToMove, targetPosition[indexToMove]));
        }

        lastBodyPosition = transform.position;

        averageLegHeight = 0;
        foreach (Transform legTransform in legIKTargets)
        {
            averageLegHeight += legTransform.position.y;
        }
        averageLegHeight /= 4;

        lastBodyPosition = transform.position;
        Vector3 v1 = legIKTargets[0].position - legIKTargets[3].position;
        Vector3 v2 = legIKTargets[1].position - legIKTargets[2].position;
        Vector3 normal = Vector3.Cross(v1, v2).normalized;

        Debug.DrawRay(legIKTargets[3].position, v1, Color.red);
        Debug.DrawRay(legIKTargets[2].position, v2, Color.red);
        Debug.DrawRay(transform.position, normal, Color.magenta);

        Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));
        //transform.up = up;
        //transform.forward = orientation.forward;

        //Vector3 rightProjected = Vector3.ProjectOnPlane(orientation.forward, up).normalized; // makes sure the vectors are perpendicular. You can skip this if you are already sure they are.
        transform.rotation = Quaternion.LookRotation(orientation.forward, up); // sets the forward vector to desiredForwardVector and the up vector to rightProjected. Now we need to rotate it so the right vector is at rightProjected
        //transform.rotation *= Quaternion.AngleAxis(-90f/*this might need to be positive*/, transform.forward);

        lastBodyUp = up;
        //Debug.Log("average leg height: " + averageLegHeight);
        orientation.position = new Vector3((float)spiderBody.position.x, averageLegHeight, (float)spiderBody.position.z);
        spiderBody.position = Vector3.Lerp(spiderBody.position, new Vector3(spiderBody.position.x, averageLegHeight, spiderBody.position.z), 1f / (float)(smoothness + 1));
    }

    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        //Debug.Log("PerformStep");
        isMovingLegTarget = true;
        Vector3 startPos = legIKTargets[index].position;
        stepCount++;
        for (int i = 1; i <= smoothness; ++i)
        {
            targetPoint = targetPosition[index] + velocity * velocityMultiplier;
            Debug.Log($"target pos: {targetPoint}, velocity: {velocity * velocityMultiplier}");
            legIKTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (float)(smoothness + 1f));
            legIKTargets[index].position += Mathf.Sin(i / (float)(smoothness + 1f) * Mathf.PI) * stepHeight * transform.up;
            yield return new WaitForFixedUpdate();
        }

        legIKTargets[index].position = targetPoint;
        lastLegPositions[index] = legIKTargets[index].position;

        isMovingLegTarget = false;
        stepCount--;
        indexToMove = -1;
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
