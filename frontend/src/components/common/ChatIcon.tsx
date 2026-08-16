export function ChatIcon({ className = 'h-5 w-5' }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      strokeWidth={1.8}
      stroke="currentColor"
      className={className}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M8.625 12a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm3.75 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm3.75 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z"
      />
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 3.75c-5.385 0-9.75 3.798-9.75 8.482 0 2.44.961 4.639 2.52 6.27-.078 1.02-.09 2.079-.337 3.11-.128.545.408 1.001.942.851 2.443-.686 4.122-1.205 4.625-1.393.75.198 1.519.33 2.3.33 5.385 0 9.75-3.798 9.75-8.482s-4.365-8.482-9.75-8.482Z"
      />
    </svg>
  )
}
