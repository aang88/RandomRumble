using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;
using System.Reflection;

[RequireComponent(typeof(NetworkObject))]
public class NetworkTransformFix : NetworkBehaviour
{
    private NetworkTransform _networkTransform;
    private Rigidbody _rigidbody;
    private PlayerMovement1 _playerMovement;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _playerMovement = GetComponent<PlayerMovement1>();
        
        // Get or add NetworkTransform component
        _networkTransform = GetComponent<NetworkTransform>();
        if (_networkTransform == null)
        {
            _networkTransform = gameObject.AddComponent<NetworkTransform>();
        }
        
        // We'll avoid direct property assignments since the NetworkTransform API seems different
        // Instead, we'll try to adapt using reflection where possible
        TrySetProperty(_networkTransform, "ClientAuthoritative", true);
        TrySetProperty(_networkTransform, "SendToOwner", false);
        
        // Note: These specific properties don't exist in your version
        // TrySetProperty(_networkTransform, "TransformSpace", ???); // Can't set enum if it doesn't exist
        
        // Configure physics if relevant properties exist
        if (_rigidbody != null)
        {
            TrySetProperty(_networkTransform, "SynchronizeVelocity", true);
            TrySetProperty(_networkTransform, "SynchronizeAngularVelocity", true);
        }
        
        // Debug available properties to help diagnose
        Debug.Log("Available NetworkTransform properties:");
        foreach (var prop in _networkTransform.GetType().GetProperties())
        {
            Debug.Log($"- {prop.Name} ({prop.PropertyType})");
        }
    }
    
    // Helper method to try setting a property if it exists
    private bool TrySetProperty(object obj, string propertyName, object value)
    {
        PropertyInfo prop = obj.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                prop.SetValue(obj, value);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set property {propertyName}: {e.Message}");
            }
        }
        return false;
    }

    // Helper method to check if a property exists
    private bool HasProperty(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName) != null;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Ensure rigidbody kinematic state is set correctly based on ownership
        if (_rigidbody != null)
        {
            if (!IsOwner)
            {
                // Remote players should use interpolation
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                // Local player uses continuous detection for better physics
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}
