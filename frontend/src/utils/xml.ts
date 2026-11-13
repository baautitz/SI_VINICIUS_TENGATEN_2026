/** Indenta um XML para leitura. Só para exibição: o documento guardado e o baixado ficam intactos. */
export function formatarXml(xml: string): string {
  const partes = xml.replace(/>\s+</g, "><").trim().split(/(?=<)|(?<=>)/);
  let nivel = 0;
  const linhas: string[] = [];
  let i = 0;
  while (i < partes.length) {
    const atual = partes[i];
    if (atual.startsWith("</")) {
      nivel = Math.max(nivel - 1, 0);
      linhas.push("  ".repeat(nivel) + atual);
    } else if (atual.startsWith("<") && !atual.startsWith("<?") && !atual.endsWith("/>") && !atual.startsWith("<!")) {
      const texto = partes[i + 1];
      const fecha = partes[i + 2];
      if (texto !== undefined && !texto.startsWith("<") && fecha?.startsWith("</")) {
        linhas.push("  ".repeat(nivel) + atual + texto + fecha);
        i += 3;
        continue;
      }
      linhas.push("  ".repeat(nivel) + atual);
      nivel++;
    } else {
      linhas.push("  ".repeat(nivel) + atual);
    }
    i++;
  }
  return linhas.join("\n");
}
