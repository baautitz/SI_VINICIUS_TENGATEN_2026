"use client";

import * as React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Globe,
  Map,
  MapPin,
  MapPinned,
  Milestone,
  LayoutDashboard,
  Users,
  Truck,
  UserCircle,
  BriefcaseBusiness,
  Car,
  Scale,
  Package,
  Tag,
  Layers,
  Sliders,
  ClipboardList,
  ChevronRight,
  Search,
} from "lucide-react";

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
} from "@/components/ui/sidebar";

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { Input } from "@/components/ui/input";
import { Kbd } from "@/components/ui/kbd";
import { useHotkeys } from "react-hotkeys-hook";

const catalogoItems = [
  {
    title: "Produtos",
    url: "/catalogo/produtos",
    icon: Package,
  },
  {
    title: "Marcas",
    url: "/catalogo/marcas",
    icon: Tag,
  },
  {
    title: "Categorias",
    url: "/catalogo/categorias",
    icon: Layers,
  },
  {
    title: "Atributos",
    url: "/catalogo/atributos",
    icon: Sliders,
  },
  {
    title: "Unidades de Medida",
    url: "/catalogo/unidades-medida",
    icon: Scale,
  },
];

const localizationItems = [
  {
    title: "Países",
    url: "/localizacao/paises",
    icon: Globe,
  },
  {
    title: "Estados",
    url: "/localizacao/estados",
    icon: Map,
  },
  {
    title: "Cidades",
    url: "/localizacao/cidades",
    icon: MapPin,
  },
  {
    title: "Bairros",
    url: "/localizacao/bairros",
    icon: Milestone,
  },
];

const parceirosItems = [
  {
    title: "Clientes",
    url: "/parceiros/clientes",
    icon: Users,
  },
  {
    title: "Fornecedores",
    url: "/parceiros/fornecedores",
    icon: Truck,
  },
  {
    title: "Emitentes",
    url: "/parceiros/emitentes",
    icon: UserCircle,
  },
];

const logisticaItems = [
  {
    title: "Transportadoras",
    url: "/logistica/transportadoras",
    icon: Truck,
  },
  {
    title: "Veículos",
    url: "/logistica/veiculos",
    icon: Car,
  },
];

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
    } catch (e) {
      console.error(e);
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

const groups = [
  {
    id: "catalogo",
    title: "Catálogo",
    icon: Package,
    urlPrefix: "/catalogo",
    items: catalogoItems,
  },
  {
    id: "estoque",
    title: "Estoque",
    icon: ClipboardList,
    urlPrefix: "/estoque",
    items: [
      {
        title: "Movimentações",
        url: "/estoque/movimentacoes",
        icon: ClipboardList,
      },
    ],
  },
  {
    id: "localizacao",
    title: "Localização",
    icon: MapPinned,
    urlPrefix: "/localizacao",
    items: localizationItems,
  },
  {
    id: "parceiros",
    title: "Parceiros",
    icon: BriefcaseBusiness,
    urlPrefix: "/parceiros",
    items: parceirosItems,
  },
  {
    id: "logistica",
    title: "Logística",
    icon: Truck,
    urlPrefix: "/logistica",
    items: logisticaItems,
  },
];

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

  const GroupIcon = group.icon;
  const { state, setOpen } = useSidebar();

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
                        <SidebarMenuSubButton asChild isActive={active}>
                          <Link href={item.url}>
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
  const pathname = usePathname();
  const [searchTerm, setSearchTerm] = React.useState("");
  const searchInputRef = React.useRef<HTMLInputElement>(null);
  const { state, setOpen } = useSidebar();

  useHotkeys(
    "alt+s",
    (e) => {
      e.preventDefault();
      if (state === "collapsed") {
        setOpen(true);
      }
      setTimeout(() => {
        searchInputRef.current?.focus();
        searchInputRef.current?.select();
      }, 50);
    },
    { enableOnFormTags: true },
    [state, setOpen],
  );

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

  const filteredGroups = React.useMemo(() => {
    if (!searchTerm.trim()) return groups;

    const term = normalizeStr(searchTerm);
    return groups
      .map((group) => {
        const groupMatches = normalizeStr(group.title).includes(term);
        const matchedItems = group.items.filter(
          (item) => groupMatches || normalizeStr(item.title).includes(term),
        );

        return {
          ...group,
          items: matchedItems,
        };
      })
      .filter((group) => group.items.length > 0);
  }, [searchTerm]);

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="flex h-12 items-center justify-end px-3 group-data-[state=collapsed]:justify-center group-data-[state=collapsed]:px-0 border-b border-sidebar-border shrink-0">
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              asChild
              isActive={pathname === "/"}
              tooltip="Início"
            >
              <Link href="/">
                <LayoutDashboard />
                <span>Início</span>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <div className="px-3 py-2 group-data-[state=collapsed]:hidden border-b border-sidebar-border/50 shrink-0">
          <div className="relative">
            <Input
              ref={searchInputRef}
              placeholder="Buscar menu..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="h-8 pl-8 pr-10 text-xs rounded-lg bg-background"
            />
            <Search className="absolute left-2.5 top-2.5 size-3 text-muted-foreground" />
            <div className="absolute right-2.5 top-1.5 flex items-center gap-0.5 pointer-events-none select-none">
              <Kbd className="h-5 text-[9px] px-1 bg-muted/40 border-none shadow-none">
                Alt+S
              </Kbd>
            </div>
          </div>
        </div>
        {filteredGroups.map((group) => (
          <SidebarGroupSection
            key={group.id}
            group={group}
            pathname={pathname}
            searchTerm={searchTerm}
          />
        ))}
      </SidebarContent>
      <SidebarFooter>
        <SidebarTrigger className="ml-auto" />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
