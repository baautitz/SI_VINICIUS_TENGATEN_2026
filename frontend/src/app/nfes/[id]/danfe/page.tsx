import React from "react";
import { Danfe } from "@/features/nfes/danfe";

export default async function DanfePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <Danfe id={Number(id)} />;
}
