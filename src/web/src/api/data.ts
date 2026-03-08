import client from './client';

export async function exportData(): Promise<Blob> {
  const { data } = await client.get('/data/export', { responseType: 'blob' });
  return data;
}

export interface ImportResult {
  watchesImported: number;
  imagesImported: number;
  wearLogsImported: number;
}

export async function importData(file: File): Promise<ImportResult> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await client.post<ImportResult>('/data/import', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}
