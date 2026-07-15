namespace ContentIntelligencePlatform.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El Id de una entidad no puede ser Guid.Empty.", nameof(id));
        Id = id;
    }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
