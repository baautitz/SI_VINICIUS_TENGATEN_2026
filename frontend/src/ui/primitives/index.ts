/**
 * Contrato visual estável do produto.
 *
 * As features dependem deste contrato, nunca da implementação concreta.
 * O adaptador Shadcn/Radix é a implementação inicial; um adaptador Base UI
 * poderá substituí-lo sem alterar os consumidores.
 */
export * from "@/ui/adapters/shadcn"
