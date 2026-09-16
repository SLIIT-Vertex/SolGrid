import { Button } from '@/components/common/Button'

interface PaginationProps {
  pageNumber: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ pageNumber, pageSize, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const rangeStart = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
  const rangeEnd = Math.min(totalCount, pageNumber * pageSize)

  return (
    <div className="flex items-center justify-between border-t border-ink-100 px-4 py-3">
      <p className="text-sm text-ink-500">
        {totalCount === 0 ? (
          'No results'
        ) : (
          <>
            Showing <span className="font-medium text-ink-700">{rangeStart}</span>–
            <span className="font-medium text-ink-700">{rangeEnd}</span> of{' '}
            <span className="font-medium text-ink-700">{totalCount}</span>
          </>
        )}
      </p>
      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(pageNumber - 1)}
          disabled={pageNumber <= 1}
        >
          Previous
        </Button>
        <span className="text-sm text-ink-500">
          Page {pageNumber} of {totalPages}
        </span>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(pageNumber + 1)}
          disabled={pageNumber >= totalPages}
        >
          Next
        </Button>
      </div>
    </div>
  )
}
