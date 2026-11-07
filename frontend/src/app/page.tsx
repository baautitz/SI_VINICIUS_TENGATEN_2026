import Link from "next/link";
import { navigationConfig as systemModules } from "@/config/navigation";

const cores: Record<string, { icone: string }> = {
  vendas: {
    icone: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  },
  compras: {
    icone: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  },
  nfes: {
    icone: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
  },
  catalogo: {
    icone: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  },
  estoque: {
    icone: "bg-orange-500/10 text-orange-600 dark:text-orange-400",
  },
  localizacao: {
    icone: "bg-teal-500/10 text-teal-600 dark:text-teal-400",
  },
  parceiros: {
    icone: "bg-pink-500/10 text-pink-600 dark:text-pink-400",
  },
  financeiro: {
    icone: "bg-indigo-500/10 text-indigo-600 dark:text-indigo-400",
  },
};

const corPadrao = {
  icone: "bg-primary/10 text-primary",
};

export default function Home() {
  return (
    <div
      data-navigation-cards="true"
      className="grid gap-4 lg:grid-cols-2 2xl:grid-cols-3"
    >
      {systemModules.map((module) => {
        const ModuleIcon = module.icon;
        const cor = cores[module.id] ?? corPadrao;

        return (
          <section
            key={module.id}
            className="bg-card flex flex-col overflow-hidden rounded-xl border shadow-xs"
          >
            <header className="flex items-center gap-3 border-b px-4 py-3">
              <div
                className={`flex size-8 items-center justify-center rounded-md ${cor.icone}`}
              >
                <ModuleIcon className="size-4" />
              </div>
              <h2 className="text-lg font-semibold tracking-tight">
                {module.title}
              </h2>
            </header>

            <nav className="flex flex-col p-2">
              {module.items.map((entity) => {
                const Icon = entity.icon;
                return (
                  <Link
                    key={entity.url + entity.title}
                    href={entity.url}
                    className="hover:bg-muted focus-visible:ring-ring text-muted-foreground hover:text-foreground flex items-center gap-3 rounded-md px-2 py-2 text-sm font-medium transition-colors outline-none focus-visible:ring-2"
                  >
                    <Icon className="size-4 shrink-0" />
                    <span>{entity.title}</span>
                  </Link>
                );
              })}
            </nav>
          </section>
        );
      })}
    </div>
  );
}
