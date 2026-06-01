import { Loader2 } from "lucide-react";

interface OverlayLoadingProps {
  open: boolean;
  message?: string;
}

export function OverlayLoading({
  open,
  message = "Carregando...",
}: OverlayLoadingProps) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm">
      <div className="flex flex-col items-center gap-4 p-6 bg-card rounded-lg shadow-lg border">
        <Loader2 className="h-10 w-10 animate-spin text-primary" />
        <p className="text-sm font-medium text-muted-foreground">{message}</p>
      </div>
    </div>
  );
}
