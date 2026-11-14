BEGIN;

CREATE TABLE IF NOT EXISTS docentes_cohortes (
    docente_id uuid NOT NULL,
    cohorte_id uuid NOT NULL,
    asignado_en timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_docentes_cohortes PRIMARY KEY (docente_id, cohorte_id),
    CONSTRAINT fk_docentes_cohortes_docentes
        FOREIGN KEY (docente_id) REFERENCES docentes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_docentes_cohortes_cohortes
        FOREIGN KEY (cohorte_id) REFERENCES cohortes (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_docentes_cohortes_cohorte_id
    ON docentes_cohortes (cohorte_id);

COMMIT;
