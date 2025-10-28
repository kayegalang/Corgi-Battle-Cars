using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    void Update()
    {
        target.position = GetMouseWorldPosition(0f);
    }
    
    public static Vector3 GetMouseWorldPosition(float y = 0f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, y, 0));

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

}
