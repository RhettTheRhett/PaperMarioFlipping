using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadraticCurve : MonoBehaviour
{

    public Transform A;
    public Transform B;
    public Transform Control;

    public Vector3 evaluate3DCurve(float t, Transform start, Transform end, Transform control) {
        Vector3 ac = Vector3.Lerp(start.position, control.position, t);
        Vector3 cb = Vector3.Lerp(control.position, end.position, t);
        return Vector3.Lerp(ac, cb, t);
    }





/*
private void OnDrawGizmos() {
        if (A == null || B == null || Control == null ) {
            return;
        }

        for (int i = 0; i <20; i++) {
            Gizmos.DrawWireSphere(evaluate3DCurve(i / 20f, A, B, Control), 0.1f);
        }

    }
*/
}
