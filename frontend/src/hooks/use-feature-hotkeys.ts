import * as React from "react";

import { useHotkeys } from "@tanstack/react-hotkeys";

interface UseFeatureHotkeysOptions {
  onAdd?: () => void;
  listRef: React.RefObject<HTMLElement | null>;
}

export function useFeatureHotkeys({
  onAdd,
  listRef,
}: UseFeatureHotkeysOptions) {
  useHotkeys(
    [
      {
        hotkey: "Alt+N",
        callback: (e: KeyboardEvent) => {
          if (typeof document !== "undefined" && listRef.current) {
            const anyDialogOpen = document.querySelectorAll('[role="dialog"]').length > 0;
            const myDialog = listRef.current.closest('[role="dialog"]');
            if (anyDialogOpen && !myDialog) return;
          }
          e.preventDefault();
          onAdd?.();
        },
        options: {
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "replace" },
  );
}
