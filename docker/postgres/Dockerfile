ARG PGVECTOR_IMAGE=pgvector/pgvector:0.8.6-pg16-bookworm
FROM ${PGVECTOR_IMAGE}

ARG PG_MAJOR=16

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        postgresql-${PG_MAJOR}-postgis-3 \
        postgresql-${PG_MAJOR}-postgis-3-scripts \
        postgresql-${PG_MAJOR}-pgrouting \
    && rm -rf /var/lib/apt/lists/*
