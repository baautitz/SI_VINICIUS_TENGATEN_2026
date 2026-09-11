export type NavigationDirection =
  | "up"
  | "down"
  | "left"
  | "right"
  | "forward"
  | "backward"

export interface NavigationRect {
  left: number
  top: number
  width: number
  height: number
}

export interface NavigationCell {
  grid: string
  row: number
  column: number
}

export interface SpatialNavigationNode {
  id: string
  rect: NavigationRect
  index?: number
  cell?: NavigationCell | null
}

/**
 * Chooses a deterministic destination without touching the DOM. Grid metadata
 * wins over geometry, while ordinary forms use their visible rectangles.
 */
export function chooseSpatialDestination(
  source: SpatialNavigationNode,
  candidates: SpatialNavigationNode[],
  direction: NavigationDirection,
): string | null {
  const available = candidates.filter((candidate) => candidate.id !== source.id)
  let geometryCandidates = available

  if (source.cell) {
    const gridDestination = chooseGridDestination(source.cell, direction, available)
    if (gridDestination) return gridDestination
    const sourceGrid = source.cell.grid
    geometryCandidates = available.filter(
      (candidate) => candidate.cell?.grid !== sourceGrid,
    )
  }

  if (direction === "up" || direction === "down") {
    return chooseVertical(source, geometryCandidates, direction)
  }

  const horizontalDirection =
    direction === "left" || direction === "backward" ? "backward" : "forward"

  // ArrowLeft/ArrowRight behave like Excel cell movement: they stay on the
  // current visual row. Tab/Shift+Tab retain the row-boundary fallback.
  if (direction === "left" || direction === "right") {
    return chooseHorizontal(
      source,
      geometryCandidates.filter((candidate) => overlapsY(source.rect, candidate.rect)),
      horizontalDirection,
    )
  }

  const horizontalDestination = chooseHorizontal(
    source,
    geometryCandidates,
    horizontalDirection,
  )
  const verticalDirection = direction === "forward" ? "down" : "up"
  const verticalDestination = chooseVertical(source, geometryCandidates, verticalDirection)

  // Tab follows the next row when there is no control on the same visual row.
  // A diagonally displaced control is only used when no vertical destination
  // exists at all, preventing jumps from a wide field into a distant column.
  if (horizontalDestination && geometryCandidates.some((candidate) =>
    overlapsY(source.rect, candidate.rect) &&
    candidate.id === horizontalDestination,
  )) {
    return horizontalDestination
  }
  return verticalDestination ?? horizontalDestination
}

function chooseGridDestination(
  source: NavigationCell,
  direction: NavigationDirection,
  candidates: SpatialNavigationNode[],
): string | null {
  const cells = candidates.filter((candidate) => candidate.cell?.grid === source.grid)
  const isForward = direction === "forward" || direction === "right"

  if (direction === "up" || direction === "down") {
    return cells
      .filter((candidate) => candidate.cell?.column === source.column)
      .filter((candidate) =>
        direction === "down"
          ? candidate.cell!.row > source.row
          : candidate.cell!.row < source.row,
      )
      .sort((a, b) =>
        direction === "down"
          ? a.cell!.row - b.cell!.row
          : b.cell!.row - a.cell!.row,
      )[0]?.id ?? null
  }

  const sameRow = cells
    .filter((candidate) => candidate.cell?.row === source.row)
    .filter((candidate) =>
      isForward
        ? candidate.cell!.column > source.column
        : candidate.cell!.column < source.column,
    )
    .sort((a, b) =>
      isForward
        ? a.cell!.column - b.cell!.column
        : b.cell!.column - a.cell!.column,
    )
  if (sameRow[0]) return sameRow[0].id

  // Horizontal arrows stop at the edge of the row, like spreadsheet cells.
  if (direction === "left" || direction === "right") return null

  const nextRows = cells
    .filter((candidate) =>
      isForward
        ? candidate.cell!.row > source.row
        : candidate.cell!.row < source.row,
    )
    .sort((a, b) =>
      isForward
        ? a.cell!.row - b.cell!.row
        : b.cell!.row - a.cell!.row,
    )
  const row = nextRows[0]?.cell?.row
  if (row === undefined) return null

  return nextRows
    .filter((candidate) => candidate.cell!.row === row)
    .sort((a, b) =>
      isForward
        ? a.cell!.column - b.cell!.column
        : b.cell!.column - a.cell!.column,
    )[0]?.id ?? null
}

function chooseVertical(
  source: SpatialNavigationNode,
  candidates: SpatialNavigationNode[],
  direction: "up" | "down",
): string | null {
  const sourceCenterX = centerX(source.rect)

  // A vertical move is a move to the next visual row, not to whichever
  // control happens to overlap the source's X axis.  The previous ordering
  // preferred overlap before distance, so a footer button aligned with a wide
  // field could win over the checkbox immediately below that field.
  const directional = candidates
    .map((candidate) => ({
      candidate,
      gap: direction === "down"
        ? candidate.rect.top - (source.rect.top + source.rect.height)
        : source.rect.top - (candidate.rect.top + candidate.rect.height),
    }))
    .filter(({ candidate, gap }) => {
      if (gap < -2) return false

      // A tiny gap without perpendicular overlap is usually a control on the
      // same visual line with a different height (for example an icon or a
      // label). Keep tightly stacked table rows when their columns overlap,
      // but do not let those near-line controls outrank the next form row.
      const sameLineTolerance = Math.max(4, Math.min(source.rect.height, 40) * 0.25)
      return gap > sameLineTolerance || overlapsX(source.rect, candidate.rect)
    })

  if (!directional.length) return null

  const nearestGap = Math.min(...directional.map(({ gap }) => Math.max(0, gap)))
  // Controls in one visual row can differ by a few pixels because of labels,
  // borders or mixed control heights. Keep that small band together, then use
  // perpendicular overlap and horizontal distance inside the chosen row.
  const rowTolerance = Math.max(8, Math.min(source.rect.height, 40) * 0.25)
  const nextRow = directional
    .filter(({ gap }) => Math.max(0, gap) <= nearestGap + rowTolerance)
    .map(({ candidate, gap }) => ({ candidate, gap }))

  return nextRow
    .sort((a, b) => {
      const aOverlap = overlapsX(source.rect, a.candidate.rect) ? 0 : 1
      const bOverlap = overlapsX(source.rect, b.candidate.rect) ? 0 : 1
      if (aOverlap !== bOverlap) return aOverlap - bOverlap

      return Math.abs(centerX(a.candidate.rect) - sourceCenterX) -
        Math.abs(centerX(b.candidate.rect) - sourceCenterX) ||
        a.gap - b.gap ||
        (a.candidate.index ?? 0) - (b.candidate.index ?? 0)
    })[0]?.candidate.id ?? null
}

function chooseHorizontal(
  source: SpatialNavigationNode,
  candidates: SpatialNavigationNode[],
  direction: "forward" | "backward",
): string | null {
  const sourceCenterX = centerX(source.rect)
  const sourceCenterY = centerY(source.rect)
  return candidates
    .filter((candidate) => {
      const candidateCenterX = centerX(candidate.rect)
      return direction === "forward"
        ? candidateCenterX > sourceCenterX + 1
        : candidateCenterX < sourceCenterX - 1
    })
    .sort((a, b) => {
      const aOverlap = overlapsY(source.rect, a.rect) ? 0 : 1
      const bOverlap = overlapsY(source.rect, b.rect) ? 0 : 1
      if (aOverlap !== bOverlap) return aOverlap - bOverlap

      const primary = Math.abs(centerX(a.rect) - sourceCenterX) -
        Math.abs(centerX(b.rect) - sourceCenterX)
      if (primary !== 0) return primary

      return Math.abs(centerY(a.rect) - sourceCenterY) -
        Math.abs(centerY(b.rect) - sourceCenterY) ||
        (a.index ?? 0) - (b.index ?? 0)
    })[0]?.id ?? null
}

function centerX(rect: NavigationRect): number {
  return rect.left + rect.width / 2
}

function centerY(rect: NavigationRect): number {
  return rect.top + rect.height / 2
}

function overlapsX(a: NavigationRect, b: NavigationRect): boolean {
  return a.left < b.left + b.width && a.left + a.width > b.left
}

function overlapsY(a: NavigationRect, b: NavigationRect): boolean {
  return a.top < b.top + b.height && a.top + a.height > b.top
}
