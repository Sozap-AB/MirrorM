MirrorM is an ORM for .NET project. Currently works with PostgreSQL database only.

# Key features #

  * Explicit workflow: no magic, no proxies, no runtime code generation. You can put breakpoint when you create a model, when MirrorM loads model from database, when fields changing, all is explicit.
  * Single memory instance per database row: MirrorM enforces that only one instance of a model exists in memory for each database row. No more duplicates, no more data inconsistency.
  * Simple generated SQL: MirrorM generates readable SQL queries, which you might expect writing yourself, doing the same job.
  * Flexible SQL generation: If you still don't like generated SQL, or want to use another database client library you can write your own adapter project, or rewrite existing one, working with a database client is completely localized in a separate project.

# Requirements #

TBD;

# Getting started #

TBD;

# Examples #

TBD;