import React from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Spinner } from "./spinner";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { Button } from "@/components/ui/button";
import { Kbd, KbdGroup } from "@/components/ui/kbd";

interface UpsertDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children?: React.ReactNode;
  footer?: React.ReactNode;
  loading?: boolean;
  onPointerDownOutside?: React.ComponentProps<
    typeof DialogContent
  >["onPointerDownOutside"];
  onEscapeKeyDown?: React.ComponentProps<
    typeof DialogContent
  >["onEscapeKeyDown"];
  disableHotkey?: boolean;
  isEdit?: boolean;
}

export function UpsertDialog({
  open,
  onOpenChange,
  title,
  children,
  footer,
  loading = false,
  disableHotkey = false,
}: UpsertDialogProps) {
  const [confirmCloseOpen, setConfirmCloseOpen] = React.useState(false);

  const formDialogRef = React.useRef<HTMLDivElement>(null);
  const confirmDialogRef = React.useRef<HTMLDivElement>(null);

  const handleOpenChange = (newOpen: boolean) => {
    if (newOpen) {
      onOpenChange(true);
      return;
    }

    setConfirmCloseOpen(true);
  };

  useHotkeys(
    [
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          const activeRef = confirmCloseOpen ? confirmDialogRef : formDialogRef;

          if (typeof document !== "undefined" && activeRef.current) {
            const dialogs = document.querySelectorAll('[role="dialog"]');
            const topDialog =
              dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;

            const myDialog =
              activeRef.current.closest('[role="dialog"]') ?? activeRef.current;

            if (dialogs.length > 0 && myDialog !== topDialog) {
              return;
            }
          }

          e.preventDefault();
          e.stopPropagation();

          if (confirmCloseOpen) {
            setConfirmCloseOpen(false);
            onOpenChange(false);
            return;
          }

          if (formDialogRef.current) {
            const submitBtn = formDialogRef.current.querySelector(
              'button[type="submit"]',
            ) as HTMLButtonElement | null;

            if (submitBtn && !submitBtn.disabled) {
              submitBtn.click();
            } else {
              const formEl = formDialogRef.current.querySelector("form");

              if (formEl) {
                formEl.requestSubmit();
              }
            }
          }
        },
        options: {
          enabled: open && !loading && !disableHotkey,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  return (
    <>
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent
          ref={formDialogRef}
          className="flex flex-col gap-0 overflow-hidden p-0"
          style={{ width: "98vw", maxWidth: "98vw", height: "95vh" }}
          aria-describedby={undefined}
          onPointerDownOutside={(e) => {
            e.preventDefault();
            setConfirmCloseOpen(true);
          }}
          onEscapeKeyDown={(e) => {
            e.preventDefault();
            setConfirmCloseOpen(true);
          }}
        >
          <DialogHeader className="bg-background shrink-0 border-b p-4">
            <DialogTitle>{title}</DialogTitle>
          </DialogHeader>

          <div className="relative flex flex-1 flex-col justify-start overflow-y-auto p-4">
            {loading ? (
              <div className="flex h-full flex-col items-center justify-center gap-4 py-20">
                <Spinner className="size-6" />
                <p className="text-muted-foreground font-medium">
                  Carregando dados...
                </p>
              </div>
            ) : (
              children
            )}
          </div>

          {!loading && footer && (
            <DialogFooter className="bg-muted/50 m-0 mt-auto shrink-0 border-t p-4">
              {footer}
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <Dialog
        open={confirmCloseOpen}
        onOpenChange={(open) => {
          if (!open) {
            setConfirmCloseOpen(false);
          }
        }}
      >
        <DialogContent ref={confirmDialogRef} className="max-w-md">
          <DialogHeader>
            <DialogTitle>Sair do Formulário?</DialogTitle>
            <DialogDescription>
              Você possui alterações não salvas. Se fechar o formulário agora,
              todos os dados digitados serão perdidos.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setConfirmCloseOpen(false)}
            >
              Permanecer <Kbd>Esc</Kbd>
            </Button>

            <Button
              type="button"
              variant="destructive"
              onClick={() => {
                setConfirmCloseOpen(false);
                onOpenChange(false);
              }}
            >
              Sair e Descartar
              <KbdGroup className="ml-2">
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
