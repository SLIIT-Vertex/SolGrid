export async function allPages<T>(
  load: (page: number) => Promise<{ items: T[]; totalCount: number }>,
) {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const result = await load(page)
    items.push(...result.items)
    if (!result.items.length || items.length >= result.totalCount) return items
  }
}
