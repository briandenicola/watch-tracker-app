export function serverMessage(error: unknown): string | undefined {
  const data = (error as { response?: { data?: Record<string, unknown> } })?.response?.data
  if (typeof data?.error === 'string') return data.error
  const errors = data?.errors as Record<string, string[]> | undefined
  const first = errors && Object.values(errors)[0]
  if (Array.isArray(first) && typeof first[0] === 'string') return first[0]
  if (typeof data?.title === 'string') return data.title
  return undefined
}
