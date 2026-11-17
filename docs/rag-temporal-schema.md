# Prepare temporal document filtering safely

This preparation-only migration adds canonical temporal metadata to `documentofuente`. It does not apply the migration, classify documents, alter content/vectors, or delete historical records.

## Rollout gate

Keep Python `RAG_TEMPORAL_FILTER_ENABLED=false` until the migration is explicitly approved and applied by an operator, and documents have been explicitly classified. Existing documents default to `desconocido`, NOT `vigente`; enabling the current-only filter before classification excludes them and can return no context. Do not infer validity from ingestion date, title, or document year.

## SQL contract

| Column | PostgreSQL type | Null/default |
|---|---|---|
| `temporal_status` | `text` | NOT NULL, default `desconocido` |
| `document_year` | `integer` | nullable; 1000–9999 when known |
| `academic_period` | `text` | nullable; canonical period such as `2025-2` |
| `effective_from` | `timestamp with time zone` | nullable |
| `effective_to` | `timestamp with time zone` | nullable |
| `last_verified_at` | `timestamp with time zone` | nullable |
| `superseded_by` | `uuid` | nullable; references `documentofuente.id`, ON DELETE RESTRICT |

`temporal_status` is constrained to `vigente`, `reemplazado`, `historico`, or `desconocido`. Status, period, and replacement reference have B-tree indexes. The existing `rag_chunk.documento_fuente_id` index and cascade relationship are unchanged.

Retrieval joins `rag_chunk.documento_fuente_id = documentofuente.id`, filters document status/period BEFORE candidate ordering and reranking, and does not scan JSONB metadata. Current answers use `vigente`; historical answers may include `historico`/`reemplazado`; a specified period uses `academic_period`. Unknown status stays excluded from current answers unless a separately approved explicit fallback policy exists.

## Review and verification

The database-free `RagTemporalSchemaTests` inspect EF metadata, registered migration operations, and generated PostgreSQL SQL. They never open a connection. No database application or live query-plan validation has been performed by this change.

`Down` is prepared only: it removes the seven new columns, checks, indexes, and replacement FK; it leaves preexisting columns, documents, chunks, and vectors intact. Running it would discard newly recorded temporal metadata, so rollback requires separate operator approval and a backup. No automatic backfill or rollback is included.
