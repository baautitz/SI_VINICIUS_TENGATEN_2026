import React from "react";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Kbd, KbdGroup } from "@/components/ui/kbd";

interface FeatureHeaderProps {
  title: string;
  icon: React.ReactNode;
  onAdd: () => void;
  addButtonLabel: string;
}

export function FeatureHeader({
  title,
  icon,
  onAdd,
  addButtonLabel,
}: FeatureHeaderProps) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2">
        <div className="bg-primary/10 text-primary rounded-lg p-2">
          <div className="[&_svg]:size-5">{icon}</div>
        </div>
        <h1 className="text-foreground text-xl font-bold tracking-tight">
          {title}
        </h1>
      </div>

      <Button onClick={onAdd} className="gap-2 rounded-lg shadow-sm">
        <Plus className="h-4 w-4" />
        {addButtonLabel}{" "}
        <KbdGroup>
          <Kbd>Alt</Kbd>
          <Kbd>N</Kbd>
        </KbdGroup>
      </Button>
    </div>
  );
}
