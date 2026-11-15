"use client";

import * as React from "react";

import { cn } from "@/lib/utils";

function Table({
  className,
  pageScroll = false,
  fill = false,
  ...props
}: React.ComponentProps<"table"> & {
  /**
   * A tabela rola junto com a página/janela (não tem rolagem própria): o cabeçalho fica sticky
   * em relação à área rolável de fora. Sem isso a tabela é o próprio container de rolagem.
   */
  pageScroll?: boolean;
  /** Ocupa a altura do pai: o espaço que sobra fica entre as linhas e o rodapé (totalizador colado embaixo). */
  fill?: boolean;
}) {
  return (
    <div
      data-slot="table-container"
      className={cn(
        "relative flex w-full flex-col [container-type:inline-size]",
        fill && "flex-1",
        // O corpo da janela tem p-1 (4px): o sticky gruda dentro desse respiro e as linhas apareciam passando por cima do cabeçalho.
        pageScroll ? "overflow-visible [&_thead]:-top-1" : "min-h-0 flex-1 overflow-auto",
      )}
    >
      <table
        data-slot="table"
        className={cn("w-full caption-bottom border-separate border-spacing-0 text-sm", fill && "h-full", className)}
        {...props}
      />
    </div>
  );
}

function TableHeader({ className, ...props }: React.ComponentProps<"thead">) {
  return (
    <thead
      data-slot="table-header"
      className={cn("bg-muted sticky top-0 z-20 [&_th]:border-b", className)}
      {...props}
    />
  );
}

function TableBody({ className, ...props }: React.ComponentProps<"tbody">) {
  return (
    <tbody
      data-slot="table-body"
      className={cn("[&_tr:last-child>td]:border-0", className)}
      {...props}
    />
  );
}

function TableFooter({ className, ...props }: React.ComponentProps<"tfoot">) {
  return (
    <tfoot
      data-slot="table-footer"
      className={cn(
        "bg-muted/50 font-medium [&>tr>td]:border-t [&>tr:last-child>td]:border-b-0",
        className,
      )}
      {...props}
    />
  );
}

function TableRow({ className, ...props }: React.ComponentProps<"tr">) {
  return (
    <tr
      data-slot="table-row"
      className={cn(
        "transition-colors [&>td]:border-b hover:bg-muted/50 has-aria-expanded:bg-muted/50 data-[state=selected]:bg-muted",
        className,
      )}
      {...props}
    />
  );
}

function TableHead({ className, ...props }: React.ComponentProps<"th">) {
  return (
    <th
      data-slot="table-head"
      className={cn(
        "h-9 px-3 text-left align-middle text-xs font-semibold tracking-wide whitespace-nowrap text-muted-foreground uppercase has-[[role=checkbox]]:pr-0",
        className,
      )}
      {...props}
    />
  );
}

function TableCell({ className, ...props }: React.ComponentProps<"td">) {
  return (
    <td
      data-slot="table-cell"
      className={cn(
        "px-3 py-1.5 align-middle whitespace-nowrap has-[[role=checkbox]]:pr-0",
        className,
      )}
      {...props}
    />
  );
}

function TableCaption({
  className,
  ...props
}: React.ComponentProps<"caption">) {
  return (
    <caption
      data-slot="table-caption"
      className={cn("mt-4 text-sm text-muted-foreground", className)}
      {...props}
    />
  );
}

export {
  Table,
  TableHeader,
  TableBody,
  TableFooter,
  TableHead,
  TableRow,
  TableCell,
  TableCaption,
};
