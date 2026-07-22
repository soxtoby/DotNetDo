namespace DotNetDo;

public static partial class Tools
{
    /// <summary>The Azure CLI for managing Azure resources from a terminal.</summary>
    public static class Azure
    {
        internal const string ToolName = "azure";
        
        /// <summary>Makes the <c>az</c> command available.</summary>
        public static ToolInstall Install => new(ToolName, "az") { ScoopApp = "azure-cli" };
    }
}
