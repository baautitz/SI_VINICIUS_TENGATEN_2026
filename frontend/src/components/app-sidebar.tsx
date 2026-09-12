"use client";

import * as React from "react";
import Link from "next/link";
import { useNavigationPathname } from "@/ui/adapters/navigation-next";
import { ChevronRight, Search } from "lucide-react";

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
  SidebarHeader,
  SidebarTrigger,
  useSidebar,
  SidebarFooter,
} from "@/ui/primitives";

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { InputGroup, InputGroupInput, InputGroupAddon } from "@/ui/primitives";
import { navigationConfig as groups, homeItem } from "@/config/navigation";
import { useWindowCommands, useUi } from "@/ui/imperative";

class LocalStorageStore {
  private subscribers = new Set<() => void>();

  subscribe = (callback: () => void) => {
    this.subscribers.add(callback);
    return () => this.subscribers.delete(callback);
  };

  get = (key: string): string | null => {
    if (typeof window === "undefined") return null;
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  };

  set = (key: string, value: string) => {
    try {
      localStorage.setItem(key, value);
      this.subscribers.forEach((cb) => cb());
    } catch {
      // Preferência de navegação é opcional; a interface permanece utilizável.
    }
  };
}

const sidebarStore = new LocalStorageStore();

function useSidebarSectionOpen(key: string, defaultOpen: boolean): boolean {
  const getSnapshot = React.useCallback(() => {
    const val = sidebarStore.get(`sidebar:${key}`);
    if (val !== null) return val === "true";
    return defaultOpen;
  }, [key, defaultOpen]);

  const getServerSnapshot = React.useCallback(() => defaultOpen, [defaultOpen]);

  return React.useSyncExternalStore(
    sidebarStore.subscribe,
    getSnapshot,
    getServerSnapshot,
  );
}

const normalizeStr = (str: string) =>
  str
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();

interface SidebarGroupSectionProps {
  group: (typeof groups)[number];
  pathname: string;
  searchTerm: string;
}

function SidebarGroupSection({
  group,
  pathname,
  searchTerm,
}: SidebarGroupSectionProps) {
  const defaultOpen = pathname.startsWith(group.urlPrefix);

  const hasMatchedItems = React.useMemo(() => {
    if (!searchTerm.trim()) return false;
    const term = normalizeStr(searchTerm);
    return group.items.some((item) => normalizeStr(item.title).includes(term));
  }, [group.items, searchTerm]);

  const localIsOpen = useSidebarSectionOpen(group.id, defaultOpen);
  const isOpen = hasMatchedItems || localIsOpen;
  const hasActiveItem = group.items.some((item) => pathname === item.url);

  const GroupIcon = group.icon;
  const { state, setOpen } = useSidebar();
  const ui = useUi();

  return (
    <SidebarGroup>
      <SidebarGroupContent>
        <SidebarMenu>
          <Collapsible
            asChild
            open={isOpen}
            onOpenChange={(open) => {
              if (searchTerm.trim()) {
                return;
              }
              if (state === "collapsed") {
                sidebarStore.set(`sidebar:${group.id}`, "true");
                setOpen(true);
              } else {
                sidebarStore.set(`sidebar:${group.id}`, String(open));
              }
            }}
            className="group/collapsible"
          >
            <SidebarMenuItem>
              <CollapsibleTrigger asChild>
                <SidebarMenuButton
                  tooltip={group.title}
                  isActive={pathname.startsWith(group.urlPrefix)}
                  data-navigation-sidebar-current={
                    pathname.startsWith(group.urlPrefix) &&
                    (!hasActiveItem || state === "collapsed")
                      ? "true"
                      : undefined
                  }
                >
                  <GroupIcon />
                  <span>{group.title}</span>
                  <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                </SidebarMenuButton>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub>
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const active = pathname === item.url;
                    return (
                      <SidebarMenuSubItem key={item.title}>
                        <SidebarMenuSubButton
                          asChild
                          isActive={active}
                          data-navigation-sidebar-subitem="true"
                          data-navigation-sidebar-current={
                            active ? "true" : undefined
                          }
                        >
                          <Link href={item.url} onClick={(event) => {
                            event.preventDefault();
                            ui.navigation.go(item.url);
                          }}>
                            <Icon />
                            <span>{item.title}</span>
                          </Link>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    );
                  })}
                </SidebarMenuSub>
              </CollapsibleContent>
            </SidebarMenuItem>
          </Collapsible>
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  );
}

export function AppSidebar() {
  const pathname = useNavigationPathname();
  const ui = useUi();
  const [searchTerm, setSearchTerm] = React.useState("");
  const searchInputRef = React.useRef<HTMLInputElement>(null);
  const { state, setOpen } = useSidebar();

  const commands = React.useMemo(
    () => [
      {
        id: "sidebar.search",
        hotkey: "Alt+S" as const,
        label: "Buscar no menu",
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          if (state === "collapsed") setOpen(true);
          window.setTimeout(() => {
            searchInputRef.current?.focus();
            searchInputRef.current?.select();
          }, 50);
        },
      },
      {
        id: "sidebar.home",
        hotkey: "Alt+H" as const,
        label: "Ir para início",
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          ui.navigation.go(homeItem.url);
        },
      },
    ],
    [setOpen, state, ui],
  );
  useWindowCommands(commands);

  const prevPathnameRef = React.useRef(pathname);

  React.useEffect(() => {
    const prev = prevPathnameRef.current;
    prevPathnameRef.current = pathname;

    groups.forEach((g) => {
      if (pathname.startsWith(g.urlPrefix) && !prev.startsWith(g.urlPrefix)) {
        sidebarStore.set(`sidebar:${g.id}`, "true");
      }
    });
  }, [pathname]);

  const flatItems = React.useMemo(() => {
    if (!searchTerm.trim()) return [];
    const term = normalizeStr(searchTerm);
    const results: Array<{
      title: string;
      url: string;
      icon: React.ComponentType;
    }> = [];
    groups.forEach((group) => {
      const groupMatches = normalizeStr(group.title).includes(term);
      group.items.forEach((item) => {
        if (groupMatches || normalizeStr(item.title).includes(term)) {
          if (!results.some((r) => r.url === item.url)) {
            results.push(item);
          }
        }
      });
    });
    return results;
  }, [searchTerm]);

  const handleSearchInputKeyDown = (
    e: React.KeyboardEvent<HTMLInputElement>,
  ) => {
    if (e.key === "Enter") {
      if (flatItems.length === 1) {
        e.preventDefault();
        ui.navigation.go(flatItems[0].url);
        setSearchTerm("");
      }
    }
  };

  const navigateSearchItem = (url: string) => {
    setSearchTerm("");
    ui.navigation.go(url);
  };

  return (
    <Sidebar collapsible="icon">
      <div data-navigation-sidebar="true" className="contents">
        <SidebarHeader className="border-sidebar-border border-b">
          <SidebarMenu className="gap-2">
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname === homeItem.url}
                tooltip={homeItem.title}
              >
                <Link
                  href={homeItem.url}
                  data-navigation-sidebar-current={
                    pathname === homeItem.url ? "true" : undefined
                  }
                  onClick={(event) => {
                    event.preventDefault();
                    ui.navigation.go(homeItem.url);
                  }}
                >
                  <homeItem.icon />
                  <span>{homeItem.title}</span>
                  <KbdGroup className="ml-auto group-data-[collapsible=icon]:hidden">
                    <Kbd>Alt</Kbd>
                    <Kbd>H</Kbd>
                  </KbdGroup>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem className="group-data-[state=collapsed]:hidden">
              <InputGroup>
                <InputGroupInput
                  ref={searchInputRef}
                  placeholder="Buscar menu..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  onKeyDown={handleSearchInputKeyDown}
                  className="h-8 rounded-lg border-none text-xs"
                />

                <InputGroupAddon>
                  <Search className="text-muted-foreground size-3" />
                </InputGroupAddon>

                <InputGroupAddon align="inline-end">
                  <Kbd>Alt</Kbd>
                  <Kbd>S</Kbd>
                </InputGroupAddon>
              </InputGroup>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>
        <SidebarContent>
          {searchTerm.trim() ? (
            <SidebarGroup>
              <SidebarGroupContent>
                <SidebarMenu>
                  {flatItems.map((item, index) => {
                    const Icon = item.icon;
                    const active = pathname === item.url;
                    return (
                      <SidebarMenuItem key={item.title}>
                        <SidebarMenuButton asChild isActive={active}>
                          <Link
                            id={`sidebar-search-item-${index}`}
                            href={item.url}
                            data-navigation-sidebar-current={
                              pathname === item.url ? "true" : undefined
                            }
                            onClick={(event) => {
                              event.preventDefault();
                              navigateSearchItem(item.url);
                            }}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                e.preventDefault();
                                e.stopPropagation();
                                navigateSearchItem(item.url);
                              }
                            }}
                          >
                            <Icon />
                            <span>{item.title}</span>
                          </Link>
                        </SidebarMenuButton>
                      </SidebarMenuItem>
                    );
                  })}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          ) : (
            groups.map((group) => (
              <SidebarGroupSection
                key={group.id}
                group={group}
                pathname={pathname}
                searchTerm={searchTerm}
              />
            ))
          )}
        </SidebarContent>
        <SidebarFooter>
          <SidebarTrigger className="ml-auto" />
        </SidebarFooter>
        <SidebarRail />
      </div>
    </Sidebar>
  );
}
