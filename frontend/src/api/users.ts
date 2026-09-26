import type { UserSearchResponse } from '../types/auth'
import { api } from './client'

// Other users whose username contains the term (up to 10).
export async function search(
  username: string,
  signal?: AbortSignal,
): Promise<UserSearchResponse[]> {
  const { data } = await api.get<UserSearchResponse[]>('/users/search', {
    params: { username },
    signal,
  })
  return data
}
