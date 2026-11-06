"use client";

import { navigationCell } from "@/ui/keyboard-navigation";
import React from "react";
import { useQuery } from "@tanstack/react-query";
import { NumberInput } from "@/ui/composites";
import {
  Button,
  Field,
  FieldError,
  FieldLabel,
  HotkeyKbd,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
} from "@/ui/primitives";
import {
  useWindow,
  useWindowCommands,
  WindowActions,
  WindowCancelButton,
} from "@/ui/imperative";
import { comprasApi } from "@/api/compras";
import { getFullSkuName } from "@/features/catalogo/skus/types";
import { combinacaoDe } from "@/lib/atalhos";
import { MAX_MOTIVO_ESTORNO, MIN_MOTIVO_ESTORNO } from "@/lib/motivo-estorno";
import {
  moeda,
  quantidadeDevolvivel,
  type Compra,
  type CriarDevolucaoCompraValues,
} from "./types";

export type TipoDevolucaoCompra = "total" | "parcial";

// Valor líquido unitário do item na compra (com o desconto), igual ao do backend.
const valorLiquidoUnitario = (item: Compra["itens"][number]) =>
  Number(item.valorTotal) / Number(item.quantidade);

export function DevolucaoCompraTipoWindow() {
  const activeWindow = useWindow<TipoDevolucaoCompra>();
  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "compras.devolucao.parcial",
          hotkey: combinacaoDe("devolucaoParcial"),
          label: "Devolução parcial de compra",
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            activeWindow.resolve("parcial");
          },
        },
        {
          id: "compras.devolucao.total",
          hotkey: combinacaoDe("devolucaoTotal"),
          label: "Devolução total de compra",
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            activeWindow.resolve("total");
          },
        },
      ],
      [activeWindow],
    ),
  );
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm">Escolha o tipo de devolução ao fornecedor.</p>
      <WindowActions>
        <WindowCancelButton />
        <Button type="button" variant="outline" onClick={() => activeWindow.resolve("parcial")}>
          Devolução parcial
          <HotkeyKbd className="ml-2" hotkey={combinacaoDe("devolucaoParcial")} />
        </Button>
        <Button type="button" autoFocus onClick={() => activeWindow.resolve("total")}>
          Devolução total
          <HotkeyKbd className="ml-2" hotkey={combinacaoDe("devolucaoTotal")} />
        </Button>
      </WindowActions>
    </div>
  );
}

export interface DevolucaoCompraWindowProps {
  compraId: number;
  /** Total: quantidades já preenchidas com o saldo devolvível; parcial: zeradas. */
  total?: boolean;
}

export function DevolucaoCompraWindow({ compraId, total: devolucaoTotal }: DevolucaoCompraWindowProps) {
  const activeWindow = useWindow<CriarDevolucaoCompraValues>();
  const { data: compra, isLoading } = useQuery({
    queryKey: ["compras", "detail", compraId],
    queryFn: () => comprasApi.getById(compraId),
    gcTime: 0,
  });
  const [quantidades, setQuantidades] = React.useState<Record<number, number>>({});
  const [error, setError] = React.useState("");

  // Campo não controlado: digitar o motivo não re-renderiza a janela.
  const motivoRef = React.useRef<HTMLTextAreaElement | null>(null);
  const registerMotivo = React.useCallback(
    (element: HTMLTextAreaElement | null) => {
      motivoRef.current = element;
      if (!element) return;
      activeWindow.setDirtyCheck(() => element.value.trim().length > 0);
      return () => activeWindow.setDirtyCheck(null);
    },
    [activeWindow],
  );

  const itens = React.useMemo(() => compra?.itens ?? [], [compra]);
  const qtdDe = React.useCallback(
    (i: Compra["itens"][number]) =>
      quantidades[i.id] ?? (devolucaoTotal ? quantidadeDevolvivel(i) : 0),
    [quantidades, devolucaoTotal],
  );
  const total = itens.reduce((sum, i) => sum + qtdDe(i) * valorLiquidoUnitario(i), 0);

  const confirm = React.useCallback(() => {
    const motivo = (motivoRef.current?.value ?? "").trim();
    const selecionados = itens
      .filter((i) => qtdDe(i) > 0)
      .map((i) => ({ compraItemId: i.id, quantidade: qtdDe(i) }));

    if (selecionados.length === 0) return setError("Informe a quantidade de ao menos um item.");
    const acima = itens.find((i) => qtdDe(i) > quantidadeDevolvivel(i));
    if (acima)
      return setError(
        `${getFullSkuName(acima.sku) || acima.sku.sku}: quantidade acima do saldo devolvível (${quantidadeDevolvivel(acima)}). O que já foi vendido não pode ser devolvido.`,
      );
    if (motivo.length < MIN_MOTIVO_ESTORNO) return setError(`O motivo deve ter pelo menos ${MIN_MOTIVO_ESTORNO} caracteres.`);

    activeWindow.resolve({ motivo, itens: selecionados });
  }, [activeWindow, itens, qtdDe]);

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "compras.devolucao.confirm",
          hotkey: combinacaoDe("confirmar"),
          label: "Confirmar devolução",
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            confirm();
          },
        },
      ],
      [confirm],
    ),
  );

  if (isLoading)
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground text-xs">
        Os itens saem do estoque e o custo médio volta ao que era sem eles. Só dá para devolver
        o que ainda está em estoque: o que já foi vendido não volta ao fornecedor.
      </p>
      <div className="rounded-lg border bg-card overflow-clip">
        <Table pageScroll>
          <TableHeader>
            <TableRow>
              <TableHead>Item</TableHead>
              <TableHead className="text-right">Comprado</TableHead>
              <TableHead className="text-right">Devolvido</TableHead>
              <TableHead className="text-right">Em estoque</TableHead>
              <TableHead className="text-right">Saldo devolvível</TableHead>
              <TableHead className="w-32 text-right">Devolver</TableHead>
              <TableHead className="text-right">Valor</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {itens.map((item) => {
              const saldo = quantidadeDevolvivel(item);
              const decimais = !!item.sku.produto?.unidadeMedida?.permiteDecimais;
              const qtd = qtdDe(item);
              return (
                <TableRow key={item.id}>
                  <TableCell>{getFullSkuName(item.sku) || item.sku.sku}</TableCell>
                  <TableCell className="text-right">{Number(item.quantidade)}</TableCell>
                  <TableCell className="text-right">{Number(item.quantidadeDevolvida ?? 0)}</TableCell>
                  <TableCell className="text-right">{Number(item.sku.estoque)}</TableCell>
                  <TableCell className="text-right">{saldo}</TableCell>
                  <TableCell className="text-right">
                    <NumberInput
                      value={qtd}
                      decimals={decimais ? 4 : 0}
                      inputSize="full"
                      inputMode={decimais ? "decimal" : "numeric"}
                      readOnly={saldo <= 0}
                      {...navigationCell({ grid: "devolucao-compra-itens", row: itens.indexOf(item), column: 0 })}
                      aria-invalid={qtd > saldo}
                      onNumberChange={(v) => {
                        setError("");
                        setQuantidades((prev) => ({ ...prev, [item.id]: v ?? 0 }));
                      }}
                    />
                  </TableCell>
                  <TableCell className="text-right">{moeda(qtd * valorLiquidoUnitario(item))}</TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
      <Field>
        <FieldLabel htmlFor="total-devolver-compra">Total a devolver</FieldLabel>
        <NumberInput
          id="total-devolver-compra"
          value={total}
          decimals={2}
          inputSize="full"
          inputMode="decimal"
          readOnly
          className="text-right font-semibold"
        />
      </Field>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor="motivo-devolucao-compra">Motivo da Devolução de compra</FieldLabel>
        <Textarea
          id="motivo-devolucao-compra"
          ref={registerMotivo}
          onChange={() => error && setError("")}
          placeholder={`Informe o motivo (mínimo de ${MIN_MOTIVO_ESTORNO} caracteres)...`}
          rows={3}
          maxLength={MAX_MOTIVO_ESTORNO}
        />
        {error && <FieldError>{error}</FieldError>}
      </Field>
      <WindowActions>
        <WindowCancelButton />
        <Button type="button" onClick={confirm}>
          Confirmar Devolução
          <HotkeyKbd className="ml-2" hotkey={combinacaoDe("confirmar")} />
        </Button>
      </WindowActions>
    </div>
  );
}
