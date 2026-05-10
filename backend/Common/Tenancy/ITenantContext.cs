namespace PRC.Common.Tenancy;

public interface ITenantContext
{
    /// <summary>Null means SuperAdmin — sees all federations.</summary>
    Guid? TenantId { get; }
}
