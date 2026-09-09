import { api } from './api'
import type { ExternalImportPreview, ExternalImportResult } from '@/types'

export async function previewExternalImport(file: File): Promise<ExternalImportPreview> {
  const form = new FormData()
  form.append('file', file)
  const { data } = await api.post<ExternalImportPreview>('/api/data/import/external/preview', form)
  return data
}

export async function importExternalData(file: File, selectedRows: number[]): Promise<ExternalImportResult> {
  const form = new FormData()
  form.append('file', file)
  form.append('selectedRows', selectedRows.join(','))
  const { data } = await api.post<ExternalImportResult>('/api/data/import/external', form)
  return data
}
