# Repository verification compares Git-visible baselines

`GitRepository.VerifyUnchanged` compares Git tree snapshots before and after an operation instead of requiring a clean repository. Git writes the real index tree, stages the working tree into a temporary index, and writes that tree. Tree comparison therefore owns content normalization, modes, symbolic links, gitlinks, binary classification, and patch generation. A small supplemental snapshot retains parent-reported dirty submodule state.

This permits unrelated pre-existing changes while detecting further edits to already-dirty tracked and untracked files. Concurrent differences in the final snapshot also fail. Ignored files, index bookkeeping flags, non-Git metadata, refs, configuration, and recursive submodule contents remain outside the repository boundary. Snapshotting requires Git and a fully merged index; generated tree and blob objects remain unreferenced and Git may prune them normally.
