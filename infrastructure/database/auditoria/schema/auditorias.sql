SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS auditoria (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  tabela VARCHAR(64) NOT NULL,
  operacao VARCHAR(10) NOT NULL,
  usuario_id INTEGER NOT NULL,
  data_hora TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  dados_antigos JSONB,
  dados_novos JSONB,
  descricao TEXT,
  sessao_id BIGINT,
  
  CONSTRAINT fk_auditoria_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id),
  CONSTRAINT fk_auditoria_sessao FOREIGN KEY (sessao_id) REFERENCES sessoes(id),
  CONSTRAINT ck_auditoria_operacao CHECK (operacao IN ('INSERT', 'UPDATE', 'DELETE'))
);

-- Índices para melhor performance em consultas
CREATE INDEX IF NOT EXISTS idx_auditoria_tabela ON auditoria(tabela);
CREATE INDEX IF NOT EXISTS idx_auditoria_usuario_id ON auditoria(usuario_id);
CREATE INDEX IF NOT EXISTS idx_auditoria_sessao_id ON auditoria(sessao_id);
CREATE INDEX IF NOT EXISTS idx_auditoria_data_hora ON auditoria(data_hora DESC);
CREATE INDEX IF NOT EXISTS idx_auditoria_operacao ON auditoria(operacao);
CREATE INDEX IF NOT EXISTS idx_auditoria_tabela_data ON auditoria(tabela, data_hora DESC);
