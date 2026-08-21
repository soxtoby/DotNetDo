# ZIP creation stages complete archives

`AbsolutePath.ZipTo` creates a complete archive in a temporary file before moving it to its destination, so compression failures do not leave a partial archive or replace an existing destination. Extraction deliberately delegates entry validation, collision handling, and partial-output behavior to `ZipFile.ExtractToDirectory`; duplicating those rules would add substantial complexity while making DotNetDo responsible for tracking evolving .NET ZIP semantics.
