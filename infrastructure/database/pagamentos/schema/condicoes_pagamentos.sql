SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS condicoes_pagamentos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  descricao VARCHAR(255) NOT NULL UNIQUE,
  metodo_pagamento_id INTEGER NOT NULL,
  entrada_minima_percentual NUMERIC(7, 4) NOT NULL DEFAULT 0,
  desconto_percentual NUMERIC(7, 4) NOT NULL DEFAULT 0,
  acrescimo_percentual NUMERIC(7, 4) NOT NULL DEFAULT 0,
  multa_percentual NUMERIC(7, 4) NOT NULL DEFAULT 0,
  taxa_juros_percentual NUMERIC(7, 4) NOT NULL DEFAULT 0,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  CONSTRAINT condicoes_pagamentos_metodo_fk
    FOREIGN KEY (metodo_pagamento_id)
    REFERENCES metodos_pagamento (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT condicoes_pagamentos_entrada_ck
    CHECK (entrada_minima_percentual >= 0 AND entrada_minima_percentual <= 100),
  CONSTRAINT condicoes_pagamentos_desconto_ck
    CHECK (desconto_percentual >= 0 AND desconto_percentual <= 100),
  CONSTRAINT condicoes_pagamentos_acrescimo_ck
    CHECK (acrescimo_percentual >= 0 AND acrescimo_percentual <= 100),
  CONSTRAINT condicoes_pagamentos_multa_ck
    CHECK (multa_percentual >= 0 AND multa_percentual <= 100),
  CONSTRAINT condicoes_pagamentos_juros_ck
    CHECK (taxa_juros_percentual >= 0 AND taxa_juros_percentual <= 100)
);
