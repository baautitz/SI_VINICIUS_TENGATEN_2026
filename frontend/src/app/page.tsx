import Link from "next/link";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { navigationConfig as systemModules } from "@/config/navigation";

export default function Home() {
  return (
    <div className="container mx-auto max-w-6xl space-y-12 py-10">
      {systemModules.map((module) => {
        const ModuleIcon = module.icon;

        return (
          <div key={module.title} className="space-y-3">
            <div className="flex items-center gap-2">
              <ModuleIcon className="text-muted-foreground size-5" />
              <h2 className="text-2xl font-semibold tracking-tight">
                {module.title}
              </h2>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {module.items.map((entity) => {
                const Icon = entity.icon;
                return (
                  <Link key={entity.title} href={entity.url}>
                    <Card className="hover:bg-muted/50 border-muted transition-colors">
                      <CardHeader className="flex flex-row items-center gap-4 space-y-0 px-4 py-2">
                        <div className="bg-primary/5 text-primary flex size-10 items-center justify-center rounded-lg">
                          <Icon className="size-5" />
                        </div>
                        <CardTitle className="text-lg font-medium">
                          {entity.title}
                        </CardTitle>
                      </CardHeader>
                    </Card>
                  </Link>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}
