// Placa brasileira: padrao antigo (ABC1234) ou Mercosul (ABC1D23); hifen opcional.
const PADRAO_PLACA = /^[A-Z]{3}[0-9]([0-9]|[A-Z])[0-9]{2}$/

export function placaValida(placa: string): boolean {
  return PADRAO_PLACA.test(placa.trim().replace(/-/g, "").toUpperCase())
}
