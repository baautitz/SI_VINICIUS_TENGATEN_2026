"use client";

import * as React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { useHotkeys } from "@tanstack/react-hotkeys";

import { Kbd, KbdGroup } from "@/components/ui/kbd";

interface DeleteDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  title: string;
  description: React.ReactNode;
  loading?: boolean;
}

export function DeleteDialog({
  open,
  onOpenChange,
  onConfirm,
  title,
  description,
  loading,
}: DeleteDialogProps) {
  const contentRef = React.useRef<HTMLDivElement>(null);

  useHotkeys(
    [
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          if (typeof document !== "undefined" && contentRef.current) {
            const dialogs = document.querySelectorAll('[role="dialog"]');
            const topDialog =
              dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;
            const myDialog =
              contentRef.current.closest('[role="dialog"]') ||
              contentRef.current;
            if (topDialog && myDialog !== topDialog) return;
          }
          e.preventDefault();
          e.stopPropagation();
          onConfirm();
        },
        options: {
          enabled: open && !loading,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent ref={contentRef} className="sm:max-w-106.25">
        <DialogHeader>
          <div className="text-destructive mb-2 flex items-center gap-2">
            <DialogTitle>{title}</DialogTitle>
          </div>
          <DialogDescription asChild>
            <div className="text-foreground/80 py-2">{description}</div>
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="mt-4 gap-2">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            Cancelar <Kbd>Esc</Kbd>
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={loading}>
            {loading ? (
              "Excluindo..."
            ) : (
              <span className="flex items-center gap-2">
                Confirmar Exclusão
                <KbdGroup>
                  <Kbd>Alt</Kbd>
                  <Kbd>Enter</Kbd>
                </KbdGroup>
              </span>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
