import assert from "node:assert/strict";
import {
  MASCARAS,
  aplicarMascara,
  limparMascara,
  editarMascara,
  encaixaNaMascara,
  formatarDocumento,
  formatarTelefone,
} from "../../src/lib/mascara.ts";
import { cpfValido, cnpjValido, telefoneValido } from "../../src/lib/documento.ts";

// Formatacao: o separador so aparece quando ha caractere depois dele.
assert.equal(aplicarMascara("", MASCARAS.cpf), "");
assert.equal(aplicarMascara("123", MASCARAS.cpf), "123");
assert.equal(aplicarMascara("1234", MASCARAS.cpf), "123.4");
assert.equal(aplicarMascara("52998224725", MASCARAS.cpf), "529.982.247-25");
assert.equal(aplicarMascara("529.982.247-25", MASCARAS.cpf), "529.982.247-25");
assert.equal(aplicarMascara("11222333000181", MASCARAS.cnpj), "11.222.333/0001-81");
assert.equal(aplicarMascara("12abc34501de35", MASCARAS.cnpj), "12.ABC.345/01DE-35");
assert.equal(aplicarMascara("1", MASCARAS.telefone), "(1");
assert.equal(aplicarMascara("4533221100", MASCARAS.telefone), "(45) 3322-1100");
assert.equal(aplicarMascara("45999887766", MASCARAS.telefone), "(45) 99988-7766");
assert.equal(aplicarMascara("85851000", MASCARAS.cep), "85851-000");
assert.equal(aplicarMascara("13112026", MASCARAS.data), "13/11/2026");
assert.equal(aplicarMascara("abc-1d23", MASCARAS.placa), "ABC1D23");
assert.equal(aplicarMascara(null, MASCARAS.cpf), "");
assert.equal(aplicarMascara("789-1000315507x9", MASCARAS.ean), "78910003155079");

// Limpeza: descarta o que nao cabe e corta o excesso (colar valor maior).
assert.equal(limparMascara("529.982.247-25", MASCARAS.cpf), "52998224725");
assert.equal(limparMascara("cpf: 529.982.247-25 999", MASCARAS.cpf), "52998224725");
assert.equal(limparMascara("(45) 99988-7766", MASCARAS.telefone), "45999887766");

const editar = (texto, cursor, mascara, anterior, tipo) => {
  const r = editarMascara(texto, cursor, mascara, anterior, tipo);
  return [r.limpo, r.formatado, r.cursor];
};

// Digitar no fim: o cursor acompanha, inclusive ao cruzar um separador.
assert.deepEqual(editar("1234", 4, MASCARAS.cpf, "123", "insertText"), ["1234", "123.4", 5]);
assert.deepEqual(editar("(45", 3, MASCARAS.telefone, "4", "insertText"), ["45", "(45", 3]);

// Digitar no meio: "529.9|82.247-25" + "0" -> o cursor fica logo depois do digito novo.
assert.deepEqual(
  editar("529.9082.247-2", 6, MASCARAS.cpf, "5299822472", "insertText"),
  ["52990822472", "529.908.224-72", 6],
);

// Digitar antes de um separador: "529|.982" + "0" -> cursor depois do 0, que passou do ponto.
assert.deepEqual(editar("5290.982", 4, MASCARAS.cpf, "529982", "insertText"), ["5290982", "529.098.2", 5]);

// Backspace no meio: "529.98|2.247-25" apaga o 8 e o cursor fica no lugar.
assert.deepEqual(
  editar("529.92.247-25", 5, MASCARAS.cpf, "52998224725", "deleteContentBackward"),
  ["5299224725", "529.922.472-5", 5],
);

// Backspace logo depois de um separador: apaga o digito anterior ao separador.
assert.deepEqual(
  editar("529982.247-25", 3, MASCARAS.cpf, "52998224725", "deleteContentBackward"),
  ["5298224725", "529.822.472-5", 2],
);

// Delete logo antes de um separador: apaga o digito seguinte ao separador.
assert.deepEqual(
  editar("529982.247-25", 3, MASCARAS.cpf, "52998224725", "deleteContentForward"),
  ["5298224725", "529.822.472-5", 3],
);

// Apagar tudo e apagar uma selecao no meio.
assert.deepEqual(editar("", 0, MASCARAS.cpf, "52998224725", "deleteContentBackward"), ["", "", 0]);
assert.deepEqual(editar("529-25", 3, MASCARAS.cpf, "52998224725", "deleteContentBackward"), ["52925", "529.25", 3]);

// Colar por cima de tudo, com mascara e com lixo.
assert.deepEqual(
  editar("529.982.247-25", 14, MASCARAS.cpf, "", "insertFromPaste"),
  ["52998224725", "529.982.247-25", 14],
);
assert.deepEqual(
  editar("11.222.333/0001-81 ", 19, MASCARAS.cnpj, "123", "insertFromPaste"),
  ["11222333000181", "11.222.333/0001-81", 18],
);

// Colar no meio: "(45) |3322" + "99" -> cursor depois do trecho colado.
assert.deepEqual(editar("(45) 993322", 7, MASCARAS.telefone, "453322", "insertFromPaste"), ["45993322", "(45) 9933-22", 7]);

// Caractere invalido no meio nao muda o valor nem move o cursor.
assert.deepEqual(editar("529.x982", 5, MASCARAS.cpf, "529982", "insertText"), ["529982", "529.982", 3]);

// Telefone troca de fixo para celular ao digitar o 11o digito no meio.
assert.deepEqual(
  editar("(45) 93322-1100", 6, MASCARAS.telefone, "4533221100", "insertText"),
  ["45933221100", "(45) 93322-1100", 6],
);

// Campo cheio: digitar no meio desloca e descarta o ultimo.
assert.deepEqual(
  editar("5129.982.247-25", 2, MASCARAS.cpf, "52998224725", "insertText"),
  ["51299822472", "512.998.224-72", 2],
);

// Mascara livre (telefone): vale enquanto o texto cabe; o que nao cabe fica como digitado.
assert.equal(encaixaNaMascara("(45) 99988-7766", MASCARAS.telefone), true);
assert.equal(encaixaNaMascara("459998877665", MASCARAS.telefone), false);
assert.equal(encaixaNaMascara("+55 45 99988-7766", MASCARAS.telefone), false);
assert.equal(aplicarMascara("+1 555 0100", MASCARAS.telefone, true), "+1 555 0100");
assert.equal(aplicarMascara("45999887766", MASCARAS.telefone, true), "(45) 99988-7766");
const livre = (texto, cursor, anterior, tipo) => {
  const r = editarMascara(texto, cursor, MASCARAS.telefone, anterior, tipo, true);
  return [r.limpo, r.formatado, r.cursor];
};
// 12o digito: sai da mascara e o texto fica como esta na tela.
assert.deepEqual(livre("(45) 99988-77665", 16, "45999887766", "insertText"), ["(45) 99988-77665", "(45) 99988-77665", 16]);
// "+" no inicio: texto livre, cursor onde o navegador deixou.
assert.deepEqual(livre("+(45) 99988-7766", 1, "45999887766", "insertText"), ["+(45) 99988-7766", "+(45) 99988-7766", 1]);
// Apagar o "+" volta para a mascara.
assert.deepEqual(livre("45 3322-1100", 0, "+45 3322-1100", "deleteContentBackward"), ["4533221100", "(45) 3322-1100", 0]);
// Enquanto cabe, edita no meio como qualquer mascara.
assert.deepEqual(livre("(45) 93322-1100", 6, "4533221100", "insertText"), ["45933221100", "(45) 93322-1100", 6]);

// Exibicao.
assert.equal(formatarDocumento("52998224725"), "529.982.247-25");
assert.equal(formatarDocumento("11222333000181"), "11.222.333/0001-81");
assert.equal(formatarDocumento("AB-123/X"), "AB-123/X");
assert.equal(formatarDocumento(null), "");
assert.equal(formatarTelefone("45999887766"), "(45) 99988-7766");
assert.equal(formatarTelefone("+1 555 0100"), "+1 555 0100");

// Digitos verificadores.
for (const ok of ["52998224725", "529.982.247-25", "11144477735"]) assert.equal(cpfValido(ok), true, ok);
for (const ruim of ["", "52998224724", "11111111111", "5299822472", "529982247250"]) assert.equal(cpfValido(ruim), false, ruim);
for (const ok of ["11222333000181", "11.222.333/0001-81", "12ABC34501DE35", "12.abc.345/01de-35"]) assert.equal(cnpjValido(ok), true, ok);
for (const ruim of ["", "11222333000180", "00000000000000", "1122233300018", "12ABC34501DE36", "12ABC34501DE3A"]) assert.equal(cnpjValido(ruim), false, ruim);
for (const ok of ["4533221100", "(45) 99988-7766"]) assert.equal(telefoneValido(ok), true, ok);
for (const ruim of ["", "453322110", "459998877665"]) assert.equal(telefoneValido(ruim), false, ruim);

console.log("ok");
