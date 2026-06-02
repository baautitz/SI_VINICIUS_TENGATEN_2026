SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_pessoa AS ENUM ('FISICA', 'JURIDICA');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;
