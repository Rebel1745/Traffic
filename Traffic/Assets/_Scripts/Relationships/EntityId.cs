using System;
using UnityEngine;

[System.Serializable]
public struct EntityId
{
    [SerializeField]
    private string _idString;

    // Public property to get/set the actual Guid type for your logic
    public Guid Id
    {
        get
        {
            // Safely parse the string back to a Guid, defaulting to Empty if invalid
            return Guid.TryParse(_idString, out var g) ? g : Guid.Empty;
        }
        set
        {
            _idString = value.ToString();
        }
    }

    public EntityId(Guid id)
    {
        _idString = id.ToString();
    }

    public static EntityId New() => new EntityId(Guid.NewGuid());

    public static readonly EntityId None = new EntityId(Guid.Empty);

    public bool IsValid => Id != Guid.Empty;

    public override bool Equals(object obj) => obj is EntityId other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();

    // creates and EntityId with a predefined string (rather than creating a new one) - used when loading an object that already has an Id
    public static EntityId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return None;

        // TryParse is safer than new Guid(value) because it won't crash if the string is bad
        if (Guid.TryParse(value, out Guid parsedGuid))
        {
            return new EntityId(parsedGuid);
        }

        Debug.LogWarning($"Failed to parse EntityId from string: {value}");
        return None;
    }

    public override string ToString() => _idString;
}