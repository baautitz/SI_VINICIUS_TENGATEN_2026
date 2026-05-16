SET search_path TO projeto_sistemas;


CREATE TABLE IF NOT EXISTS produtos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  produto VARCHAR(150) NOT NULL,
  descricao TEXT NOT NULL,
  categoria_id INTEGER NOT NULL,
  marca_id INTEGER NOT NULL,
  unidade_medida_id INTEGER NOT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  CONSTRAINT produtos_categoria_fk
    FOREIGN KEY (categoria_id)
    REFERENCES categorias (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT produtos_marca_fk
    FOREIGN KEY (marca_id)
    REFERENCES marcas (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT produtos_unidade_fk
    FOREIGN KEY (unidade_medida_id)
    REFERENCES unidades_medida (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);
