namespace backend.DTO.HealthCheck;

public enum ComponentType
{
    Server,
    Component,
}

public class ServerStatus
{
    public ComponentStatus? Server { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public List<ComponentStatus> Dependencies { get; set; } = new();
}
