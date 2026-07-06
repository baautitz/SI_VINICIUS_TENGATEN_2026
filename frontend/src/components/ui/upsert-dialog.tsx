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
  onPointerDownOutside?: React.ComponentProps<typeof DialogContent>["onPointerDownOutside"];
  onEscapeKeyDown?: React.ComponentProps<typeof DialogContent>["onEscapeKeyDown"];
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
  onPointerDownOutside,
  onEscapeKeyDown,
  disableHotkey = false,
  isEdit = false,
}: UpsertDialogProps) {
  const [confirmCloseOpen, setConfirmCloseOpen] = React.useState(false);
  const contentRef = React.useRef<HTMLDivElement>(null);

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen && isEdit) {
      setConfirmCloseOpen(true);
    } else {
      onOpenChange(newOpen);
    }
  };

  useHotkeys([
    {
      hotkey: "Alt+Enter",
      callback: (e) => {
        if (typeof document !== 'undefined' && contentRef.current) {
          const dialogs = document.querySelectorAll('[role="dialog"]');
          const topDialog = dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;
          const myDialog = contentRef.current.closest('[role="dialog"]') || contentRef.current;
          if (dialogs.length > 0 && myDialog !== topDialog) return;
        }

        e.preventDefault();
        e.stopPropagation();
        if (contentRef.current) {
          const submitBtn = contentRef.current.querySelector(
            'button[type="submit"]'
          ) as HTMLButtonElement | null;
          if (submitBtn && !submitBtn.disabled) {
            submitBtn.click();
          } else {
            const formEl = contentRef.current.querySelector("form");
            if (formEl) {
              formEl.requestSubmit();
            }
          }
        }
      },
      options: {
        enabled: open && !loading && !disableHotkey && !confirmCloseOpen,
        ignoreInputs: false,
      },
    },
    {
      hotkey: "Alt+Enter",
      callback: (e) => {
        e.preventDefault();
        setConfirmCloseOpen(false);
        onOpenChange(false);
      },
      options: {
        enabled: confirmCloseOpen,
        ignoreInputs: false,
      },
    },
  ], { conflictBehavior: "allow" });

  return (
    <>
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent
          ref={contentRef}
          className="flex flex-col gap-0 p-0 overflow-hidden"
          style={{ width: "98vw", maxWidth: "98vw", height: "95vh" }}
          aria-describedby={undefined}
          onPointerDownOutside={(e) => {
            if (isEdit) {
              e.preventDefault();
              setConfirmCloseOpen(true);
            } else if (onPointerDownOutside) {
              onPointerDownOutside(e);
            }
          }}
          onEscapeKeyDown={(e) => {
            if (isEdit) {
              e.preventDefault();
              setConfirmCloseOpen(true);
            } else if (onEscapeKeyDown) {
              onEscapeKeyDown(e);
            }
          }}
        >
          <DialogHeader className="p-4 border-b bg-background shrink-0">
            <DialogTitle>{title}</DialogTitle>
          </DialogHeader>

          <div className="flex-1 overflow-y-auto p-4 flex flex-col justify-start relative">
            {loading ? (
              <div className="flex flex-col items-center justify-center h-full gap-4 py-20">
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
            <DialogFooter className="m-0 p-4 mt-auto shrink-0 border-t bg-muted/50">
              {footer}
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={confirmCloseOpen} onOpenChange={setConfirmCloseOpen}>
        <DialogContent className="max-w-md">
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
