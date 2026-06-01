import React, { useState } from "react"
import { RowSelectionState } from "@tanstack/react-table"

interface UseFeatureListOptions {
  initialSearchTerm?: string
}

export function useFeatureList<T>({ initialSearchTerm = "" }: UseFeatureListOptions = {}) {
  const [searchTerm, setSearchTerm] = useState(initialSearchTerm)
  const deferredSearch = React.useDeferredValue(searchTerm)
  const [page, setPage] = useState(1)

  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})

  const [isUpsertOpen, setIsUpsertOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<T | null>(null)

  const [deleteDialogOpen, setDeleteDialogVisible] = useState(false)
  const [itemToDelete, setItemToDelete] = useState<T | null>(null)

  const handleCreate = () => {
    setEditingItem(null)
    setIsUpsertOpen(true)
  }

  const handleEdit = (item: T) => {
    setEditingItem(item)
    setIsUpsertOpen(true)
  }

  const handleDeleteClick = (item: T) => {
    setItemToDelete(item)
    setDeleteDialogVisible(true)
  }

  const handleSearchChange = (val: string) => {
    setSearchTerm(val)
    setPage(1)
  }

  return {
    searchTerm,
    deferredSearch,
    page,
    setPage,
    rowSelection,
    setRowSelection,
    isUpsertOpen,
    setIsUpsertOpen,
    editingItem,
    deleteDialogOpen,
    setDeleteDialogVisible,
    itemToDelete,
    handleCreate,
    handleEdit,
    handleDeleteClick,
    handleSearchChange,
  }
}
