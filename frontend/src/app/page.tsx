import Link from "next/link";
import {
  Globe,
  Map,
  MapPin,
  Milestone,
  LayoutGrid,
  Users,
  Truck,
  UserCircle,
  Car,
  Scale,
} from "lucide-react";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";

const systemModules = [
  {
    name: "Catálogo",
    entities: [
      { name: "Unidades de Medida", url: "/catalogo/unidades-medida", icon: Scale },
    ],
  },
  {
    name: "Localização",
    entities: [
      { name: "Países", url: "/localizacao/paises", icon: Globe },
      { name: "Estados", url: "/localizacao/estados", icon: Map },
      { name: "Cidades", url: "/localizacao/cidades", icon: MapPin },
      { name: "Bairros", url: "/localizacao/bairros", icon: Milestone },
    ],
  },
  {
    name: "Parceiros",
    entities: [
      { name: "Clientes", url: "/parceiros/clientes", icon: Users },
      { name: "Fornecedores", url: "/parceiros/fornecedores", icon: Truck },
      { name: "Emitentes", url: "/parceiros/emitentes", icon: UserCircle },
    ],
  },
  {
    name: "Logística",
    entities: [
      { name: "Transportadoras", url: "/logistica/transportadoras", icon: Truck },
      { name: "Veículos", url: "/logistica/veiculos", icon: Car },
    ],
  },
];

export default function Home() {
  return (
    <div className="container py-10 max-w-6xl mx-auto space-y-12">
      {systemModules.map((module) => (
        <div key={module.name} className="space-y-3">
          <div className="flex items-center gap-2">
            <LayoutGrid className="size-5 text-muted-foreground" />
            <h2 className="text-2xl font-semibold tracking-tight">
              {module.name}
            </h2>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {module.entities.map((entity) => {
              const Icon = entity.icon;
              return (
                <Link key={entity.name} href={entity.url}>
                  <Card className="hover:bg-muted/50 transition-colors border-muted shadow-sm">
                    <CardHeader className="flex flex-row items-center gap-4 space-y-0 px-4 py-2">
                      <div className="flex size-10 items-center justify-center rounded-lg bg-primary/5 text-primary">
                        <Icon className="size-5" />
                      </div>
                      <CardTitle className="text-lg font-medium">
                        {entity.name}
                      </CardTitle>
                    </CardHeader>
                  </Card>
                </Link>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
