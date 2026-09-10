import * as React from "react";
import { useWindowCommands } from "@/ui/imperative";

interface UseFeatureHotkeysOptions {
  onAdd?: () => void;
  listRef: React.RefObject<HTMLElement | null>;
}

export function useFeatureHotkeys({
  onAdd,
  listRef,
}: UseFeatureHotkeysOptions) {
  // A list remains a root-scope command surface. The manager arbitrates
  // active windows, so background lists cannot steal Alt+N from a dialog.
  void listRef;
  useWindowCommands([
    {
      id: "feature.list.create",
      hotkey: "Alt+N",
      label: "Novo registro",
      run: (event) => {
        event.preventDefault();
        onAdd?.();
      },
    },
  ]);
}
