# Generated tasks use a floating DotNetDo reference

Generated tasks reference the latest DotNetDo package instead of pinning the tool version that created them. DotNetDo v1 favors getting current helpers and fixes by default; version pinning can be added later when a task needs reproducibility.
