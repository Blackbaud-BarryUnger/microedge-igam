namespace MicroEdge.Igam.Providers.Logging
{
    /// <summary>
    /// Interface that any log provider used by the log manager must implement.
    /// </summary>
    /// 
    /// <remarks>
    /// Author:  LDF
    /// Created: 9/25/2008
    /// </remarks>
    public interface ILogProvider
    {
        void LogDebug(string message, params object[] parameters);
        void LogInfo(string message);
        void LogWarning(string warning);
        void LogError(string error, params object[] parameters);
    }
}
