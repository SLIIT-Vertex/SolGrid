/** Chart colors mapped to the app's Tailwind theme, so charts and badges stay in one language. */
export const chartColors = {
  brand: '#139a68',
  brandSoft: '#46d89e',
  amber: '#b45309',
  amberSoft: '#f59e0b',
  red: '#b91c1c',
  ink: '#5f6874',
  inkSoft: '#98a1ad',
} as const

export const slotColors = {
  available: chartColors.brand,
  reserved: chartColors.amberSoft,
  occupied: chartColors.ink,
  outOfService: chartColors.red,
} as const

export const reservationColors = {
  pending: chartColors.amberSoft,
  approved: chartColors.brand,
  completed: chartColors.brandSoft,
  rejected: chartColors.red,
  cancelled: chartColors.inkSoft,
} as const
