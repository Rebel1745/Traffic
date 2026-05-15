using UnityEngine;

public static class Utils
{
    public static Vector3 GetVectorWithSetHeight(Vector3 vector, float height)
    {
        return new Vector3(vector.x, height, vector.z);
    }

    public static float GetDistanceWithSetHeight(Vector3 v1, Vector3 v2, float height)
    {
        Vector3 newV1 = new(v1.x, height, v1.z);
        Vector3 newV2 = new(v2.x, height, v2.z);

        return Vector3.Distance(newV1, newV2);
    }
}
