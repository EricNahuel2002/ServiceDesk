import type { SelectHTMLAttributes } from 'react'

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
}

export function Select({ label, error, className = '', children, ...props }: SelectProps) {
  const selectId = props.id ?? props.name

  return (
    <div className="flex flex-col gap-1">
      {label ? (
        <label htmlFor={selectId} className="text-sm font-medium text-gray-700">
          {label}
        </label>
      ) : null}
      <select
        id={selectId}
        className={`rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-emerald-500 focus:outline-none ${className}`}
        {...props}
      >
        {children}
      </select>
      {error ? <p className="text-sm text-red-600">{error}</p> : null}
    </div>
  )
}
