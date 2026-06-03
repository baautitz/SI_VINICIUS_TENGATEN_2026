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
} from "@/components/ui/sidebar";

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { ChevronRight } from "lucide-react";

const catalogoItems = [
  {
    title: "Unidades de Medida",
    url: "/catalogo/unidades-medida",
    icon: Scale,
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

export function AppSidebar() {
  const pathname = usePathname();

  return (
    <Sidebar collapsible="icon">
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
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
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <Collapsible
                asChild
                defaultOpen={pathname.startsWith("/catalogo")}
                className="group/collapsible"
              >
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton
                      tooltip="Catálogo"
                      isActive={pathname.startsWith("/catalogo")}
                    >
                      <Package />
                      <span>Catálogo</span>
                      <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <SidebarMenuSub>
                      {catalogoItems.map((item) => {
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

        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <Collapsible
                asChild
                defaultOpen={pathname.startsWith("/localizacao")}
                className="group/collapsible"
              >
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton
                      tooltip="Localização"
                      isActive={pathname.startsWith("/localizacao")}
                    >
                      <MapPinned />
                      <span>Localização</span>
                      <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <SidebarMenuSub>
                      {localizationItems.map((item) => {
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

        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <Collapsible
                asChild
                defaultOpen={pathname.startsWith("/parceiros")}
                className="group/collapsible"
              >
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton
                      tooltip="Parceiros"
                      isActive={pathname.startsWith("/parceiros")}
                    >
                      <BriefcaseBusiness />
                      <span>Parceiros</span>
                      <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <SidebarMenuSub>
                      {parceirosItems.map((item) => {
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
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <Collapsible
                asChild
                defaultOpen={pathname.startsWith("/logistica")}
                className="group/collapsible"
              >
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton
                      tooltip="Logística"
                      isActive={pathname.startsWith("/logistica")}
                    >
                      <Truck />
                      <span>Logística</span>
                      <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <SidebarMenuSub>
                      {logisticaItems.map((item) => {
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
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
