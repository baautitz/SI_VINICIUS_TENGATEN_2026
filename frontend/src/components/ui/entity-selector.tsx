"use client"

import React, { useState, useRef } from "react"
import { Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Field, FieldLabel, FieldError } from "@/components/ui/field"

interface EntitySelectorProps<T> {
  name: string
  label: string
  placeholder?: string
  error?: string
  initialDisplayValue?: string
  
  onSelectId: (id: number | undefined) => void
  
  // APIs
  onSearchById: (id: number) => Promise<T | null>
  onSearchByName: (term: string) => Promise<T | null>
  getDisplayLabel: (item: T) => string
  getId: (item: T) => number

  // Modal
  modalTitle: string
  renderFeature: (props: { selectionMode: boolean; onSelect: (item: T) => void; initialSearchTerm: string }) => React.ReactNode
}

export function EntitySelector<T>({
  name,
  label,
  placeholder = "Digite o código, nome ou clique na lupa...",
  error,
  initialDisplayValue = "",
  onSelectId,
  onSearchById,
  onSearchByName,
  getDisplayLabel,
  getId,
  modalTitle,
  renderFeature,
}: EntitySelectorProps<T>) {
  const [isOpen, setIsOpen] = useState(false)
  const [searchText, setSearchText] = useState(initialDisplayValue)
  const [selectedLabel, setSelectedLabel] = useState(initialDisplayValue)
  const [prevInitialValue, setPrevInitialValue] = useState(initialDisplayValue)
  const inputRef = useRef<HTMLInputElement>(null)

  if (initialDisplayValue !== prevInitialValue) {
    setPrevInitialValue(initialDisplayValue)
    setSearchText(initialDisplayValue)
    setSelectedLabel(initialDisplayValue)
  }

  const handleSearch = async (text: string, isBlur = false) => {
    if (!text.trim()) {
      onSelectId(undefined)
      setSearchText("")
      setSelectedLabel("")
      return
    }
    if (isBlur && selectedLabel === text) return

    try {
      // 1. Prioridade: Busca por ID se o texto for numérico
      const numericId = parseInt(text, 10)
      if (!isNaN(numericId) && /^\d+$/.test(text.trim())) {
        try {
          const matched = await onSearchById(numericId)
          if (matched) {
            const newLabel = getDisplayLabel(matched)
            onSelectId(getId(matched))
            setSearchText(newLabel)
            setSelectedLabel(newLabel)
            return
          }
        } catch {
          // Fallback to name search
        }
      }

      // 2. Busca por nome (tenta encontrar correspondência exata de 1 item)
      const matched = await onSearchByName(text)
      if (matched) {
        const newLabel = getDisplayLabel(matched)
        onSelectId(getId(matched))
        setSearchText(newLabel)
        setSelectedLabel(newLabel)
        return
      }

      if (isBlur) {
        // Se perdeu o foco e não achou exatamente 1, volta para o que estava selecionado
        setSearchText(selectedLabel)
      } else {
        setIsOpen(true)
      }
    } catch {
      if (isBlur) {
        setSearchText(selectedLabel)
      } else {
        setIsOpen(true)
      }
    }
  }

  const handleSelect = (item: T) => {
    const newLabel = getDisplayLabel(item)
    onSelectId(getId(item))
    setSearchText(newLabel)
    setSelectedLabel(newLabel)
    setIsOpen(false)
    setTimeout(() => {
      document.getElementById(name)?.focus()
    }, 100)
  }

  // Função para limpar parênteses (ex: Foz do Iguaçu (PR) -> Foz do Iguaçu)
  const getCleanSearchTerm = (text: string) => {
    if (text === selectedLabel) {
      // Remove o sufixo no formato " (UF)" ou qualquer coisa entre parênteses no final
      return text.replace(/\s*\([^)]*\)$/, '').trim()
    }
    return text
  }

  return (
    <>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor={name}>{label}</FieldLabel>
        <div className="relative flex-1">
          <Input
            ref={inputRef}
            id={name}
            placeholder={placeholder}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault()
                handleSearch(searchText)
              }
            }}
            onBlur={() => {
              if (searchText !== selectedLabel && searchText.trim() !== "") {
                handleSearch(searchText, true)
              }
            }}
            aria-invalid={!!error}
            className="pr-10"
          />
          <Button
            size="icon-xs"
            variant="ghost"
            type="button"
            className="absolute right-1 top-1 h-6 w-6 text-muted-foreground hover:text-foreground"
            onClick={() => setIsOpen(true)}
          >
            <Search className="size-4" />
          </Button>
        </div>
        {error && <FieldError>{error}</FieldError>}
      </Field>

      <Dialog open={isOpen} onOpenChange={(o) => {
        setIsOpen(o)
        if (!o) {
          setTimeout(() => {
            document.getElementById(name)?.focus()
          }, 100)
        }
      }}>
        <DialogContent className="sm:max-w-[70vw] w-[70vw] max-w-none max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{modalTitle}</DialogTitle>
          </DialogHeader>
          <div className="py-2">
            {renderFeature({
              selectionMode: true,
              onSelect: handleSelect,
              initialSearchTerm: getCleanSearchTerm(searchText),
            })}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
