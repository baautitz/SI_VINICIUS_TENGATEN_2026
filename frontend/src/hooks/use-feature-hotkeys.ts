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
            const isHidden = !!listRef.current.closest('[aria-hidden="true"]');
            if (isHidden) return;

            const dialogs = document.querySelectorAll('[role="dialog"]');
            const topDialog =
              dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;
            const myDialog = listRef.current.closest('[role="dialog"]');
            if (dialogs.length > 0 && myDialog !== topDialog) return;
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
