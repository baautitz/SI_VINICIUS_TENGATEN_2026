SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS nfes_pagamentos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL,
  metodo_pagamento_id INTEGER NOT NULL,
  indicador_pagamento indicador_pagamento_enum NOT NULL,
  valor_pagamento NUMERIC(14, 2) NOT NULL,
  CONSTRAINT nfes_pagamentos_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_pagamentos_metodo_fk
    FOREIGN KEY (metodo_pagamento_id)
    REFERENCES metodos_pagamento (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_pagamentos_valor_ck
    CHECK (valor_pagamento >= 0)
);
