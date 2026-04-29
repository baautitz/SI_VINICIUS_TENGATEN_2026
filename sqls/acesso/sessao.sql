SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS sessao (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  usuario_id INTEGER NOT NULL,
  token VARCHAR(500) NOT NULL UNIQUE,
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_expiracao TIMESTAMP WITH TIME ZONE,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  
  CONSTRAINT fk_sessao_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

-- Índices para melhor performance
CREATE INDEX IF NOT EXISTS idx_sessao_usuario_id ON sessao(usuario_id);
CREATE INDEX IF NOT EXISTS idx_sessao_ativo ON sessao(ativo);
