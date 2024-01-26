namespace Liquid
{
    /// <summary>
    /// Setup command called from command line arguments
    /// </summary>
    public interface IWorkBenchCommand
    {
        /// <summary>
        /// Executes the command
        /// </summary>
        /// <returns></returns>
        bool Execute();
    }
}
