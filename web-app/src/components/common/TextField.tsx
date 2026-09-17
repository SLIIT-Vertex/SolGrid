import { forwardRef } from 'react'
import type { ChangeEvent, ChangeEventHandler, InputHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
  sanitizeValue?: (value: string) => string
}

/** Input types that reject setSelectionRange; caret restoration is skipped for them. */
const SELECTIONLESS_TYPES = new Set(['email', 'number', 'date', 'datetime-local', 'time', 'month', 'week', 'color'])

/**
 * Rewrite the typed text in place while keeping the caret next to the character
 * the user just entered, so a rejected keystroke never sends the caret to the end.
 */
function applySanitizedValue(input: HTMLInputElement, sanitized: string) {
  const raw = input.value
  if (sanitized === raw) return

  const caret = input.selectionStart ?? raw.length
  const removedBeforeCaret = countRemovedBefore(raw, sanitized, caret)
  input.value = sanitized

  if (SELECTIONLESS_TYPES.has(input.type)) return
  const nextCaret = Math.min(sanitized.length, Math.max(0, caret - removedBeforeCaret))
  input.setSelectionRange(nextCaret, nextCaret)
}

/** Count how many of the characters dropped by sanitising sat before the caret. */
function countRemovedBefore(raw: string, sanitized: string, caret: number): number {
  let matched = 0
  for (let index = 0; index < caret && matched < sanitized.length; index += 1) {
    if (raw[index] === sanitized[matched]) matched += 1
  }
  return caret - matched
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  ({ label, error, hint, id, className, sanitizeValue, onChange, ...rest }, ref) => {
    const inputId = id ?? rest.name
    const handleChange: ChangeEventHandler<HTMLInputElement> = (event) => {
      if (sanitizeValue) {
        // Read from `event.target`: it stays the input even after React clears `currentTarget`.
        const input = event.target as HTMLInputElement
        applySanitizedValue(input, sanitizeValue(input.value))
      }
      onChange?.(event as ChangeEvent<HTMLInputElement>)
    }

    return (
      <div className="flex flex-col gap-1.5">
        {label ? (
          <label htmlFor={inputId} className="text-sm font-medium text-ink-700">
            {label}
          </label>
        ) : null}
        <input
          ref={ref}
          id={inputId}
          className={cn(
            'h-10 rounded-lg border border-ink-200 bg-white px-3 text-sm text-ink-900',
            'placeholder:text-ink-400',
            'focus:border-brand-500 focus:outline focus:outline-2 focus:outline-brand-100',
            'read-only:bg-ink-50 read-only:text-ink-700',
            error && 'border-red-300 focus:border-red-400 focus:outline-red-100',
            className,
          )}
          {...rest}
          onChange={handleChange}
          aria-invalid={Boolean(error)}
          aria-describedby={
            error ? `${inputId}-error` : rest['aria-describedby'] ?? (hint ? `${inputId}-hint` : undefined)
          }
        />
        {error ? (
          <p id={`${inputId}-error`} role="alert" className="text-sm text-red-600">
            {error}
          </p>
        ) : hint ? (
          <p id={`${inputId}-hint`} className="text-sm text-ink-500">{hint}</p>
        ) : null}
      </div>
    )
  },
)

TextField.displayName = 'TextField'
