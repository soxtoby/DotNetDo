# DotNetDo Package Is Tool And Library

DotNetDo v1 ships one NuGet package that provides both the `do` global tool command and the `DotNetDo` library namespace used by generated file-based apps. Keeping one package makes the generated template obvious and keeps versioning simple; the package can split later if NuGet tool packaging makes the combined shape impractical.

