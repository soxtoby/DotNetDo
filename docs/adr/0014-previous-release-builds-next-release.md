# Previous release builds the next release

DotNetDo's release scripts reference a pinned published `DotNetDo.Core` package and run through its paired `DotNetDo` tool rather than the source projects. This keeps the repository's automation representative of consumer usage and prevents automation from building its own dependency implicitly; consequently, release scripts may use only APIs available in the previous release, and `make-release` advances both pins to the current release while preparing the next one.
