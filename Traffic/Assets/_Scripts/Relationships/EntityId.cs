using System;

[System.Serializable]
public struct EntityId
{
    public Guid Id;

    public EntityId(Guid id)
    {
        Id = id;
    }

    public static EntityId New() => new EntityId(Guid.NewGuid());

    public static readonly EntityId None = new EntityId(Guid.Empty);

    public bool IsValid => Id != Guid.Empty;

    public override bool Equals(object obj) => obj is EntityId other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}