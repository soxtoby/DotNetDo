# Azure CLI owns Bicep bootstrap

Azure CLI command families belong in DotNetDo only when no stable, supported Microsoft NuGet package provides the operation; CLI convenience alone is insufficient. `Tools.Azure.Bicep` therefore models `az bicep` because the public `Azure.Bicep.Core` package is unsupported. DotNetDo requires Azure CLI installation explicitly, but preserves Azure CLI's native behavior of installing or updating its nested Bicep binary when a Bicep command runs; `Tools.Azure.Bicep.Install` remains available when a task needs explicit version or platform control.
