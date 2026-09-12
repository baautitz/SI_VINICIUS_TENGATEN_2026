"use client";

import { useLayoutEffect, useMemo, useRef } from "react";
import type { ComponentType, ComponentProps, ReactNode } from "react";
import { AppWindow } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/primitives";
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/ui/primitives";
import { ActiveWindowProvider } from "./active-context";
import { useWindowRuntime, type WindowRecord } from "./provider";
import type { WindowCloseReason, WindowIcon } from "./types";
import {
  markKeyboardFocus,
  useNavigationScope,
} from "@/ui/keyboard-navigation";

const sizeClasses = {
  small: "max-w-md",
  medium: "max-w-2xl",
  large: "max-w-6xl",
  full: "h-[95dvh] max-h-[95dvh] w-[95dvw] max-w-[95dvw]",
} as const;

/** Renderiza cada janela como uma superfície modal independente. */
export function WindowManagerHost() {
  const runtime = useWindowRuntime();

  return (
    <>
      {runtime.layers.map((record) => (
        <ManagedWindow
          key={record.id}
          record={record}
          isTop={record.id === runtime.activeWindowId}
          runtime={runtime}
        />
      ))}
    </>
  );
}

function ManagedWindow({
  record,
  isTop,
  runtime,
}: {
  record: WindowRecord;
  isTop: boolean;
  runtime: ReturnType<typeof useWindowRuntime>;
}) {
  const scopeRef = useRef<HTMLDivElement | null>(null);
  const Component = record.options.component as ComponentType<
    Record<string, unknown>
  >;
  const componentProps = record.options.props as Record<string, unknown>;
  const title = record.options.title ?? "Janela";
  const description = record.options.description;
  const icon = record.options.icon;
  const reasonForClose: Extract<
    WindowCloseReason,
    "cancel" | "escape" | "outside"
  > = "cancel";
  const activeContext = useMemo(
    () => ({
      id: record.id,
      get isActive() {
        return runtime.controller.snapshot().activeId === record.id;
      },
      scopeRef,
      dismiss: (reason: WindowCloseReason = reasonForClose) => {
        runtime.controller.dismiss(record.id, reason);
      },
      resolve: (value: unknown) => runtime.controller.resolve(record.id, value),
      setDirty: (dirty: boolean) =>
        runtime.controller.markDirty(record.id, dirty),
    }),
    [record.id, runtime.controller],
  );
  useNavigationScope(scopeRef, {
    id: `window-${String(record.id)}`,
    active: isTop,
    priority: 100,
  });
  const body = (
    <div data-window-scope="true" className="contents">
      <Component {...componentProps} />
    </div>
  );
  const semanticClassName = cn(
    record.options.size ? sizeClasses[record.options.size] : undefined,
    record.options.chrome === "plain" ? "p-0" : undefined,
  );
  const titlebarClassName = cn(
    "shrink-0 border-b px-4 py-3",
    record.options.surface === "sheet" ||
      record.options.presentation === "drawer"
      ? undefined
      : record.options.chrome === "plain"
        ? undefined
        : "-mx-4 -mt-4",
  );
  const requestClose = (
    reason: Extract<WindowCloseReason, "cancel" | "escape" | "outside">,
  ) => runtime.controller.requestDismiss(record.id, reason);

  // Dialog auto-focus runs in an effect as well. Running this layout effect
  // first gives forms their first real field before Radix can fall back to the
  // close button. The body observer/retry also covers fields revealed after
  // async loading (for example, edit forms that start with a Spinner).
  useLayoutEffect(() => {
    if (!isTop) return;

    let disposed = false;
    let attempts = 0;
    let frame: number | null = null;

    const focusField = () => {
      if (disposed) return true;
      const container = scopeRef.current;
      if (!container) return false;

      const field = findFirstField(container);
      if (field) {
        markKeyboardFocus();
        field.focus({ preventScroll: true });
        return true;
      }

      // Confirmation/command windows may intentionally have no form. Their
      // first content action is preferable to the decorative close button.
      const hasForm = Boolean(container.querySelector("form"));
      if (!hasForm && attempts > 1) {
        const action = findFirstAction(container);
        if (action) {
          action.focus({ preventScroll: true });
          return true;
        }
      }

      // If a form is present but all fields are temporarily disabled, give it
      // a few frames to settle before falling back to an action.
      if (attempts >= 60) {
        const action = findFirstAction(container);
        if (action) {
          action.focus({ preventScroll: true });
          return true;
        }
      }

      return false;
    };

    const retryFocus = () => {
      if (disposed) return;
      attempts += 1;
      if (focusField()) {
        observer.disconnect();
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
        return;
      }
      if (attempts < 60) frame = requestAnimationFrame(retryFocus);
    };

    const observer = new MutationObserver(() => {
      if (focusField()) {
        observer.disconnect();
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
      } else if (frame === null && attempts < 60) {
        frame = requestAnimationFrame(retryFocus);
      }
    });

    // Observe body instead of the dialog ref alone: Radix portals and async
    // upsert shells can attach the actual content after this layout effect.
    observer.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ["aria-hidden", "disabled"],
    });
    retryFocus();

    return () => {
      disposed = true;
      observer.disconnect();
      if (frame !== null) cancelAnimationFrame(frame);
    };
  }, [isTop, record.id]);

  if (record.options.surface === "confirmation") {
    return (
      <AlertDialog
        open
        onOpenChange={(open) => {
          if (!open) requestClose("cancel");
        }}
      >
        <AlertDialogContent
          ref={scopeRef}
          aria-hidden={!isTop ? true : undefined}
          inert={!isTop ? true : undefined}
          onEscapeKeyDown={(event) => {
            event.preventDefault();
            requestClose("escape");
          }}
          onOpenAutoFocus={(event) => focusFirstField(event, isTop, scopeRef)}
        >
          <AlertDialogHeader>
            <AlertDialogTitle>{title}</AlertDialogTitle>
            {description ? (
              <AlertDialogDescription asChild>
                <div>{description}</div>
              </AlertDialogDescription>
            ) : (
              <AlertDialogDescription className="sr-only">
                Confirme ou cancele esta ação.
              </AlertDialogDescription>
            )}
          </AlertDialogHeader>
          <AlertDialogBody>
            <ActiveWindowProvider value={activeContext}>
              {body}
            </ActiveWindowProvider>
          </AlertDialogBody>
        </AlertDialogContent>
      </AlertDialog>
    );
  }

  if (
    record.options.presentation === "drawer" ||
    record.options.surface === "sheet"
  ) {
    return (
      <Sheet
        open
        onOpenChange={(open) => {
          if (!open) requestClose("cancel");
        }}
      >
        <SheetContent
          ref={scopeRef}
          side={record.options.side ?? record.options.drawerSide ?? "right"}
          aria-hidden={!isTop ? true : undefined}
          inert={!isTop ? true : undefined}
          className={semanticClassName}
          onOpenAutoFocus={(event) => focusFirstField(event, isTop, scopeRef)}
          onEscapeKeyDown={(event) => {
            event.preventDefault();
            requestClose("escape");
          }}
          onInteractOutside={(event) => {
            if (record.options.closeOnOutside === false) {
              event.preventDefault();
              return;
            }
            event.preventDefault();
            requestClose("outside");
          }}
        >
          <SheetHeader
            className={titlebarClassName}
            data-window-titlebar="true"
          >
            <WindowTitleContent
              icon={icon}
              title={title}
              description={description}
              Title={SheetTitle}
              Description={SheetDescription}
            />
          </SheetHeader>
          <SheetBody>
            <ActiveWindowProvider value={activeContext}>
              {body}
            </ActiveWindowProvider>
          </SheetBody>
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) requestClose("cancel");
      }}
    >
      <DialogContent
        ref={scopeRef}
        aria-hidden={!isTop ? true : undefined}
        inert={!isTop ? true : undefined}
        className={semanticClassName}
        onOpenAutoFocus={(event) => focusFirstField(event, isTop, scopeRef)}
        onEscapeKeyDown={(event) => {
          event.preventDefault();
          requestClose("escape");
        }}
        onInteractOutside={(event) => {
          if (record.options.closeOnOutside === false) {
            event.preventDefault();
            return;
          }
          event.preventDefault();
          requestClose("outside");
        }}
      >
        <DialogHeader className={titlebarClassName} data-window-titlebar="true">
          <WindowTitleContent
            icon={icon}
            title={title}
            description={description}
            Title={DialogTitle}
            Description={DialogDescription}
          />
        </DialogHeader>
        <DialogBody>
          <ActiveWindowProvider value={activeContext}>
            {body}
          </ActiveWindowProvider>
        </DialogBody>
      </DialogContent>
    </Dialog>
  );
}

/**
 * O primeiro campo da janela é o ponto de entrada do teclado. O Radix continua
 * sendo o fallback para janelas sem campos (por exemplo, uma janela só com
 * ações), mas nunca escolhemos Cancelar/Salvar antes de um input real.
 */
function focusFirstField(
  event: Event,
  isTop: boolean,
  scopeRef: React.RefObject<HTMLElement | null>,
): void {
  if (!isTop) {
    event.preventDefault();
    return;
  }

  const container =
    scopeRef.current ??
    (event.currentTarget instanceof HTMLElement ? event.currentTarget : null);
  const field = findFirstField(container);

  // Never let Radix choose the decorative close button while an upsert is
  // loading. The layout observer above will focus the field when it mounts.
  event.preventDefault();
  if (field) {
    markKeyboardFocus();
    field.focus({ preventScroll: true });
    return;
  }

  const hasForm = Boolean(container?.querySelector("form"));
  if (!hasForm && container) {
    findFirstAction(container)?.focus({ preventScroll: true });
  }
}

function findFirstField(
  container: HTMLElement | null,
): HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement | null {
  if (!container) return null;

  return (
    Array.from(
      container.querySelectorAll<
        HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
      >("input, textarea, select"),
    ).find((candidate) => {
      if (
        candidate.disabled ||
        candidate.getAttribute("aria-hidden") === "true" ||
        candidate.closest("[aria-hidden='true'], [inert]") ||
        !isVisible(candidate)
      ) {
        return false;
      }
      if (candidate instanceof HTMLInputElement) {
        return !["hidden", "button", "submit", "reset", "image"].includes(
          candidate.type,
        );
      }
      return true;
    }) ?? null
  );
}

function findFirstAction(container: HTMLElement): HTMLElement | null {
  return (
    Array.from(
      container.querySelectorAll<HTMLElement>(
        "[data-window-actions] button, [data-window-actions] a[href], button, a[href], [tabindex]",
      ),
    ).find((candidate) => {
      if (
        candidate.getAttribute("data-slot") === "dialog-close" ||
        candidate.getAttribute("data-slot") === "sheet-close" ||
        candidate.getAttribute("data-slot") === "alert-dialog-cancel" ||
        candidate.hasAttribute("disabled") ||
        candidate.getAttribute("aria-disabled") === "true" ||
        candidate.tabIndex < 0 ||
        candidate.closest("[aria-hidden='true'], [inert]")
      ) {
        return false;
      }
      return isVisible(candidate);
    }) ?? null
  );
}

function isVisible(element: HTMLElement): boolean {
  const style = window.getComputedStyle(element);
  if (style.display === "none" || style.visibility === "hidden") return false;
  const rect = element.getBoundingClientRect();
  return rect.width > 0 && rect.height > 0;
}

function WindowTitleContent({
  icon,
  title,
  description,
  Title,
  Description,
}: {
  icon?: WindowIcon;
  title: ReactNode;
  description?: ReactNode;
  Title: ComponentType<ComponentProps<typeof DialogTitle>>;
  Description: ComponentType<ComponentProps<typeof DialogDescription>>;
}) {
  const Icon = typeof icon === "function" ? icon : null;
  const iconNode: ReactNode = Icon ? <Icon /> : (icon as ReactNode);
  return (
    <div className="flex min-w-0 items-center gap-3">
      <div
        aria-hidden="true"
        className="bg-primary/10 text-primary flex size-10 shrink-0 items-center justify-center rounded-lg [&_svg]:size-5"
      >
        {iconNode ?? <AppWindow />}
      </div>
      <div className="flex min-w-0 flex-col gap-1 pr-8">
        <Title className="truncate text-lg font-semibold">{title}</Title>
        {description ? (
          <Description asChild>
            <div className="text-muted-foreground text-sm">{description}</div>
          </Description>
        ) : (
          <Description className="sr-only">Conteúdo da janela.</Description>
        )}
      </div>
    </div>
  );
}
