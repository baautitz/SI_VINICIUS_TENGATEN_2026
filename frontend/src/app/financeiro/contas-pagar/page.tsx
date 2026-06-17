import React from "react";
import { ContasPagarFeature } from "@/features/financeiro/contas-pagar";

export const metadata = {
  title: "Contas a Pagar - ERP",
  description: "Gerenciamento de obrigações e contas a pagar do sistema.",
};

export default function ContasPagarPage() {
  return (
    <main className="w-full h-full min-h-0 flex flex-col">
      <ContasPagarFeature />
    </main>
  );
}
