SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS condicoes_pagamentos_parcelas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  condicao_pagamento_id INTEGER NOT NULL,
  numero_parcela INTEGER NOT NULL,
  percentual NUMERIC(7, 4) NOT NULL,
  prazo_dias INTEGER NOT NULL,
  CONSTRAINT condicoes_pagamentos_parcelas_condicao_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT condicoes_pagamentos_parcelas_unique
    UNIQUE (condicao_pagamento_id, numero_parcela),
  CONSTRAINT condicoes_pagamentos_parcelas_numero_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT condicoes_pagamentos_parcelas_percentual_ck
    CHECK (percentual > 0 AND percentual <= 100),
  CONSTRAINT condicoes_pagamentos_parcelas_prazo_ck
    CHECK (prazo_dias >= 0)
);
