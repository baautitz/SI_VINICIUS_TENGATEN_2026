import {
  Globe,
  Map,
  MapPin,
  Milestone,
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
  MapPinned,
  LayoutDashboard,
  CreditCard,
  Landmark,
  Receipt,
} from "lucide-react";
import React from "react";

export interface NavItem {
  title: string;
  url: string;
  icon: React.ComponentType<{ className?: string }>;
}

export interface NavGroup {
  id: string;
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  urlPrefix: string;
  items: NavItem[];
}

export const navigationConfig: NavGroup[] = [
  {
    id: "catalogo",
    title: "Catálogo",
    icon: Package,
    urlPrefix: "/catalogo",
    items: [
      { title: "Produtos", url: "/catalogo/produtos", icon: Package },
      { title: "Marcas", url: "/catalogo/marcas", icon: Tag },
      { title: "Categorias", url: "/catalogo/categorias", icon: Layers },
      { title: "Atributos", url: "/catalogo/atributos", icon: Sliders },
      { title: "Unidades de Medida", url: "/catalogo/unidades-medida", icon: Scale },
    ],
  },
  {
    id: "estoque",
    title: "Estoque",
    icon: ClipboardList,
    urlPrefix: "/estoque",
    items: [
      { title: "Movimentações", url: "/estoque/movimentacoes", icon: ClipboardList },
    ],
  },
  {
    id: "localizacao",
    title: "Localização",
    icon: MapPinned,
    urlPrefix: "/localizacao",
    items: [
      { title: "Países", url: "/localizacao/paises", icon: Globe },
      { title: "Estados", url: "/localizacao/estados", icon: Map },
      { title: "Cidades", url: "/localizacao/cidades", icon: MapPin },
      { title: "Bairros", url: "/localizacao/bairros", icon: Milestone },
    ],
  },
  {
    id: "parceiros",
    title: "Parceiros",
    icon: BriefcaseBusiness,
    urlPrefix: "/parceiros",
    items: [
      { title: "Clientes", url: "/parceiros/clientes", icon: Users },
      { title: "Fornecedores", url: "/parceiros/fornecedores", icon: Truck },
      { title: "Emitentes", url: "/parceiros/emitentes", icon: UserCircle },
    ],
  },
  {
    id: "logistica",
    title: "Logística",
    icon: Truck,
    urlPrefix: "/logistica",
    items: [
      { title: "Transportadoras", url: "/logistica/transportadoras", icon: Truck },
      { title: "Veículos", url: "/logistica/veiculos", icon: Car },
    ],
  },
  {
    id: "pagamentos",
    title: "Pagamentos",
    icon: CreditCard,
    urlPrefix: "/pagamentos",
    items: [
      { title: "Métodos de Pagamento", url: "/pagamentos/metodos", icon: Landmark },
      { title: "Condições de Pagamento", url: "/pagamentos/condicoes", icon: Receipt },
    ],
  },
  {
    id: "financeiro",
    title: "Financeiro",
    icon: Landmark,
    urlPrefix: "/financeiro",
    items: [
      { title: "Contas a Pagar", url: "/financeiro/contas-pagar", icon: Receipt },
      { title: "Contas a Receber", url: "/financeiro/contas-receber", icon: Landmark },
    ],
  },
];

export const homeItem = {
  title: "Início",
  url: "/",
  icon: LayoutDashboard,
};
