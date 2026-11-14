-- Crea de forma idempotente la relación de acceso compartido; no cambia el docente propietario
-- ni las matrículas, escenarios o partidas existentes.
CREATE TABLE IF NOT EXISTS cohortes_docentes
(
    docente_id uuid NOT NULL REFERENCES docentes (id) ON DELETE CASCADE,
    cohorte_id uuid NOT NULL REFERENCES cohortes (id) ON DELETE CASCADE,
    asignada_en timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_cohortes_docentes PRIMARY KEY (docente_id, cohorte_id)
);

CREATE INDEX IF NOT EXISTS ix_cohortes_docentes_cohorte_id
    ON cohortes_docentes (cohorte_id);
