namespace TutorConnect.Domain.Common
{
    /// <summary>
    /// Base class for entities that use a BIGINT identity primary key.
    /// </summary>
    public abstract class BaseEntity
    {
        public long Id { get; protected set; }
    }
}
