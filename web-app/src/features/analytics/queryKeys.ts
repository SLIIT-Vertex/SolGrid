export const analyticsKeys = {
  all: ['analytics'] as const,
  snapshot: () => [...analyticsKeys.all, 'snapshot'] as const,
}
