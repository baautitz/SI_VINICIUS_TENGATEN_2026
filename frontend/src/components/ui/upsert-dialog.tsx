import React from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";

interface UpsertDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: React.ReactNode;
  footer: React.ReactNode;
}

export function UpsertDialog({
  open,
  onOpenChange,
  title,
  children,
  footer,
}: UpsertDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="flex flex-col gap-0 p-0 overflow-hidden"
        style={{ width: "98vw", maxWidth: "98vw", height: "95vh" }}
      >
        <DialogHeader className="p-4 border-b bg-background shrink-0">
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto p-4 flex flex-col justify-start relative">
          {children}
        </div>

        <DialogFooter className="m-0 p-4 mt-auto shrink-0 border-t bg-muted/50">
          {footer}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
