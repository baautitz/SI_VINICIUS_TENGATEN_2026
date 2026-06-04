import React, { useEffect, useRef } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

interface ListDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: React.ReactNode;
}

export function ListDialog({
  open,
  onOpenChange,
  title,
  children,
}: ListDialogProps) {
  const contentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open) {
      const timer = setTimeout(() => {
        const input = contentRef.current?.querySelector("input");
        if (input) {
          input.focus();
        }
      }, 50);
      return () => clearTimeout(timer);
    }
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        ref={contentRef}
        className="flex flex-col gap-0 p-0 overflow-hidden"
        style={{ width: "98vw", maxWidth: "98vw", height: "95vh" }}
      >
        <DialogHeader className="p-4 border-b bg-background shrink-0">
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto p-4 flex flex-col justify-start relative h-full">
          {children}
        </div>
      </DialogContent>
    </Dialog>
  );
}
