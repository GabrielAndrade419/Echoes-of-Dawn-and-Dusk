using UnityEngine;

public class CombatRelay : MonoBehaviour
{
    private PlayerController _playerController;

    void Start()
    {
        _playerController = GetComponentInParent<PlayerController>();
    }
    public void EnableTrail()
    {
        if (_playerController != null) _playerController.EnableTrail();
    }
    public void DisableTrail()
    {
        if (_playerController != null) _playerController.DisableTrail();
    }
}