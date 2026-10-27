// SessionUtil is a process-wide singleton, so tests that log in as different users must not overlap.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
