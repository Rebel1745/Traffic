using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Camera _camera;
    private Transform _followTarget;
    [SerializeField] private Vector3 _offset;
    private bool _isStatic = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void LateUpdate()
    {
        if (_followTarget == null) return;
        if (_isStatic) return;

        _camera.transform.position = new Vector3(_followTarget.position.x, 0f, _followTarget.position.z) - _offset;
    }

    public void SetFollowTarget(Vector3 target, Vector3 offset, Vector3 rotation)
    {
        _offset = offset;
        _camera.transform.position = new Vector3(target.x, 0f, target.z) - offset;
        _camera.transform.rotation = Quaternion.Euler(rotation);
        _camera.gameObject.SetActive(true);
    }

    public void SetFollowTarget(Transform target, Vector3 offset, Vector3 rotation)
    {
        _offset = offset;
        _followTarget = target;
        _camera.transform.rotation = Quaternion.Euler(rotation);
        _camera.gameObject.SetActive(true);
    }

    public void StopFollowing()
    {
        _followTarget = null;
        _camera.gameObject.SetActive(false);
    }
}
