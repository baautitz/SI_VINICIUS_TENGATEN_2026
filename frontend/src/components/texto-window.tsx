"use client";

import React from "react";
import { WindowActions } from "@/imperative-ui";
import { Button, Kbd, Field, FieldLabel, Textarea } from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";

export interface TextoWindowProps {
  label: string;
  value: string;
  readOnly?: boolean;
}

/** Modal de texto livre (observação, motivo): leitura, ou edição que resolve com o novo texto. */
export function TextoWindow({ label, value, readOnly = true }: TextoWindowProps) {
  const activeWindow = useWindow<string>();
  const ref = React.useRef<HTMLTextAreaElement | null>(null);
  const confirm = React.useCallback(
    () => activeWindow.resolve(ref.current?.value ?? ""),
    [activeWindow],
  );
  useWindowCommands(
    React.useMemo(
      () =>
        readOnly
          ? []
          : [
              {
                id: "texto.confirm",
                hotkey: "Alt+Enter" as const,
                label: "Confirmar",
                run: (event: KeyboardEvent) => {
                  event.preventDefault();
                  confirm();
                },
              },
            ],
      [readOnly, confirm],
    ),
  );

  return (
    <div className="flex flex-col gap-4">
      <Field>
        <FieldLabel htmlFor="texto-window">{label}</FieldLabel>
        <Textarea
          id="texto-window"
          ref={ref}
          defaultValue={value}
          disabled={readOnly}
          rows={14}
          maxLength={500}
        />
      </Field>
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (
          <Button type="button" onClick={confirm}>
            Confirmar <Kbd>Alt+Enter</Kbd>
          </Button>
        )}
      </WindowActions>
    </div>
  );
}
