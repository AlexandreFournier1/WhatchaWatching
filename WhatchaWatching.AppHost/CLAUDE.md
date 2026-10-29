# Aspire AppHost (`WhatchaWatching.AppHost`)

[Program.cs](Program.cs) is the single place that declares resources (MySQL container + `DefaultConnection` database). Add new resources there and chain `.WithReference(...)` to the web app. The resource name must stay equal to `DatabaseConstants.CONNECTION_NAME`. The `Aspire.AppHost.Sdk` SDK needs no separate Aspire package reference; `Projects.WhatchaWatching` is source-generated.
