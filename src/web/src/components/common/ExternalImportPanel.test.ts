import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ExternalImportPanel from './ExternalImportPanel.vue'

const dataImport = vi.hoisted(() => ({
  previewExternalImport: vi.fn(),
  importExternalData: vi.fn(),
}))

vi.mock('@/services/dataImport', () => dataImport)

describe('ExternalImportPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    dataImport.previewExternalImport.mockResolvedValue({
      source: 'Wristcheck',
      collectionCount: 2,
      wishlistCount: 0,
      disposedCount: 0,
      duplicateCount: 1,
      rows: [
        {
          rowNumber: 2,
          brand: 'Seiko',
          model: 'SPB143',
          destination: 'Collection',
          canImport: true,
          isDuplicate: true,
          duplicateWatchId: 7,
          duplicateReason: 'Manufacturer and model match an existing watch.',
          warnings: [],
          errors: [],
        },
        {
          rowNumber: 3,
          brand: 'Omega',
          model: 'Speedmaster',
          destination: 'Collection',
          canImport: true,
          isDuplicate: false,
          warnings: [],
          errors: [],
        },
        {
          rowNumber: 4,
          brand: '',
          model: 'Missing brand',
          destination: 'Collection',
          canImport: false,
          isDuplicate: false,
          warnings: [],
          errors: ['The Brand field is required.'],
        },
      ],
    })
    dataImport.importExternalData.mockResolvedValue({
      imported: 2,
      skipped: 1,
      duplicatesImported: 1,
    })
  })

  it('leaves duplicates unchecked and invalid rows disabled in the preview', async () => {
    const wrapper = mount(ExternalImportPanel)
    const file = new File(['csv'], 'wristcheck.csv', { type: 'text/csv' })
    const input = wrapper.get('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [file] })
    await input.trigger('change')
    await wrapper.findAll('button').find(button => button.text() === 'Preview Import')!.trigger('click')
    await flushPromises()

    const checkboxes = wrapper.findAll('input[type="checkbox"]')
    expect(checkboxes).toHaveLength(3)
    expect((checkboxes[0].element as HTMLInputElement).checked).toBe(false)
    expect((checkboxes[1].element as HTMLInputElement).checked).toBe(true)
    expect(checkboxes[2].attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Likely duplicate of watch #7')
    expect(wrapper.text()).toContain('Import 1 Selected')
  })

  it('allows an explicit duplicate override and imports only selected rows', async () => {
    const wrapper = mount(ExternalImportPanel)
    const file = new File(['csv'], 'wristcheck.csv', { type: 'text/csv' })
    const input = wrapper.get('input[type="file"]')
    Object.defineProperty(input.element, 'files', { value: [file] })
    await input.trigger('change')
    await wrapper.findAll('button').find(button => button.text() === 'Preview Import')!.trigger('click')
    await flushPromises()

    await wrapper.findAll('input[type="checkbox"]')[0].setValue(true)
    await wrapper.findAll('button').find(button => button.text() === 'Import 2 Selected')!.trigger('click')
    await flushPromises()

    expect(dataImport.importExternalData).toHaveBeenCalledWith(file, expect.arrayContaining([2, 3]))
    expect(wrapper.text()).toContain('Imported 2 watches; skipped 1.')
  })
})
