import React from "react";
import { ContasReceberFeature } from "@/features/financeiro/contas-receber";

export const metadata = {
  title: "Contas a Receber - ERP",
  description: "Gerenciamento de direitos e contas a receber do sistema.",
};

export default function ContasReceberPage() {
  return (
    <main className="w-full h-full min-h-0 flex flex-col">
      <ContasReceberFeature />
    </main>
  );
}
